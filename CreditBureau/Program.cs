using CreditBureau;
using CreditBureau.Endpoints;
using Infrastructure.Services.CreditBureauReportServices;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddHostedService<Asoki>();
builder.Services.AddHostedService<AsokiXml>();
builder.Services.AddHostedService<CreditBureauReportWorker>();
builder.Services.AddCreditBureau(builder.Configuration);

// Добавление HTTP endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Настройка HTTP пайплайна
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Регистрация endpoints для отправки отчетов за период
app.MapCreditBureauPeriodReportEndpoints();

// Запуск Windows Service и HTTP сервера
app.Run();