# Переход на JWT-авторизацию КАТМ API (ТЗ 10.0) — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Все запросы к КАТМ API (CI-001…CI-023, CI-017 и `/credit/report/status`) отправляются с заголовком `Authorization: Bearer <token>` вместо `security.pLogin/pPassword` в теле, с включением по флагу к 1 декабря 2026.

**Architecture:** Весь HTTP к КАТМ уже идёт через две перегрузки `RequestManagerService.SendPostRequest`. Туда встраивается один новый сервис `KatmAuthService`: он логинится в `/auth/login`, кэширует access-токен до `exp` из JWT, ставит заголовок Bearer, вырезает блок `security` из тела и при HTTP 401 перелогинивается и повторяет запрос один раз. Обработчики CI, `CreditReportService`, `CreditReportXmlService`, контракты `BaseRequest`/`RequestSecurity` **не меняются**: блок `security` удаляется из готового JSON в одной точке. Флаг `UseJwtAuth=false` сохраняет текущее поведение (ТЗ 9.16) байт-в-байт.

**Tech Stack:** .NET 8, Newtonsoft.Json, `IHttpClientFactory` (уже зарегистрирован через `services.AddHttpClient()`), `TimeProvider` (BCL .NET 8), xUnit + Moq.

## Global Constraints

- ТЗ 10.0, раздел 2: `POST {base url}/auth/login`, тело `{"login": "...", "password": "..."}`; ответ `{"error":{"errId":0,"errMsg":"..."},"success":true,"data":{"accessToken":"...","refreshToken":"..."}}`.
- ТЗ 10.0: заголовок `Authorization: Bearer <token>` во всех запросах; access-токен живёт 1 ч (`exp: 1h`).
- `/auth/refresh` **не используется**: по ТЗ он принимает те же `login/password`, что и `/auth/login`, а refresh-токен живёт 10 мин. Повторный логин проще и даёт тот же результат.
- Тело запроса после удаления `security` должно сохранить форматы дат (`2026-06-17T09:18:37.409+0500`, `dd.MM.yyyy HH:mm:ss`) и сумм (`1500000.50`) **без изменений**.
- Токен никогда не пишется в логи, БД или Telegram.
- Новые NuGet-пакеты не добавляются.
- Тесты: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj`.

## Открытые вопросы к КАТМ (не блокируют задачи 1–4)

1. Base URL после 10.0: остаётся ли `/katm-api/v1`? (В ТЗ 10.0 Swagger указан без `/v1`.) Все пути вынесены в `appsettings.json`, поэтому код это не затрагивает.
2. Точное имя поля токена: в таблице ТЗ `accesToken`, в JSON-примере `accessToken`. План использует `accessToken`; сверить по Swagger до задачи 5.
3. Возвращает ли КАТМ HTTP 401 на просроченный токен или 200 с кодом ошибки в `result`? План обрабатывает 401; если окажется иначе, нужна правка в задаче 3.

## Файлы

| Файл | Что делаем |
|---|---|
| `Infrastructure/Common/Helpers/JsonHelpes/ExtensionJsonHelper.cs` | + `RemoveSecurity()` |
| `Infrastructure.Tests/Common/Helpers/JsonHelpes/ExtensionJsonHelperTests.cs` | + тесты `RemoveSecurity` |
| `Domain/Common/Settings/CreditBureauReportApiOptions.cs` | + `UseJwtAuth`, `AuthLoginUrl` |
| `Infrastructure/Services/HttpClients/KatmAuthService.cs` | **новый**: `IKatmAuthService` + `KatmAuthService` |
| `Infrastructure.Tests/Services/HttpClients/KatmAuthServiceTests.cs` | **новый** |
| `Infrastructure/Services/HttpClients/RequestManagerService.cs` | отправка через `IKatmAuthService` |
| `Infrastructure/Services/CreditBureauReportServices/Handlers/DependencyInjection.cs` | регистрация `IKatmAuthService` |
| `CreditBureauService/appsettings.json` | + `UseJwtAuth: false`, `AuthLoginUrl` |

---

### Task 1: `RemoveSecurity()` — вырезать блок `security` из тела без порчи форматов

**Files:**
- Modify: `Infrastructure/Common/Helpers/JsonHelpes/ExtensionJsonHelper.cs` (добавить метод после `RedactSecurity`)
- Test: `Infrastructure.Tests/Common/Helpers/JsonHelpes/ExtensionJsonHelperTests.cs`

**Interfaces:**
- Produces: `public static string RemoveSecurity(this string json)` в `Infrastructure.Common.Helpers.JsonHelpes.ExtensionJsonHelper`. Если `security` нет, возвращает исходную строку. На невалидном JSON бросает `JsonException`.

- [x] **Step 1: Написать падающие тесты** (дописать в конец класса `ExtensionJsonHelperTests`)

```csharp
    [Fact]
    public void RemoveSecurity_WhenSecurityPresent_RemovesItAndKeepsDateAndDecimalFormats()
    {
        const string json = """
            {"data":{"pDate":"2026-06-17T09:18:37.409+0500","claim_date":"03.06.2026 10:05:00","summa":1500000.50,"pReportId":21},
             "security":{"pLogin":"login","pPassword":"s3cr3t"}}
            """;

        var body = json.RemoveSecurity();

        Assert.DoesNotContain("security", body);
        Assert.DoesNotContain("s3cr3t", body);
        Assert.Contains("\"pDate\":\"2026-06-17T09:18:37.409+0500\"", body);
        Assert.Contains("\"claim_date\":\"03.06.2026 10:05:00\"", body);
        Assert.Contains("\"summa\":1500000.50", body);
        Assert.Contains("\"pReportId\":21", body);
    }

    [Fact]
    public void RemoveSecurity_WhenSecurityAbsent_ReturnsSameString()
    {
        const string json = "{\n  \"data\": {\"pClaimId\": \"c1\"}\n}";

        Assert.Same(json, json.RemoveSecurity());
    }
