{*******************************************************************************
  Модуль для вызова API отправки отчетов в кредитное бюро (CI-015, CI-016, CI-018)
  Используется в АБС для периодической отправки отчетов
  
  Зависимости: Indy 10 (TIdHTTP, TIdJSONEncoder)
  
  Установка:
  1. Добавьте этот файл в ваш проект
  2. Убедитесь, что Indy 10 установлен
  3. Настройте URL сервера в константе CREDIT_BUREAU_API_URL
*******************************************************************************}
unit uCreditBureauAPI;

interface

uses
  System.Classes, System.SysUtils, System.JSON, IdHTTP, IdSSLOpenSSL;

type
  { Результат обработки одного CI-кода }
  TCiResult = record
    Processed: Integer;  // Всего обработано
    Success: Integer;    // Успешно
    Error: Integer;      // Ошибки
    Pending: Integer;    // В ожидании
  end;

  { Результат выполнения запроса }
  TSendReportResult = record
    Success: Boolean;
    Message: string;
    Results: TDictionary<string, TCiResult>;
  end;

  { Основной класс для работы с API }
  TCreditBureauAPI = class
  private
    FBaseUrl: string;
    FTimeout: Integer;
    FIdHTTP: TIdHTTP;
    FIOHandler: TIdSSLIOHandlerSocketOpenSSL;
    function BuildJsonRequest(const StartDate, EndDate: TDateTime; const CiCodes: TArray<Integer>): string;
    function ParseResponse(const ResponseJson: string): TSendReportResult;
  public
    constructor Create(const ABaseUrl: string = 'http://localhost:5000'; ATimeout: Integer = 300000);
    destructor Destroy; override;
    
    { Отправка отчетов за период }
    function SendReportsByPeriod(
      const StartDate, EndDate: TDateTime;
      const CiCodes: TArray<Integer>
    ): TSendReportResult;
    
    { Проверка доступности сервиса }
    function HealthCheck: Boolean;
    
    { Свойства }
    property BaseUrl: string read FBaseUrl write FBaseUrl;
    property Timeout: Integer read FTimeout write FTimeout;
  end;

{ Вспомогательные функции }
function DateTimeToISODate(const ADate: TDateTime): string;
function CiCodesToString(const CiCodes: TArray<Integer>): string;

implementation

{ TCreditBureauAPI }

constructor TCreditBureauAPI.Create(const ABaseUrl: string; ATimeout: Integer);
begin
  inherited Create;
  FBaseUrl := ABaseUrl;
  FTimeout := ATimeout;
  
  FIdHTTP := TIdHTTP.Create(nil);
  FIdHTTP.Request.ContentType := 'application/json';
  FIdHTTP.Request.Accept := 'application/json';
  FIdHTTP.ReadTimeout := FTimeout;
  FIdHTTP.ConnectTimeout := FTimeout;
  
  { Для HTTPS раскомментируйте:
  FIOHandler := TIdSSLIOHandlerSocketOpenSSL.Create(FIdHTTP);
  FIdHTTP.IOHandler := FIOHandler;
  FIOHandler.SSLOptions.Method := sslvTLSv1_2;
  FIOHandler.SSLOptions.Mode := sslmClient;
  }
end;

destructor TCreditBureauAPI.Destroy;
begin
  FIdHTTP.Free;
  if Assigned(FIOHandler) then
    FIOHandler.Free;
  inherited Destroy;
end;

function TCreditBureauAPI.BuildJsonRequest(
  const StartDate, EndDate: TDateTime;
  const CiCodes: TArray<Integer>
): string;
var
  JsonObject: TJSONObject;
  JsonArray: TJSONArray;
  CiCode: Integer;
begin
  JsonObject := TJSONObject.Create;
  try
    JsonObject.AddPair('startDate', TJSONString.Create(DateTimeToISODate(StartDate)));
    JsonObject.AddPair('endDate', TJSONString.Create(DateTimeToISODate(EndDate)));
    
    JsonArray := TJSONArray.Create;
    for CiCode in CiCodes do
      JsonArray.Add(CiCode);
    
    JsonObject.AddPair('ciCodes', JsonArray);
    
    Result := JsonObject.ToString;
  finally
    JsonObject.Free;
  end;
