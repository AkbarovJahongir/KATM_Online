# API для отправки отчетов CI-015, CI-016, CI-018 за период

## Описание

Данный API предназначен для вызова из АБС (написанной на Delphi) для периодической отправки отчетов в кредитное бюро.

## Базовый URL

```
http://localhost:5000
```

> **Примечание:** Порт может быть изменён в конфигурации приложения

---

## 1. Health Check (Проверка доступности)

### Запрос

```http
GET /api/creditbureau/health
```

### Ответ

```json
{
  "status": "OK",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## 2. Отправка отчетов за период

### Запрос

```http
POST /api/creditbureau/send-by-period
Content-Type: application/json
```

### Тело запроса

```json
{
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "ciCodes": [15, 16, 18]
}
```

### Параметры

| Параметр   | Тип       | Обязательный | Описание                          |
|------------|-----------|--------------|-----------------------------------|
| startDate  | DateTime  | Да           | Дата начала периода (YYYY-MM-DD)  |
| endDate    | DateTime  | Да           | Дата окончания периода (YYYY-MM-DD) |
| ciCodes    | int[]     | Да           | Массив CI-кодов для отправки (15, 16, 18) |

### Поддерживаемые CI-коды

- **15** - CI-015: Сведения об остатках на счетах
- **16** - CI-016: Сведения о платежных документах
- **18** - CI-018: Сведения о статусе счетов

### Успешный ответ (200 OK)

```json
{
  "success": true,
  "message": "Отчеты отправлены успешно",
  "results": {
    "CI-015": {
      "processed": 100,
      "success": 95,
      "error": 5,
      "pending": 0
    },
    "CI-016": {
      "processed": 150,
      "success": 145,
      "error": 5,
      "pending": 0
    },
    "CI-018": {
      "processed": 80,
      "success": 78,
      "error": 2,
      "pending": 0
    }
  }
}
```

### Ответ с ошибкой (500 Internal Server Error)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "Ошибка при отправке отчетов",
  "status": 500,
  "detail": "Текст ошибки"
}
```

---

## Пример вызова из Delphi

### Используя Indy (TIdHTTP)

```delphi
uses
  IdHTTP, JsonDataObjects;

procedure SendCreditBureauReports;
var
  Http: TIdHTTP;
  RequestJson, ResponseJson: TJsonObject;
  Response: string;
begin
  Http := TIdHTTP.Create(nil);
  try
    Http.Request.ContentType := 'application/json';
    Http.Request.Accept := 'application/json';
    
    // Формирование запроса
    RequestJson := TJsonObject.Create;
    try
      RequestJson['startDate'] := '2024-01-01';
      RequestJson['endDate'] := '2024-01-31';
      RequestJson['ciCodes'].Add(15);
      RequestJson['ciCodes'].Add(16);
      RequestJson['ciCodes'].Add(18);
      
      // Отправка POST запроса
      Response := Http.Post(
        'http://localhost:5000/api/creditbureau/send-by-period',
        RequestJson.ToString
      );
      
      // Парсинг ответа
      ResponseJson := TJsonObject.Parse(Response) as TJsonObject;
      try
        if ResponseJson['success'].AsBoolean then
        begin
          // Обработка успешного ответа
          WriteLn('Отчеты отправлены успешно');
          // Можно получить статистику по каждому CI-коду
        end
        else
        begin
          WriteLn('Ошибка: ' + ResponseJson['message'].AsString);
        end;
      finally
        ResponseJson.Free;
      end;
    finally
      RequestJson.Free;
    end;
  except
    on E: Exception do
    begin
      WriteLn('Ошибка HTTP: ' + E.Message);
    end;
  end;
  Http.Free;
end;
```

### Используя REST.Client (TRESTClient)

```delphi
uses
  REST.Client, REST.Response.Adapter, Data.DBXJSON;

procedure SendCreditBureauReports;
var
  RESTClient: TRESTClient;
  RESTRequest: TRESTRequest;
  RESTResponse: TRESTResponse;
begin
  RESTClient := TRESTClient.Create('http://localhost:5000');
  RESTRequest := TRESTRequest.Create(nil);
  RESTResponse := TRESTResponse.Create(nil);
  try
    RESTRequest.Client := RESTClient;
    RESTRequest.Response := RESTResponse;
    
    RESTRequest.Resource := 'api/creditbureau/send-by-period';
    RESTRequest.Method := rmPOST;
    
    // Добавление параметров
    RESTRequest.Params.AddItem('startDate', '2024-01-01', pkGETorPOST);
    RESTRequest.Params.AddItem('endDate', '2024-01-31', pkGETorPOST);
    
    // Тело запроса (JSON)
    RESTRequest.Body.Add('{ "startDate": "2024-01-01", "endDate": "2024-01-31", "ciCodes": [15, 16, 18] }',
      ctJSON, 'application/json');
    
    // Выполнение запроса
    RESTRequest.Execute;
    
    if RESTResponse.StatusCode = 200 then
    begin
      // Обработка успешного ответа
      WriteLn('Ответ: ' + RESTResponse.Content);
    end
    else
    begin
      WriteLn('Ошибка HTTP: ' + IntToStr(RESTResponse.StatusCode));
    end;
  finally
    RESTRequest.Free;
    RESTResponse.Free;
    RESTClient.Free;
  end;
end;
```

---

## Swagger UI

Для просмотра документации в формате Swagger откройте в браузере:

```
http://localhost:5000/swagger
```

> Swagger UI доступен только в режиме разработки (Development)

---

## Конфигурация

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

### Изменение порта

Откройте `appsettings.json` и измените значение `Url` в секции `Kestrel.Endpoints.Http`.

---

## Запуск сервиса

### Как Windows Service

```bash
# Установка сервиса
sc create CreditBureau binPath= "C:\Path\To\CreditBureau.exe"

# Запуск сервиса
net start CreditBureau

# Остановка сервиса
net stop CreditBureau
```

### Как консольное приложение (для тестирования)

```bash
dotnet run --project CreditBureau.csproj
```

---

## Логи

Логи работы сервиса записываются в папку, указанную в конфигурации:

```
logs/
  CreditRegistrationAgreement.txt
  CreditRegistrationAccountStatus.txt
  ...
```

---

## Поддержка

По вопросам обращайтесь к разработчикам проекта.