```

- [x] **Step 2: Запустить, убедиться что падают**

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ExtensionJsonHelperTests.RemoveSecurity"`
Expected: ошибка компиляции `'string' does not contain a definition for 'RemoveSecurity'`.

- [x] **Step 3: Реализация** (в `ExtensionJsonHelper`, после `RedactSecurity`)

```csharp
        /// <summary>
        /// Удаляет верхнеуровневый блок "security" из тела запроса (ТЗ 10.0: логин/пароль
        /// больше не передаются в теле, авторизация — заголовком Bearer).
        /// </summary>
        public static string RemoveSecurity(this string json)
        {
            // DateParseHandling.None / FloatParseHandling.Decimal: тело уходит в КАТМ,
            // даты и суммы должны остаться в исходном текстовом виде.
            var obj = JsonConvert.DeserializeObject<JObject>(json, new JsonSerializerSettings
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal
            });

            if (obj is null || !obj.Remove("security"))
            {
                return json;
            }

            return obj.ToString(Formatting.None);
        }
```

- [x] **Step 4: Запустить тесты**

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~ExtensionJsonHelperTests"`
Expected: PASS, все 7 тестов (5 старых + 2 новых).

- [x] **Step 5: Commit**

```bash
git add Infrastructure/Common/Helpers/JsonHelpes/ExtensionJsonHelper.cs Infrastructure.Tests/Common/Helpers/JsonHelpes/ExtensionJsonHelperTests.cs
git commit -m "feat: add RemoveSecurity JSON helper for KATM JWT auth"
```

---

### Task 2: `KatmAuthService` — логин, кэш токена, Bearer, удаление `security`

**Files:**
- Modify: `Domain/Common/Settings/CreditBureauReportApiOptions.cs`
- Create: `Infrastructure/Services/HttpClients/KatmAuthService.cs`
- Test: `Infrastructure.Tests/Services/HttpClients/KatmAuthServiceTests.cs`

**Interfaces:**
- Consumes: `string.RemoveSecurity()` из Task 1.
- Produces:
  - `CreditBureauReportApiOptions.UseJwtAuth` (`bool`), `CreditBureauReportApiOptions.AuthLoginUrl` (`string`).
  - `public interface IKatmAuthService { Task<HttpResponseMessage> SendPostAsync(HttpClient httpClient, string jsonData, CancellationToken cancellationToken); }`
  - `public class KatmAuthService(CreditBureauReportApiOptions options, IHttpClientFactory httpClientFactory, ILogger<KatmAuthService> logger, TimeProvider? timeProvider = null) : IKatmAuthService`
  - `httpClient` уже содержит `BaseAddress` = полный URL метода (так `RequestManagerService.CreateHttpClient` делает сейчас), запрос отправляется без `RequestUri`.