end;

function TCreditBureauAPI.ParseResponse(const ResponseJson: string): TSendReportResult;
var
  JsonObject: TJSONObject;
  ResultsObject: TJSONObject;
  JsonValue: TJsonValue;
  Pair: TJsonPair;
  CiResult: TCiResult;
  CiName: string;
begin
  Result.Success := False;
  Result.Message := '';
  Result.Results := TDictionary<string, TCiResult>.Create;
  
  try
    JsonObject := TJSONObject.ParseJSONValue(ResponseJson) as TJSONObject;
    if not Assigned(JsonObject) then
    begin
      Result.Message := 'Ошибка парсинга JSON ответа';
      Exit;
    end;
    
    try
      // Получаем поле success
      JsonValue := JsonObject.GetValue('success');
      if Assigned(JsonValue) and JsonValue is TJSONBool then
        Result.Success := TJSONBool(JsonValue).AsBoolean;
      
      // Получаем поле message
      JsonValue := JsonObject.GetValue('message');
      if Assigned(JsonValue) and JsonValue is TJSONString then
        Result.Message := TJSONString(JsonValue).Value;
      
      // Получаем результаты по CI-кодам
      JsonValue := JsonObject.GetValue('results');
      if Assigned(JsonValue) and JsonValue is TJSONObject then
      begin
        ResultsObject := TJSONObject(JsonValue);
        for Pair in ResultsObject do
        begin
          CiName := Pair.JsonString.Value;
          
          if Pair.JsonValue is TJSONObject then
          begin
            CiResult.Processed := TJSONObject(Pair.JsonValue).GetValue('processed', 0);
            CiResult.Success := TJSONObject(Pair.JsonValue).GetValue('success', 0);
            CiResult.Error := TJSONObject(Pair.JsonValue).GetValue('error', 0);
            CiResult.Pending := TJSONObject(Pair.JsonValue).GetValue('pending', 0);
            
            Result.Results.Add(CiName, CiResult);
          end;
        end;
      end;
    finally
      JsonObject.Free;
    end;
  except
    on E: Exception do
      Result.Message := 'Ошибка обработки ответа: ' + E.Message;
  end;
end;

function TCreditBureauAPI.SendReportsByPeriod(
  const StartDate, EndDate: TDateTime;
  const CiCodes: TArray<Integer>
): TSendReportResult;
var
  Url: string;
  RequestJson: string;
  ResponseJson: string;
begin
  Url := FBaseUrl + '/api/creditbureau/send-by-period';
  RequestJson := BuildJsonRequest(StartDate, EndDate, CiCodes);
  
  try
    ResponseJson := FIdHTTP.Post(Url, RequestJson);
    Result := ParseResponse(ResponseJson);
  except
    on E: EIdHTTPProtocolException do
    begin
      Result.Success := False;
      Result.Message := Format('HTTP ошибка %d: %s', [E.ErrorCode, E.Message]);
      Result.Results := TDictionary<string, TCiResult>.Create;
    end;
    on E: Exception do
    begin
      Result.Success := False;
      Result.Message := 'Ошибка соединения: ' + E.Message;
      Result.Results := TDictionary<string, TCiResult>.Create;
    end;
  end;
end;

function TCreditBureauAPI.HealthCheck: Boolean;
var
  Url: string;
  Response: string;
begin
  Url := FBaseUrl + '/api/creditbureau/health';
  try
    Response := FIdHTTP.Get(Url);
    Result := Pos('"status"', Response) > 0;
  except
    on E: Exception do
      Result := False;
  end;
end;

{ Вспомогательные функции }

function DateTimeToISODate(const ADate: TDateTime): string;
begin
  Result := FormatDateTime('yyyy-mm-dd', ADate);
end;

function CiCodesToString(const CiCodes: TArray<Integer>): string;
var
  I: Integer;
begin
  Result := '[';
  for I := 0 to High(CiCodes) do
  begin
    if I > 0 then
      Result := Result + ', ';
    Result := Result + IntToStr(CiCodes[I]);
  end;
  Result := Result + ']';
end;

end.
