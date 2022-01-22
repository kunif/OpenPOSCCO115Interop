# OPOS version 1.15 コントロールをPOS for.NETから呼び出す代替手段

これはOPOS version 1.15 コントロールをPOS for.NETから呼び出すためのPOS for.NETのサービスオブジェクトです。  

- 2022-01-22 BugFix:以下のデバイスで未実装のプロパティ/メソッドがあった。
  - CashChanger
  - CheckScanner
  - FiscalPrinter
  - POSPower
  - POSPrinter
  - Scale
- Visual Studio 2022と.NET Framework 4.8に移行した。

POS for.NETには 既にLegacy COM Interopと呼ばれる、OPOSコントロールを呼び出す仕組みがPOS for.NET内部に組み込まれています。  
しかしこの仕組みには以下の課題があります。

- UnifiedPOS version 1.15で追加された以下の追加/変更に対応していません  
  - ElectronicValueReader/WriterデバイスへCATデバイス機能を追加  
  - ElectronicValueReader/WriterデバイスのServiceTypeプロパティへEVRW_ST_CATを追加  
  - FiscalPrinterデバイスのCountryCode, DateTypeプロパティへ値を追加  
- UnifiedPOSに定義された36種類のデバイスクラスのうち24種類しかサポートされていません(v1.14.1の場合)  
  サポートされていないデバイス:  
  - Belt
  - Bill Acceptor
  - Bill Dispenser
  - Biometrics
  - Coin Acceptor
  - Electronic Journal
  - Electronic Value Reader/Writer
  - Gate
  - Image Scanner
  - Item Dispenser
  - Lights
  - RFID Scanner
- OPOSのBinaryConversionプロパティの値を変更するとOPOSを直接呼び出した場合とは違った変換処理が必要になります  

これらを解決するために、以下の特徴を持つサービスオブジェクトを作成しました。

- UnifiedPOS version 1.15で追加されたインタフェースや定義をサポートしたPOS for.NET用拡張DLLを組み込みました
- UnifiedPOSに定義された36種類のデバイスクラスをすべてサポートしました
- OPOSのためのBinaryConversion処理は2種類に分けました  
  - POS for.NETでもOPOSでもstringのプロパティ/パラメータは何も処理せずそのまま通します
  - POS for.NETでbyte[]やBitmap等のプロパティ/パラメータはBinaryConversionの値に応じた変換処理を行います
- POS for.NETにおいてEnumとみなされているプロパティを読み取った際に、定義されていない値がOPOSから通知されたならば、PosControlExceptionを発生させ、その値を例外のErrorCodeExtendedプロパティに格納して通知します。
- 対応するOPOSデバイス名の情報は、POS for.NETのConfiguration.xmlファイルにて定義します

## 開発/実行環境

このプログラムの開発および実行には以下が必要です。

- Visual Studio 2022またはVisual Studio Community 2022 version 17.0.5 (開発のみ)  
- .NET framework 4.8  
- Microsoft Point of Service for .NET v1.14.1 (POS for.NET) : https://www.microsoft.com/en-us/download/details.aspx?id=55758  
- OpenPOS.Extension.Ver115 : https://github.com/kunif/OpenPOSExtensionVer115  
- Common Control Objects 1.15.000 : https://github.com/kunif/OPOS-CCO  
- 対象デバイスのOPOSサービスオブジェクト

このサービスオブジェクトの開発/実行には、Common Control Objects 1.15 の CCO と PIA および UnifiedPOS 1.15サポートPOS for.NET用拡張DLL が必要です。  
デバイスベンダのOPOSで対象デバイスの.ocxしかインストールされない場合や、CCOの 1.14.001およびそれ以前の版はサポートされないので入れ替えてください。  
Common Control Objects 1.15 の Install_CCOandPIA.bat にて CCO と PIA の両方をインストールしてください。  
UnifiedPOS 1.15サポートPOS for.NET用拡張DLLを登録してください。

## 実行環境へのインストール

実行環境へのインストールは以下の手順で行ってください。

- 適当なフォルダを作成し、 OpenPOS.CCO115Interop.dll をコピー  
  ドライブのルートではなく、かつフォルダのパス名は空白を含まず、0x7E以下の英数字のみで構成してください。  
  その方が問題発生の可能性が少ないでしょう。  