- [x] **Step 1: Добавить настройки** в `Domain/Common/Settings/CreditBureauReportApiOptions.cs` (после `PPassword`)

```csharp
        /// <summary>ТЗ 10.0: авторизация Bearer JWT вместо security в теле. false — поведение ТЗ 9.16.</summary>
        public bool UseJwtAuth { get; set; }
        public string AuthLoginUrl { get; set; } = null!;
```

- [x] **Step 2: Написать падающие тесты** — создать `Infrastructure.Tests/Services/HttpClients/KatmAuthServiceTests.cs`

```csharp
using System.Net;
using System.Text;
using Domain.Common.Settings;
using Infrastructure.Services.HttpClients;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Infrastructure.Tests.Services.HttpClients;

public class KatmAuthServiceTests
{
    private const string ApiUrl = "https://katm.test/katm-api/v1/credit/report";
    private const string RequestJson = """{"data":{"pClaimId":"c1"},"security":{"pLogin":"login","pPassword":"s3cr3t"}}""";

    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed record Sent(string Path, string? Authorization, string Body);

    /// <summary>Отвечает на /auth/login выданными токенами по очереди, на остальное — очередью apiStatuses.</summary>
    private sealed class FakeKatm(Queue<string> loginResponses, Queue<HttpStatusCode> apiStatuses) : HttpMessageHandler
    {
        public List<Sent> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri!;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new Sent(uri.AbsolutePath, request.Headers.Authorization?.ToString(), body));

            if (uri.AbsolutePath.EndsWith("/auth/login"))
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(loginResponses.Dequeue(), Encoding.UTF8, "application/json") };

            return new HttpResponseMessage(apiStatuses.Count > 0 ? apiStatuses.Dequeue() : HttpStatusCode.OK)
            {
                Content = new StringContent("""{"data":{"result":"05000"}}""", Encoding.UTF8, "application/json")
            };
        }
    }

    private static string Jwt(DateTimeOffset exp, string id)
    {
        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64("""{"alg":"HS256"}""")}.{B64($$"""{"exp":{{exp.ToUnixTimeSeconds()}},"jti":"{{id}}"}""")}.sig";
    }

    private static string LoginOk(string token) =>
        $$$"""{"error":{"errId":0,"errMsg":null},"success":true,"data":{"accessToken":"{{{token}}}","refreshToken":"r"}}""";

    private static (KatmAuthService Sut, FakeKatm Katm, HttpClient Api) Create(
        bool useJwt, FakeTime time, Queue<string> logins, params HttpStatusCode[] apiStatuses)
    {
        var katm = new FakeKatm(logins, new Queue<HttpStatusCode>(apiStatuses));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(katm, disposeHandler: false));
        var options = new CreditBureauReportApiOptions
        {
            HostAddress = "https://katm.test",
            AuthLoginUrl = "/katm-api/v1/auth/login",
            PLogin = "login",
            PPassword = "s3cr3t",
            UseJwtAuth = useJwt
        };
        var sut = new KatmAuthService(options, factory.Object, NullLogger<KatmAuthService>.Instance, time);
        var api = new HttpClient(katm, disposeHandler: false) { BaseAddress = new Uri(ApiUrl) };
        return (sut, katm, api);
    }

    [Fact]
    public async Task SendPostAsync_WhenJwtDisabled_SendsBodyAsIsWithoutLoginOrHeader()
    {
        var (sut, katm, api) = Create(useJwt: false, new FakeTime(), new Queue<string>());

        using var response = await sut.SendPostAsync(api, RequestJson, CancellationToken.None);

        var sent = Assert.Single(katm.Requests);
        Assert.Null(sent.Authorization);
        Assert.Equal(RequestJson, sent.Body);
    }

    [Fact]
    public async Task SendPostAsync_WhenJwtEnabled_LogsInOnceAddsBearerAndStripsSecurity()
    {
        var time = new FakeTime();
        var token = Jwt(time.Now.AddHours(1), "t1");
        var (sut, katm, api) = Create(useJwt: true, time, new Queue<string>([LoginOk(token)]));

        using (await sut.SendPostAsync(api, RequestJson, CancellationToken.None)) { }
        using (await sut.SendPostAsync(api, RequestJson, CancellationToken.None)) { }

        Assert.Equal(3, katm.Requests.Count);
        Assert.Equal("/katm-api/v1/auth/login", katm.Requests[0].Path);
        Assert.Equal("""{"login":"login","password":"s3cr3t"}""", katm.Requests[0].Body);
        foreach (var apiCall in katm.Requests.Skip(1))
        {
            Assert.Equal($"Bearer {token}", apiCall.Authorization);
            Assert.Equal("""{"data":{"pClaimId":"c1"}}""", apiCall.Body);
        }
    }

    [Fact]
    public async Task SendPostAsync_WhenTokenNearExpiry_LogsInAgain()
    {
        var time = new FakeTime();
        var first = Jwt(time.Now.AddHours(1), "t1");
        var second = Jwt(time.Now.AddHours(2), "t2");
        var (sut, katm, api) = Create(useJwt: true, time, new Queue<string>([LoginOk(first), LoginOk(second)]));

        using (await sut.SendPostAsync(api, RequestJson, CancellationToken.None)) { }
        time.Now = time.Now.AddMinutes(59.5); // внутри минутного запаса до exp
        using (await sut.SendPostAsync(api, RequestJson, CancellationToken.None)) { }

        Assert.Equal(2, katm.Requests.Count(r => r.Path.EndsWith("/auth/login")));
        Assert.Equal($"Bearer {second}", katm.Requests.Last().Authorization);
    }

    [Fact]
    public async Task SendPostAsync_WhenLoginFails_ThrowsWithKatmErrorMessage()
    {
        const string failed = """{"error":{"errId":401,"errMsg":"Неверный логин или пароль"},"success":false,"data":null}""";
        var (sut, _, api) = Create(useJwt: true, new FakeTime(), new Queue<string>([failed]));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendPostAsync(api, RequestJson, CancellationToken.None));

        Assert.Contains("Неверный логин или пароль", ex.Message);
        Assert.DoesNotContain("s3cr3t", ex.Message);
    }
}
```

