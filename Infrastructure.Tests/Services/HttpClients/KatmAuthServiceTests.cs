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
    public async Task SendPostAsync_WhenLoginReturnsAccesTokenSpelling_UsesIt()
    {
        var time = new FakeTime();
        var token = Jwt(time.Now.AddHours(1), "t1");
        var login = LoginOk(token).Replace("\"accessToken\"", "\"accesToken\"");
        var (sut, katm, api) = Create(useJwt: true, time, new Queue<string>([login]));

        using (await sut.SendPostAsync(api, RequestJson, CancellationToken.None)) { }

        Assert.Equal($"Bearer {token}", katm.Requests.Last().Authorization);
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
}