- POS for.NETレジストリのControlAssembliesキーに上記フォルダを値にして任意の名前で登録  
  例えば "AltCCOInterops"="C:\\\\POSforNET\\\\CCOInterop\\\\"  
  ただし、開発作業中はビルド時に処理の一環で自動的に登録されます。  
  対象キーの位置は以下です。  
  - 64bitOS: HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\POSfor.NET\\ControlAssemblies  
  - 32bitOS: HKEY_LOCAL_MACHINE\\SOFTWARE\\POSfor.NET\\ControlAssemblies  

## 設定

POS for.NETのposdm.exeプログラムを使用してデバイスエントリ作成およびプロパティ設定を行ってください。  

- posdmのADDDEVICEコマンドでデバイスエントリを作成  
  使用例: posdm ADDDEVICE OposEVRW1 /type:ElectronicValueRW /soname:"OpenPOS 1.15 ElectronicValueRW"  

  - 指定したデバイス名(例では"OposEVRW1")は"HardwarePath"の値として格納されます  
    他のPOS for.NETの名前やOPOSの名前と重ならないユニークな名前を指定してください  
  - /soname:には頭に"OpenPOS 1.15 "を付けてダブルクォーテーションで囲んだデバイスクラス名を指定してください  
    例えば "OpenPOS 1.15 CashDrawer", "OpenPOS 1.15 POSPrinter", "OpenPOS 1.15 Scanner" 等  

- posdmのADDPROPERTYコマンドで使用するOPOSデバイス名を設定  
  使用例: posdm ADDPROPERTY OposDeviceName VenderName_ModelName /type:ElectronicValueRW /soname:"OpenPOS 1.15 ElectronicValueRW" /path:OposEVRW1  

  - 設定対象のプロパティ名は "OposDeviceName"  
  - 設定する値(例では"VenderName_ModelName")はOPOSレジストリに存在するデバイスネームキーまたは論理デバイス名を指定してください  

上記使用例実行後のConfiguration.xmlの対象デバイスエントリ

    <ServiceObject Name="OpenPOS 1.15 ElectronicValueRW" Type="ElectronicValueRW">  
      <Device HardwarePath="" Enabled="yes" PnP="no" />  
      <Device HardwarePath="OposEVRW1" Enabled="yes" PnP="no">  
        <Property Name="OposDeviceName" Value="VenderName_ModelName" />  
      </Device>  
    </ServiceObject>

## 呼び出し方

上記設定の使用例で作成したデバイスエントリを呼び出す手順と例を示します。

- プロジェクトの参照に"OpenPOS.Extension.Ver115"を追加する(以下にある)
  "C:\\Windows\\Microsoft.NET\\assembly\\GAC_MSIL\\OpenPOS.Extension.Ver115\\v4.0_1.15.0.0__ad2c9a67c3439201\\OpenPOS.Extension.Ver115.dll"
- ソースコードに"using OpenPOS.Extension;"を追加する
- PosExplorerのGetDevicesメソッドにデバイスクラス名やDeviceCompatibilitiesを指定して呼び出し、該当デバイスクラスのデバイスコレクションを取得  
- 取得したデバイスコレクションの中で ServiceObjectName と HardwarePath が一致するDeviceInfoを検索し、それを基にCreateInstanceメソッドでオブジェクトを生成  
- イベントハンドラを登録  
- Openメソッドを呼び出す  

コード例:


    ElectronicValueRW evrwObj1 = null;
    PosExplorer explorer = new PosExplorer();
    DeviceCollection evrwList = explorer.GetDevices("ElectronicValueRW", DeviceCompatibilities.CompatibilityLevel1);
    foreach (DeviceInfo evrwInfo in evrwList)
    {
        if  (evrwInfo.ServiceObjectName == "OpenPOS 1.15 ElectronicValueRW")
        {
            if (evrwInfo.HardwarePath == "OposEVRW1")
            {
                evrwObj1 = (ElectronicValueRW)explorer.CreateInstance(evrwInfo);
                break;
            }
        }
    }
    if (evrwObj1 != null)
    {
        evrwObj1.DataEvent += evrwObj1_DataEvent;
        evrwObj1.DirestIOEvent += evrwObj1_DirectIOEvent;
        evrwObj1.ErrorEvent += evrwObj1_ErrorEvent;
        evrwObj1.OutputCompleteEvent += evrwObj1_OutputCompleteEvent;
        evrwObj1.StatusUpdateEvent += evrwObj1_StatusUpdateEvent;
        evrwObj1.TransitionEvent += evrwObj1_TransitionEvent;

        evrwObj1.Open();
    }