- [x] **Step 3: Запустить, убедиться что падают**

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~KatmAuthServiceTests"`
Expected: ошибка компиляции `The type or namespace name 'KatmAuthService' could not be found`.

- [x] **Step 4: Реализация** — создать `Infrastructure/Services/HttpClients/KatmAuthService.cs`

```csharp
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
```

- [x] **Step 5: Запустить тесты**

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~KatmAuthServiceTests"`
Expected: PASS, 4 теста.

- [x] **Step 6: Commit**

```bash
git add Domain/Common/Settings/CreditBureauReportApiOptions.cs Infrastructure/Services/HttpClients/KatmAuthService.cs Infrastructure.Tests/Services/HttpClients/KatmAuthServiceTests.cs
git commit -m "feat: add KatmAuthService for KATM JWT bearer auth (TZ 10.0)"
```

---

### Task 3: Повтор при HTTP 401 (токен отозван до `exp`)

**Files:**
- Modify: `Infrastructure/Services/HttpClients/KatmAuthService.cs` (метод `SendPostAsync` + новый `Invalidate`)
- Test: `Infrastructure.Tests/Services/HttpClients/KatmAuthServiceTests.cs`

**Interfaces:**
- Consumes: `KatmAuthService`, тестовые `Create`, `Jwt`, `LoginOk`, `FakeTime` из Task 2.
- Produces: поведение `SendPostAsync`: при 401 перелогин и **один** повтор; второй 401 возвращается вызывающему как есть.

- [x] **Step 1: Написать падающие тесты** (дописать в `KatmAuthServiceTests`)

```csharp
    [Fact]
    public async Task SendPostAsync_WhenApiReturns401_ReloginsAndRetriesOnce()
    {
        var time = new FakeTime();
        var first = Jwt(time.Now.AddHours(1), "t1");
        var second = Jwt(time.Now.AddHours(1), "t2");
        var (sut, katm, api) = Create(useJwt: true, time, new Queue<string>([LoginOk(first), LoginOk(second)]),
            HttpStatusCode.Unauthorized, HttpStatusCode.OK);

        using var response = await sut.SendPostAsync(api, RequestJson, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, katm.Requests.Count(r => r.Path.EndsWith("/auth/login")));
        Assert.Equal($"Bearer {second}", katm.Requests.Last().Authorization);
    }

    [Fact]
    public async Task SendPostAsync_WhenApiReturns401Twice_ReturnsSecond401WithoutLooping()
    {
        var time = new FakeTime();
        var (sut, katm, api) = Create(useJwt: true, time,
            new Queue<string>([LoginOk(Jwt(time.Now.AddHours(1), "t1")), LoginOk(Jwt(time.Now.AddHours(1), "t2"))]),
            HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized);

        using var response = await sut.SendPostAsync(api, RequestJson, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(4, katm.Requests.Count); // login, api, login, api
    }
```

