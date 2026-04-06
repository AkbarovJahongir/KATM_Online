object frmCreditBureauSend: TfrmCreditBureauSend
  Left = 0
  Top = 0
  Caption = 'Отправка отчетов в кредитное бюро (CI-015, CI-016, CI-018)'
  ClientHeight = 544
  ClientWidth = 624
  Color = clBtnFace
  Font.Charset = DEFAULT_CHARSET
  Font.Color = clWindowText
  Font.Height = -12
  Font.Name = 'Segoe UI'
  Font.Style = []
  OnCreate = FormCreate
  TextHeight = 15
  object pnlMain: TPanel
    Left = 0
    Top = 0
    Width = 624
    Height = 121
    Align = alTop
    BevelOuter = bvNone
    Padding.Left = 10
    Padding.Top = 10
    Padding.Right = 10
    Padding.Bottom = 10
    TabOrder = 0
    object lblStartDate: TLabel
      Left = 10
      Top = 15
      Width = 69
      Height = 15
      Caption = 'Дата начала:'
    end
    object lblEndDate: TLabel
      Left = 10
      Top = 55
      Width = 66
      Height = 15
      Caption = 'Дата конца:'
    end
    object dtpStartDate: TDateTimePicker
      Left = 110
      Top = 10
      Width = 200
      Height = 23
      Date = 45387.000000000000000000
      Time = 0.628451307870375000
      TabOrder = 0
    end
    object dtpEndDate: TDateTimePicker
      Left = 110
      Top = 50
      Width = 200
      Height = 23
      Date = 45387.000000000000000000
      Time = 0.628451307870375000
      TabOrder = 1
    end
    object cbCI015: TCheckBox
      Left = 350
      Top = 12
      Width = 150
      Height = 17
      Caption = 'CI-015 (Остатки на счетах)'
      Checked = True
      State = cbChecked
      TabOrder = 2
    end
    object cbCI016: TCheckBox
      Left = 350
      Top = 35
      Width = 180
      Height = 17
      Caption = 'CI-016 (Платежные документы)'
      Checked = True
      State = cbChecked
      TabOrder = 3
    end
    object cbCI018: TCheckBox
      Left = 350
      Top = 58
      Width = 150
      Height = 17
      Caption = 'CI-018 (Статусы счетов)'
      Checked = True
      State = cbChecked
      TabOrder = 4
    end
    object btnHealthCheck: TButton
      Left = 10
      Top = 85
      Width = 120
      Height = 25
      Caption = 'Проверка связи'
      TabOrder = 5
      OnClick = btnHealthCheckClick
    end
    object btnSend: TButton
      Left = 140
      Top = 85
      Width = 150
      Height = 25
      Caption = 'Отправить отчеты'
      TabOrder = 6
      OnClick = btnSendClick
    end
  end
  object pnlStatus: TPanel
    Left = 0
    Top = 121
    Width = 624
    Height = 40
    Align = alTop
    BevelOuter = bvNone
    Color = clWhite
    ParentBackground = False
    TabOrder = 1
    object lblStatus: TLabel
      Left = 10
      Top = 12
      Width = 77
      Height = 15
      Caption = 'Готов к работе'
      Font.Charset = DEFAULT_CHARSET
      Font.Color = clWindowText
      Font.Height = -12
      Font.Name = 'Segoe UI'
      Font.Style = [fsBold]
      ParentFont = False
    end
  end
  object mmLog: TMemo
    Left = 0
    Top = 161
    Width = 624
    Height = 383
    Align = alClient
    Font.Charset = RUSSIAN_CHARSET
    Font.Color = clWindowText
    Font.Height = -12
    Font.Name = 'Consolas'
    Font.Style = []
    ParentFont = False
    ReadOnly = True
    ScrollBars = ssVertical
    TabOrder = 2
  end
end