注) Compatibilityプロパティ(DeviceCompatibilities)の値は場合によって変わります。  
DeviceCollection/DeviceInfoにリストされた状態では"CompatibilityLevel1"、CreateInstanceで生成されたオブジェクトでは"Opos"となります。  

- OpenPOS.Extension.Ver115のインターフェースを含んでいるかを確認してから UnifoedPOS 1.15 のメソッドを呼び出す  

コード例:


    // ElectronicValueRW evrwObj1 で宣言されている
    
    try
    {
        if (evrwObj1 is IElectronicValueRW115)
        {
            if (((IElectronicValueRW115) evrwObj1).ServiceType115 == ServiceType115.Cat)
            {
                int iSequenceNumber = 9999;
                decimal dAmount = 5000;
                decimal dTaxOthers = 500;
                int iTimeout = 300000;
                try
                {
                    ((IElectronicValueRW115) evrwObj1).AuthorizeSales(iSequenceNumber, dAmount, dTaxOthers, iTimeout);
                }
                catch(UPOSException ue)
                {
                }
            }
        }
    }
    catch(Exception ae)
    {
    }


## 既知の課題   

既知の課題は以下になります。

- 実際のOPOSやデバイスを使った動作確認は行っていません。  
- 特に以下のプロパティ/パラメータ/戻り値の、string(OPOS)とBitmap等(POS for.NET)との変換は正しいかどうか不明です。  
  - BiometricsデバイスのBiometricsInformationRecord(BIR)関連プロパティ/パラメータ/戻り値  
  - BiometricsデバイスのRawSensorDataプロパティ  
  - CheckScannerデバイスのImageDataプロパティ  
  - ImageScannerデバイスのFrameDataプロパティ  
- 動作記録採取や障害調査用情報取得などの機能がありません。  

## POS for.NETの課題  

開発過程でPOS for.NETに以下の課題が判明しました。  

- POS for.NETで定義されているBiometricsのBIRに情報が不足している。  
  UnifiedPOS仕様書に記述されているBIR図の以下の情報が入っていない。  
  - Quality  
  - Product ID (Owner, Type)  
  - Subtype  
  - Index Flag  
  - Index(UUID)  
  - Digital Signature  
- 同じくBIRで、課題かどうかは不明だが、以下の状況になっている。  
  - Creation Date/Creation Timeが、装置が情報を読み取った日時ではなく、POS for.NETサービスオブジェクトの中でBIRオブジェクトを作成した際の日時になっている。  
    POS for.NET自身のサービスオブジェクトでは問題無いかもしれないが、OPOSに中継する処理の場合は情報が書き換わってしまう。

## カスタマイズ

もし特定ユーザー/ベンダー固有の処理等のためのカスタマイズを加えたい場合、それは自由に行ってください。  
ただしその場合は、このサービスオブジェクトと同時に並行して使用しても問題無いように、以下の情報をすべて変更して独立したファイルにしてください。  

- ファイル名: OpenPOS.CCO115Interop.dll  
- namespace: OpenPOS.CCO115Interop  
- GUID: [assembly: Guid("a2226b69-b229-4d9b-95a9-624ea83f9207")]  
- サービスオブジェクト名: [ServiceObject(DeviceType.Xxxx, "OpenPOS 1.15 Xxxx",  
- クラス名: public class OpenPOSXxxx :  

注) 上記のうち "Xxxx" はUnifiedPOS/POS for.NETのデバイスクラス名が入る  

カスタマイズを行いたいデバイスだけを抜き出して新しく作るのが作業量も削減出来て良いでしょう。  

なお、汎用性のある良い機能ならば、こちらにも提案してください。

## ライセンス

[zlib License](./LICENSE) の下でライセンスされています。