- [x] **Step 2: Запустить, убедиться что падают**

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~KatmAuthServiceTests.SendPostAsync_WhenApiReturns401"`
Expected: FAIL — `Assert.Equal() Failure: Expected: OK, Actual: Unauthorized` и `Expected: 4, Actual: 2`.

- [x] **Step 3: Реализация** — в `KatmAuthService.cs` добавить `using System.Net;` и заменить ветку JWT в `SendPostAsync`:

```csharp
            var body = jsonData.RemoveSecurity();
            var token = await GetAccessTokenAsync(cancellationToken);
            var response = await PostAsync(httpClient, body, token, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            // Токен мог быть отозван на стороне КАТМ раньше exp — перелогин и один повтор.
            logger.LogWarning("КАТМ вернул 401, повторный логин и повтор запроса");
            response.Dispose();
            Invalidate(token);
            token = await GetAccessTokenAsync(cancellationToken);
            return await PostAsync(httpClient, body, token, cancellationToken);
```

и метод (после `IsFresh`):

```csharp
        // Сбрасываем только тот токен, который получил 401: параллельный поток мог уже взять новый.
        private void Invalidate(string token)
        {
            var cached = _cached;
            if (cached?.Value == token)
                Interlocked.CompareExchange(ref _cached, null, cached);
        }
```

- [x] **Step 4: Запустить тесты**

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~KatmAuthServiceTests"`
Expected: PASS, 6 тестов.

- [x] **Step 5: Commit**

```bash
git add Infrastructure/Services/HttpClients/KatmAuthService.cs Infrastructure.Tests/Services/HttpClients/KatmAuthServiceTests.cs
git commit -m "feat: re-login and retry once on KATM 401"
```

---

### Task 4: Подключить `KatmAuthService` в `RequestManagerService`, DI и конфиг

**Files:**
- Modify: `Infrastructure/Services/HttpClients/RequestManagerService.cs:15-22, 76, 234, 291-298`
- Modify: `Infrastructure/Services/CreditBureauReportServices/Handlers/DependencyInjection.cs` (после регистрации `IRequestManagerService`)
- Modify: `CreditBureauService/appsettings.json` (секция `CreditBureauReportApiOptions`)

**Interfaces:**
- Consumes: `IKatmAuthService.SendPostAsync(HttpClient, string, CancellationToken)` из Task 2/3.
- Produces: сигнатуры `IRequestManagerService` **не меняются**, вызывающий код не трогаем.

Почему без нового теста: у `RequestManagerService` нет тестов, а он создаёт `HttpClient` через `new`. Логика авторизации покрыта в Task 2/3. Здесь меняются только три строки вызова. Проверка — сборка, полный прогон тестов и Task 5 на TEST-стенде.

- [x] **Step 1: Конструктор** — в `RequestManagerService.cs` добавить параметр и поле:

```csharp
    public class RequestManagerService(LogWriter logWriter, ILogger<RequestManagerService> logger, IHelperRepository repository, IRequestManagerRepository requestManagerRepository, ITelegramNotificationService telegramNotificationService, ICreditBureauReportRepository creditBureauReportRepository, IKatmAuthService katmAuthService) : IRequestManagerService
    {
        ...
        private readonly ICreditBureauReportRepository _creditBureauReportRepository = creditBureauReportRepository;
        private readonly IKatmAuthService _katmAuthService = katmAuthService;
```

- [x] **Step 2: Перегрузка CI-017** (строка 76) — заменить

```csharp
                httpResponseMessage = await httpClient.SendAsync(request, cancellationToken);
```

на

```csharp
                // request ниже используется только для логов — в нём нет заголовка с токеном.
                httpResponseMessage = await _katmAuthService.SendPostAsync(httpClient, jsonData, cancellationToken);
```

- [x] **Step 3: Перегрузка CI-001…CI-023** (строка 234) — заменить

```csharp
                        httpResponseMessage = await httpClient.SendAsync(request, cancellationToken);
```

на

```csharp
                        httpResponseMessage = await _katmAuthService.SendPostAsync(httpClient, jsonData, cancellationToken);
```

