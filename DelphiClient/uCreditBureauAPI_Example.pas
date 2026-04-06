{*******************************************************************************
  Пример использования модуля uCreditBureauAPI
  Демонстрация вызова API отправки отчетов CI-015, CI-016, CI-018 за период
*******************************************************************************}
unit uCreditBureauAPI_Example;

interface

uses
  System.Classes, System.SysUtils, Vcl.Forms, Vcl.StdCtrls, Vcl.ExtCtrls,
  uCreditBureauAPI;

type
  TfrmCreditBureauSend = class(TForm)
    pnlMain: TPanel;
    lblStartDate: TLabel;
    lblEndDate: TLabel;
    dtpStartDate: TDateTimePicker;
    dtpEndDate: TDateTimePicker;
    cbCI015: TCheckBox;
    cbCI016: TCheckBox;
    cbCI018: TCheckBox;
    btnSend: TButton;
    btnHealthCheck: TButton;
    mmLog: TMemo;
    pnlStatus: TPanel;
    lblStatus: TLabel;
    procedure FormCreate(Sender: TObject);
    procedure btnHealthCheckClick(Sender: TObject);
    procedure btnSendClick(Sender: TObject);
  private
    FCreditBureauAPI: TCreditBureauAPI;
    procedure Log(const Msg: string);
    procedure LogResult(const Result: TSendReportResult);
  public
    { Деструктор }
    destructor Destroy; override;
  end;

var
  frmCreditBureauSend: TfrmCreditBureauSend;

implementation

{$R *.dfm}

{ TfrmCreditBureauSend }

procedure TfrmCreditBureauSend.FormCreate(Sender: TObject);
begin
  { Инициализация API клиента }
  { URL сервера - измените на ваш адрес }
  FCreditBureauAPI := TCreditBureauAPI.Create('http://localhost:5000', 300000);
  
  { Установка дат по умолчанию - текущий месяц }
  dtpStartDate.Date := EncodeDate(YearOf(Now), MonthOf(Now), 1);
  dtpEndDate.Date := Now;
  
  { Чекбоксы по умолчанию }
  cbCI015.Checked := True;
  cbCI016.Checked := True;
  cbCI018.Checked := True;
  
  Log('Клиент инициализирован. URL: ' + FCreditBureauAPI.BaseUrl);
end;

destructor TfrmCreditBureauSend.Destroy;
begin
  FCreditBureauAPI.Free;
  inherited Destroy;
end;

procedure TfrmCreditBureauSend.Log(const Msg: string);
begin
  mmLog.Lines.Add(FormatDateTime('dd.mm.yyyy hh:nn:ss', Now) + '  ' + Msg);
end;

procedure TfrmCreditBureauSend.LogResult(const Result: TSendReportResult);
var
  CiName: string;
  CiResult: TCiResult;
begin
  Log('');
  Log('=== Результат ===');
  Log('Успех: ' + BoolToStr(Result.Success, True));
  Log('Сообщение: ' + Result.Message);
  
  if Result.Results.Count > 0 then
  begin
    Log('');
    Log('Детализация по CI-кодам:');
    for CiName in Result.Results.Keys do
    begin
      CiResult := Result.Results[CiName];
      Log(Format('  %s: Обработано=%d, Успешно=%d, Ошибок=%d',
        [CiName, CiResult.Processed, CiResult.Success, CiResult.Error]));
    end;
  end;
  Log('==================');
  Log('');
end;

procedure TfrmCreditBureauSend.btnHealthCheckClick(Sender: TObject);
begin
  Log('Проверка доступности сервиса...');
  if FCreditBureauAPI.HealthCheck then
    Log('✓ Сервис доступен')
  else
    Log('✗ Сервис недоступен');
end;

procedure TfrmCreditBureauSend.btnSendClick(Sender: TObject);
var
  CiCodes: TArray<Integer>;
  CiCount: Integer;
  SendResult: TSendReportResult;
begin
  { Формирование списка CI-кодов }
  CiCount := 0;
  if cbCI015.Checked then Inc(CiCount);
  if cbCI016.Checked then Inc(CiCount);
  if cbCI018.Checked then Inc(CiCount);
  
  if CiCount = 0 then
  begin
    ShowMessage('Выберите хотя бы один тип отчета!');
    Exit;
  end;
  
  SetLength(CiCodes, CiCount);
  CiCount := 0;
  if cbCI015.Checked then
  begin
    CiCodes[CiCount] := 15;
    Inc(CiCount);
  end;
  if cbCI016.Checked then
  begin
    CiCodes[CiCount] := 16;
    Inc(CiCount);
  end;
  if cbCI018.Checked then
  begin
    CiCodes[CiCount] := 18;
    Inc(CiCount);
  end;
  
  { Отправка запроса }
  Log('');
  Log('Отправка отчетов за период с ' + 
    DateToStr(dtpStartDate.Date) + ' по ' + DateToStr(dtpEndDate.Date));
  Log('CI-коды: ' + CiCodesToString(CiCodes));
  Log('');
  
  pnlStatus.Color := clYellow;
  lblStatus.Caption := 'Отправка...';
  Application.ProcessMessages;
  
  try
    SendResult := FCreditBureauAPI.SendReportsByPeriod(
      dtpStartDate.Date,
      dtpEndDate.Date,
      CiCodes
    );
    
    if SendResult.Success then
    begin
      pnlStatus.Color := clGreen;
      lblStatus.Caption := 'Успешно отправлено';
    end
    else
    begin
      pnlStatus.Color := clRed;
      lblStatus.Caption := 'Ошибка отправки';
    end;
    
    LogResult(SendResult);
    
  except
    on E: Exception do
    begin
      pnlStatus.Color := clRed;
      lblStatus.Caption := 'Критическая ошибка';
      Log('Критическая ошибка: ' + E.Message);
    end;
  end;
end;

end.
