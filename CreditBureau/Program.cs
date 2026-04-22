using CreditBureau;
using CreditBureau.Endpoints;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging.EventLog;

var builder = WebApplication.CreateBuilder(args);
var isWindowsService = WindowsServiceHelpers.IsWindowsService();

if (isWindowsService)
{
    builder.Host.UseWindowsService();
}
else
{
    // Prevent Windows Event Log teardown errors during local console runs.
    builder.Logging.AddFilter<EventLogLoggerProvider>(_ => false);
}

var explicitUrls = builder.Configuration[WebHostDefaults.ServerUrlsKey];
var configuredHttpUrl = builder.Configuration["Kestrel:Endpoints:Http:Url"];

if (!isWindowsService && string.IsNullOrWhiteSpace(explicitUrls) && IsWildcardUrl(configuredHttpUrl))
{
    builder.WebHost.UseUrls(ToLocalhostUrl(configuredHttpUrl!));
}

// Добавление сервисов
builder.Services.AddHostedService<Asoki>();
builder.Services.AddHostedService<AsokiXml>();
builder.Services.AddHostedService<CreditBureauReportWorker>();
builder.Services.AddHostedService<StartupTelegramNotificationHostedService>();
builder.Services.AddCreditBureau(builder.Configuration);

// Добавление HTTP endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRequestTimeouts(options =>
{
    options.AddPolicy("ApiLongRunning", TimeSpan.FromMinutes(10));
});

var app = builder.Build();
app.Services.GetRequiredService<GlobalExceptionNotifier>().Register();

// Настройка HTTP пайплайна
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRequestTimeouts();

// Регистрация endpoints для отправки отчетов за период
app.MapCreditBureauPeriodReportEndpoints();

// Запуск Windows Service и HTTP сервера
app.Run();

static bool IsWildcardUrl(string? url) =>
    !string.IsNullOrWhiteSpace(url) &&
    (url.Contains("://*:", StringComparison.Ordinal) || url.Contains("://+:", StringComparison.Ordinal));

static string ToLocalhostUrl(string url)
{
    var localhostUrl = url.Replace("://*:", "://localhost:", StringComparison.Ordinal)
                          .Replace("://+:", "://localhost:", StringComparison.Ordinal);

    return new Uri(localhostUrl).GetLeftPart(UriPartial.Authority);
}