и удалить ставшую ненужной пересборку запроса в `catch` (строки 291–298, `SendPostAsync` создаёт новый `HttpRequestMessage` на каждую попытку):

```csharp
                        // HttpRequestMessage can only be sent once — rebuild content for retry
                        request.Dispose();
                        request = new HttpRequestMessage
                        {
                            Content = new StringContent(jsonData, Encoding.UTF8, "application/json"),
                            Method = HttpMethod.Post
                        };
                        request.Content.Headers.ContentType!.CharSet = string.Empty;
```

Остаются `await Task.Delay(delay, cancellationToken);` и логирование перед ним.

Ошибка логина (`InvalidOperationException` из `KatmAuthService`) попадает в существующие `catch`: в CI-017 это общий `catch (Exception ex)` → Telegram, `InsertLog`, комментарий в `Loan_History_KB`; в CI-001…023 — `catch (Exception ex) when (ex != lastException)` → Telegram и `InsertRequestLog`. Новая обработка не нужна.

- [x] **Step 4: DI** — в `Infrastructure/Services/CreditBureauReportServices/Handlers/DependencyInjection.cs` после `services.AddSingleton<IRequestManagerService, RequestManagerService>();`:

```csharp
            services.AddSingleton<IKatmAuthService, KatmAuthService>();
```

(`IHttpClientFactory` уже есть: `services.AddHttpClient()` в `CreditBureauService/DependencyInjection.cs`. Singleton обязателен, иначе кэш токена не переживёт запрос.)

- [x] **Step 5: Конфиг** — в `CreditBureauService/appsettings.json`, секция `CreditBureauReportApiOptions`, после `"PPassword"`:

```json
    "UseJwtAuth": false,
    "AuthLoginUrl": "/katm-api/v1/auth/login",
```

- [x] **Step 6: Сборка и полный прогон тестов**

Run: `dotnet build CreditBureauService.sln`
Expected: `Build succeeded`, 0 errors.

Run: `dotnet test Infrastructure.Tests/Infrastructure.Tests.csproj`
Expected: все тесты PASS (старые не затронуты: `UseJwtAuth=false` по умолчанию, контракты не менялись).

- [x] **Step 7: Commit**

```bash
git add Infrastructure/Services/HttpClients/RequestManagerService.cs Infrastructure/Services/CreditBureauReportServices/Handlers/DependencyInjection.cs CreditBureauService/appsettings.json
git commit -m "feat: route KATM requests through KatmAuthService behind UseJwtAuth flag"
```

---

### Task 5: Проверка на TEST-стенде КАТМ и переключение

**Files:**
- Modify (только на стенде, не коммитить): `appsettings.json` развёрнутого сервиса

- [ ] **Step 1: Закрыть открытые вопросы 1–3** по Swagger КАТМ или у поддержки. Если имя поля не `accessToken`, поправить `SelectToken("data.accessToken")` и `LoginOk` в тестах. Если путь без `/v1`, поправить только `appsettings.json`.

- [ ] **Step 2: Деплой на TEST** с `"HostAddress": "https://testapi.infokredit.uz"` (и `CreditBureauApiOptions.HostAddress` тоже), `"UseJwtAuth": true`. Запустить сервис.

- [ ] **Step 3: Проверить логи**
  - в консоли/ILogger: `КАТМ: получен новый access-токен` — **один раз** за час, а не на каждый запрос;
  - `RequestManager.txt`: ответы со статусом 200 и `result` = `05000` для CI-001/CI-004/CI-017;
  - в `RequestBody` логов нет блока `security`, нигде в логах нет строки `Bearer`.

- [ ] **Step 4: Отказ логина**: временно указать неверный `PPassword`. Ожидается сообщение в Telegram с текстом ошибки КАТМ и запись в `RequestLogs`, без падения воркера. Вернуть пароль.

- [ ] **Step 5: Прод**: 1 декабря 2026 (дата действия ТЗ 10.0) выставить `"UseJwtAuth": true` в прод-конфиге и перезапустить службу. Откат — `false` и перезапуск.

---

## После переключения (отдельная задача, не в этом плане)

Когда 10.0 стабильно работает в проде, удалить `UseJwtAuth`, `RequestSecurity`, `BaseRequest.Security`, `BaseRequestForCreditApplications.Security`, `RemoveSecurity` и параметр `RequestSecurity` из 16 обработчиков и тестов. До переключения это делать нельзя: без флага нет отката на ТЗ 9.16.
