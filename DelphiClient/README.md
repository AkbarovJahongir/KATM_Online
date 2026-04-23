# Интеграция с АБС на Delphi

## Обзор

Данный проект предоставляет HTTP API для вызова из АБС (на Delphi) для отправки отчетов в кредитное бюро за указанный период.

---

## 📋 Шаг 1: Настройка и запуск сервиса

### 1.1 Конфигурация

Откройте файл `CreditBureau/appsettings.json` и настройте порт:

```json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://*:5000"
    }
  }
}
```

> **Важно:** Для доступа из локальной сети используйте `http://*:5000`  
> Для доступа только с локальной машины: `http://localhost:5000`

### 1.2 Запуск сервиса

**Вариант A: Как Windows Service (рекомендуется)**

```powershell
# Установка сервиса
New-Service -Name "CreditBureau" -BinaryPathName "C:\Path\To\CreditBureau.exe" -DisplayName "Credit Bureau Service"

# Запуск
Start-Service CreditBureau

# Проверка статуса
Get-Service CreditBureau
```

**Вариант B: Как консольное приложение (для тестирования)**

```bash
dotnet run --project CreditBureau/CreditBureau.csproj
```

---

## 📋 Шаг 2: Интеграция в АБС на Delphi

### 2.1 Добавьте файлы в проект

Добавьте в ваш проект Delphi следующие файлы из папки `DelphiClient`:

1. `uCreditBureauAPI.pas` — основной модуль
2. `uCreditBureauAPI_Example.pas` — пример использования (опционально)
3. `uCreditBureauAPI_Example.dfm` — форма примера (опционально)

### 2.2 Минимальный пример вызова

```delphi
uses
  uCreditBureauAPI;

procedure SendReports;
var
  API: TCreditBureauAPI;
  CiCodes: TArray<Integer>;
  Result: TSendReportResult;
  CiName: string;
  CiResult: TCiResult;
begin
  API := TCreditBureauAPI.Create('http://localhost:5000');
  try
    { Формирование списка CI-кодов }
    SetLength(CiCodes, 3);
    CiCodes[0] := 15;  // CI-015
    CiCodes[1] := 16;  // CI-016
    CiCodes[2] := 18;  // CI-018
    
    { Отправка отчетов за период }
    Result := API.SendReportsByPeriod(
      EncodeDate(2024, 1, 1),  // Дата начала
      EncodeDate(2024, 1, 31), // Дата конца
      CiCodes
    );
    
    { Обработка результата }
    if Result.Success then
    begin
      ShowMessage('Отчеты отправлены успешно!');
      
      { Вывод статистики }
      for CiName in Result.Results.Keys do
      begin
        CiResult := Result.Results[CiName];
        WriteLn(Format('%s: Обработано=%d, Успешно=%d, Ошибок=%d',
          [CiName, CiResult.Processed, CiResult.Success, CiResult.Error]));
      end;
    end
    else
    begin
      ShowMessage('Ошибка: ' + Result.Message);
    end;
    
  finally
    API.Free;
  end;
end;
```

### 2.2 Проверка доступности сервиса

```delphi
var
  API: TCreditBureauAPI;
begin
  API := TCreditBureauAPI.Create('http://localhost:5000');
  try
    if API.HealthCheck then
      ShowMessage('Сервис доступен')
    else
      ShowMessage('Сервис недоступен');
  finally
    API.Free;
  end;
end;
```

---

## 📋 Шаг 3: Настройка брандмауэра

Если сервис должен быть доступен по сети:

### Windows Firewall

```powershell
# Открыть порт 5000
New-NetFirewallRule -DisplayName "Credit Bureau API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

---

## 📋 Шаг 4: Тестирование

### 4.1 Проверка Health Check

Откройте в браузере:
```
http://localhost:5000/api/creditbureau/health
```

Ожидаемый ответ:
```json
{
  "status": "OK",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### 4.2 Тест через curl

```bash
curl -X POST http://localhost:5000/api/creditbureau/send-by-period \
  -H "Content-Type: application/json" \
  -d "{\"startDate\":\"2024-01-01\",\"endDate\":\"2024-01-31\",\"ciCodes\":[15,16,18]}"
```

### 4.3 Swagger UI

Для просмотра документации в браузере:
```
http://localhost:5000/swagger
```

---

## 📋 Шаг 5: Использование в фоновом режиме

### Пример: Отправка отчетов каждый день в 23:00

Создайте форму с таймером:

```delphi
type
  TfrmAutoSend = class(TForm)
    Timer: TTimer;
    procedure TimerTimer(Sender: TObject);
  private
    FAPI: TCreditBureauAPI;
    procedure SendDailyReports;
  end;

procedure TfrmAutoSend.TimerTimer(Sender: TObject);
begin
  { Проверка времени }
  if (Time >= EncodeTime(23, 0, 0, 0)) and (Time < EncodeTime(23, 5, 0, 0)) then
  begin
    { Отправка только один раз в день }
    if FLastSendDate <> Date then
    begin
      SendDailyReports;
      FLastSendDate := Date;
    end;
  end;
end;

procedure TfrmAutoSend.SendDailyReports;
var
  CiCodes: TArray<Integer>;
  Result: TSendReportResult;
begin
  SetLength(CiCodes, 3);
  CiCodes[0] := 15;
  CiCodes[1] := 16;
  CiCodes[2] := 18;
  
  { Отправка за вчерашний день }
  Result := FAPI.SendReportsByPeriod(
    Date - 1,
    Date - 1,
    CiCodes
  );
  
  { Логирование результата }
  if Result.Success then
    LogToFile('Отчеты отправлены успешно')
  else
    LogToFile('Ошибка: ' + Result.Message);
end;
```

---

## 🔧 Решение проблем

### Ошибка: "Connection refused"

**Причина:** Сервис не запущен или порт заблокирован

**Решение:**
1. Проверьте, запущен ли сервис: `Get-Service CreditBureau`
2. Проверьте порт в `appsettings.json`
3. Проверьте брандмауэр

### Ошибка: "SSL/TLS error"

**Причина:** Проблемы с HTTPS

**Решение:**
1. Для тестирования используйте HTTP
2. Установите OpenSSL библиотеки для Delphi (libeay32.dll, ssleay32.dll)

### Ошибка: "Timeout"

**Причина:** Долгая обработка

**Решение:**
1. Увеличьте таймаут в конструкторе: `TCreditBureauAPI.Create('http://...', 600000)`
2. Проверьте логи сервиса

---

## 📊 Мониторинг

### Логи сервиса

Логи записываются в папку `logs/`:
- `CreditRegistrationAgreement.txt` — логи CI-015, CI-016
- `CreditRegistrationAccountStatus.txt` — логи CI-018

### Просмотр логов в реальном времени

```powershell
Get-Content logs\CreditRegistrationAgreement.txt -Wait -Tail 50
```

---

## 📞 Поддержка

При возникновении проблем:

1. Проверьте логи сервиса
2. Проверьте логи АБС
3. Убедитесь, что версии .NET и Delphi совместимы
4. Проверьте сетевое соединение

---

## 📝 Полный список API методов

| Метод | URL | Описание |
|-------|-----|----------|
| `GET` | `/api/creditbureau/health` | Проверка доступности |
| `POST` | `/api/creditbureau/send-by-period` | Отправка отчетов за период |

### Формат запроса

```json
{
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "ciCodes": [15, 16, 18]
}
```

### Формат ответа

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
    }
  }
}
```
