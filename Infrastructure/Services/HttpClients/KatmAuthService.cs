using Domain.Common.Settings;
using Infrastructure.Common.Helpers.JsonHelpes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Infrastructure.Services.HttpClients
{
    public interface IKatmAuthService
    {
        /// <summary>
        /// POST в КАТМ. При UseJwtAuth: логин/кэш токена, заголовок Bearer, без блока security в теле.
        /// Иначе — тело как есть (ТЗ 9.16).
        /// </summary>
        Task<HttpResponseMessage> SendPostAsync(HttpClient httpClient, string jsonData, CancellationToken cancellationToken);
    }

    public class KatmAuthService(
        CreditBureauReportApiOptions options,
        IHttpClientFactory httpClientFactory,
        ILogger<KatmAuthService> logger,
        TimeProvider? timeProvider = null) : IKatmAuthService
    {
        private sealed record CachedToken(string Value, DateTimeOffset ExpiresAt);

        private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);
        // ТЗ 10.0: access-токен живёт 1 ч; запас на случай, если exp не прочитался.
        private static readonly TimeSpan FallbackLifetime = TimeSpan.FromMinutes(50);

        private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
        private readonly SemaphoreSlim _loginLock = new(1, 1);
        private CachedToken? _cached;

        public async Task<HttpResponseMessage> SendPostAsync(HttpClient httpClient, string jsonData, CancellationToken cancellationToken)
        {
            if (!options.UseJwtAuth)
                return await PostAsync(httpClient, jsonData, token: null, cancellationToken);

            var body = jsonData.RemoveSecurity();
            var token = await GetAccessTokenAsync(cancellationToken);
            return await PostAsync(httpClient, body, token, cancellationToken);
        }

        private static async Task<HttpResponseMessage> PostAsync(HttpClient httpClient, string body, string? token, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Content.Headers.ContentType!.CharSet = string.Empty;
            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return await httpClient.SendAsync(request, cancellationToken);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            var cached = _cached;
            if (IsFresh(cached))
                return cached!.Value;

            await _loginLock.WaitAsync(cancellationToken);
            try
            {
                cached = _cached;
                if (IsFresh(cached))
                    return cached!.Value;

                var token = await LoginAsync(cancellationToken);
                _cached = new CachedToken(token, ReadExpiry(token) ?? _time.GetUtcNow() + FallbackLifetime);
                return token;
            }
            finally
            {
                _loginLock.Release();
            }
        }

        private bool IsFresh(CachedToken? cached) =>
            cached is not null && _time.GetUtcNow() < cached.ExpiresAt - RefreshMargin;

        private async Task<string> LoginAsync(CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();
            var payload = JsonConvert.SerializeObject(new { login = options.PLogin, password = options.PPassword });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(options.HostAddress + options.AuthLoginUrl, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            string? token = null;
            string? errorMessage = null;
            try
            {
                var json = JObject.Parse(responseBody);
                token = (string?)json.SelectToken("data.accessToken");
                errorMessage = (string?)json.SelectToken("error.errMsg");
            }
            catch (JsonException)
            {
                // не JSON (HTML-страница прокси и т.п.) — уйдёт в сообщение исключения как есть
            }

            if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException(
                    $"КАТМ auth/login не выдал токен: HTTP {(int)response.StatusCode}, {errorMessage ?? responseBody}");

            logger.LogInformation("КАТМ: получен новый access-токен");
            return token;
        }

        private static DateTimeOffset? ReadExpiry(string jwt)
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2)
                return null;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                var exp = (long?)JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)))["exp"];
                return exp is null ? null : DateTimeOffset.FromUnixTimeSeconds(exp.Value);
            }
            catch (Exception ex) when (ex is FormatException or JsonException)
            {
                return null;
            }
        }
    }
}
