using Infrastructure.Services.CreditBureauReportServices;
using Infrastructure.Services.CreditBureauReportServices.Handlers;

namespace CreditBureau.Endpoints;

/// <summary>
/// Minimal API endpoints для отправки отчетов CI-015, CI-016, CI-018 за период
/// Вызывается из АБС (Delphi) через HTTP запросы
/// </summary>
public static class CreditBureauPeriodReportEndpoints
{
    /// <summary>
    /// Добавление endpoints в приложение
    /// </summary>
    public static void MapCreditBureauPeriodReportEndpoints(this WebApplication app)
    {
        // POST /api/creditbureau/send-by-period
        // Тело запроса: { "startDate": "2024-01-01", "endDate": "2024-01-31", "ciCodes": [15, 16, 18] }
        app.MapPost("/api/creditbureau/send-by-period", async (
            PeriodReportRequest request,
            ICreditBureauReportService reportService,
            ILogger<PeriodReportRequest> logger,
            CancellationToken cancellationToken) =>
        {
            try
            {
                logger.LogInformation(
                    "Получен запрос на отправку отчетов за период с {StartDate} по {EndDate}. Отчеты: {CiCodes}",
                    request.StartDate, request.EndDate, string.Join(", ", request.CiCodes));

                var results = new Dictionary<int, CiProcessingResult>();

                // Обработка каждого запрошенного CI-кода
                foreach (var ciCode in request.CiCodes)
                {
                    if (ciCode is not 15 and not 16 and not 18)
                    {
                        logger.LogWarning("CI-{CiCode} не поддерживается для отправки по периоду", ciCode);
                        continue;
                    }

                    CiProcessingResult result = ciCode switch
                    {
                        15 => await reportService.SendCi015ByPeriodAsync(request.StartDate, request.EndDate, cancellationToken),
                        16 => await reportService.SendCi016ByPeriodAsync(request.StartDate, request.EndDate, cancellationToken),
                        18 => await reportService.SendCi018ByPeriodAsync(request.StartDate, request.EndDate, cancellationToken),
                        _ => new CiProcessingResult()
                    };

                    results[ciCode] = result;
                    logger.LogInformation(
                        "CI-{CiCode} обработан. Всего: {Processed}, Успешно: {Success}, Ошибок: {Error}",
                        ciCode, result.Processed, result.Success, result.Error);
                }

                return Results.Ok(new PeriodReportResponse
                {
                    Success = true,
                    Message = "Отчеты отправлены успешно",
                    Results = results.ToDictionary(
                        k => $"CI-{k.Key:D3}",
                        v => new CiResult
                        {
                            Processed = v.Value.Processed,
                            Success = v.Value.Success,
                            Error = v.Value.Error,
                            Pending = v.Value.Pending
                        })
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при отправке отчетов за период: {Error}", ex.Message);
                return Results.Problem(
                    title: "Ошибка при отправке отчетов",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("SendCreditBureauReportsByPeriod")
        .WithOpenApi();

        // GET /api/creditbureau/health
        // Проверка доступности сервиса
        app.MapGet("/api/creditbureau/health", () =>
        {
            return Results.Ok(new { Status = "OK", Timestamp = DateTime.UtcNow });
        })
        .WithName("HealthCheck")
        .WithOpenApi();
    }

    /// <summary>
    /// Модель запроса для отправки отчетов за период
    /// </summary>
    public class PeriodReportRequest
    {
        /// <summary>
        /// Дата начала периода (формат: YYYY-MM-DD)
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Дата окончания периода (формат: YYYY-MM-DD)
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Список CI-кодов для отправки (15, 16, 18)
        /// </summary>
        public List<int> CiCodes { get; set; } = new();
    }

    /// <summary>
    /// Модель ответа
    /// </summary>
    public class PeriodReportResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, CiResult> Results { get; set; } = new();
    }

    /// <summary>
    /// Результат обработки одного CI-кода
    /// </summary>
    public class CiResult
    {
        public int Processed { get; set; }
        public int Success { get; set; }
        public int Error { get; set; }
        public int Pending { get; set; }
    }
}
