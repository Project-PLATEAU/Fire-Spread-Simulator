using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulationCommonLibrary.Utility;
using SimulationSourceFileCreator.Controller;
using SimulationSourceFileCreator.Model;

namespace SimulationSourceFileCreator.View
{
    /// <summary>
    /// 変換種類
    /// </summary>
    public enum ConvertType
    {
        /// <summary>
        /// すべて変換
        /// </summary>
        [Description("通常（すべて変換）")]
        All,

        /// <summary>
        /// 中間CSVファイルまで変換
        /// </summary>
        [Description("高度な変換の「出力」（中間CSVファイルまで変換）")]
        ToCSV,

        /// <summary>
        /// 中間CSVファイルから変換
        /// </summary>
        [Description("高度な変換の「変換」（中間CSVファイルから変換）")]
        FromCSV,
    }

    /// <summary>
    /// MainWindowのViewModel
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly List<string> targetFilePathList = [];

        private SourceFileCreator? creator = null;

        /// <summary>
        /// 入力フォルダ・ファイル選択がフォルダかどうか<br/>
        /// true = フォルダ、false = ファイル
        /// </summary>
        [ObservableProperty]
        private bool isInputTypeFolder = false;

        /// <summary>
        ///  入力フォルダ・ファイルパス
        /// </summary>
        [ObservableProperty]
        private string inputFolderOrFilePath = string.Empty;

        /// <summary>
        /// 出力フォルダパス
        /// </summary>
        [ObservableProperty]
        private string outputFolderPath = string.Empty;

        /// <summary>
        /// 選択中の平面直角座標系
        /// </summary>
        [ObservableProperty]
        private int selectedCoordinateSystemNumber = 9;

        /// <summary>
        /// 選択中の防火構造
        /// </summary>
        [ObservableProperty]
        private int selectedFireproofStructureType = 3;

        /// <summary>
        /// 入力フォルダ・ファイル選択のエラーメッセージ
        /// </summary>
        [ObservableProperty]
        private string inputFolderOrFilePathErrorMessage = string.Empty;

        /// <summary>
        /// 出力フォルダ選択のエラーメッセージ
        /// </summary>
        [ObservableProperty]
        private string outputFolderPathErrorMessage = string.Empty;

        /// <summary>
        /// 変換中かどうか
        /// </summary>
        [ObservableProperty]
        private bool isConverting = false;

        /// <summary>
        /// 進捗表示コントロールのViewModel
        /// </summary>
        [ObservableProperty]
        private ProgressControlViewModel? progressControlViewModel;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel()
        {
            // 初期値の設定
            this.isInputTypeFolder = true;

            // 平面直角座標系のリストをリソースから取得
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "SimulationSourceFileCreator.Resources.CoordinateSystemNumbers.xml";

            List<CoordinateSystemNumber>? loadList = null;
            var serializer = new XmlSerializer(typeof(List<CoordinateSystemNumber>));
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var sr = new StreamReader(stream))
                    {
                        loadList = serializer.Deserialize(sr) as List<CoordinateSystemNumber>;
                    }
                }
            }

            // 平面直角座標系のリストの初期化
            this.CoordinateSystemNumbers = loadList ?? [];

            // 防火構造のリストの初期化
            this.FireproofStructureTypeDict = new Dictionary<int, string>()
            {
                { 3, "防火造" },
                { 4, "準防火造" },
                { 5, "裸木造" },
            };

            // 作業フォルダパス（CSV）の初期化
            this.OutputCSVFolderPath = ConstSystemPath.WorkspaceCSVFolderPath;
        }

        #region プロパティ

        /// <summary>
        /// 平面直角座標系のリスト
        /// </summary>
        public List<CoordinateSystemNumber> CoordinateSystemNumbers { get; set; }

        /// <summary>
        /// 防火構造のリスト
        /// </summary>
        public Dictionary<int, string> FireproofStructureTypeDict { get; set; }

        /// <summary>
        /// 作業フォルダパス（CSV）
        /// </summary>
        public string OutputCSVFolderPath { get; set; }

        /// <summary>
        /// ウィンドウをアクティブにするAction
        /// </summary>
        internal Action? WindowActivate { get; set; }

        #endregion プロパティ

        /// <summary>
        /// ウィンドウが閉じられる直前に呼び出されます。
        /// </summary>
        /// <param name="e">イベント情報</param>
        internal void WindowClosing(CancelEventArgs e)
        {
            if (!this.IsConverting)
            {
                return;
            }

            if (!MessageBoxUtility.ShowQuestion("処理を中止してアプリケーションを終了します。\r\nよろしいですか？", Properties.Resources.WindowTitle))
            {
                // 終了をキャンセル
                e.Cancel = true;
                return;
            }

            this.creator?.Cancel();
        }

        /// <summary>
        /// 入力内容をチェックします。
        /// </summary>
        /// <returns>成否</returns>
        private bool Validate()
        {
            var result = true;
            this.InputFolderOrFilePathErrorMessage = string.Empty;
            this.OutputFolderPathErrorMessage = string.Empty;

            if (string.IsNullOrEmpty(this.InputFolderOrFilePath))
            {
                result = false;
                this.InputFolderOrFilePathErrorMessage = "入力フォルダまたはファイルを選択してください。";
            }

            if (string.IsNullOrEmpty(this.OutputFolderPath))
            {
                result = false;
                this.OutputFolderPathErrorMessage = "出力フォルダを選択してください。";
            }

            return result;
        }

        /// <summary>
        /// 処理を実行します。
        /// </summary>
        /// <param name="convertType">変換種類</param>
        private async void ExecuteAsync(ConvertType convertType)
        {
            var successCount = 0;
            var errorCount = 0;
            var cancelToken = new CancellationTokenSource();
            this.creator = null;

            this.ProgressControlViewModel = new ProgressControlViewModel(
                (progress) =>
                {
                    this.creator = SourceFileCreator.CreateInstance(progress, cancelToken);

                    if (this.creator == null)
                    {
                        return;
                    }

                    (successCount, errorCount) = this.creator.Execute(convertType, this.targetFilePathList, this.OutputFolderPath, this.SelectedCoordinateSystemNumber, this.SelectedFireproofStructureType);
                },
                () =>
                {
                    this.creator?.Cancel();
                },
                cancelToken);

            this.IsConverting = true;

            App.Logger.Info($"========================================");
            App.Logger.Info($"入力フォルダ・ファイル：{this.InputFolderOrFilePath}");
            App.Logger.Info($"出力フォルダ　　　　　：{this.OutputFolderPath}");
            App.Logger.Info($"平面直角座標系　：{this.SelectedCoordinateSystemNumber}");
            App.Logger.Info($"木造建物防火構造：{this.SelectedFireproofStructureType} {this.FireproofStructureTypeDict[this.SelectedFireproofStructureType]}");
            App.Logger.Info($"変換タイプ　　　：{this.GetDescription(convertType)}");
            App.Logger.Info($"========================================");

            await this.ProgressControlViewModel.ExecuteAsync();

            this.IsConverting = false;

            this.WindowActivate?.Invoke();

            if (cancelToken.IsCancellationRequested)
            {
                MessageBoxUtility.ShowInformation($"変換を中止しました。", Properties.Resources.WindowTitle);
                return;
            }

            var totalCount = this.targetFilePathList.Count;
            var message = $"全 {totalCount:#,0}件（成功 {successCount:#,0}件, 失敗 {errorCount:#,0}件, 未実施 {totalCount - successCount - errorCount:#,0}件）";
            if (successCount != totalCount)
            {
                MessageBoxUtility.ShowWarning($"変換に失敗しました。\r\n{message}", Properties.Resources.WindowTitle);
                return;
            }

            MessageBoxUtility.ShowInformation($"変換が完了しました。", Properties.Resources.WindowTitle);
        }

        /// <summary>
        /// Description属性の内容を取得します。
        /// </summary>
        /// <param name="convertType">変換種類</param>
        /// <returns>Description属性の内容</returns>
        private string GetDescription(ConvertType convertType)
        {
            var fieldInfo = convertType.GetType().GetField(convertType.ToString());
            if (fieldInfo == null)
            {
                return string.Empty;
            }

            var attr = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute));
            if (attr == null)
            {
                return string.Empty;
            }

            var descAttr = (DescriptionAttribute)attr;
            return descAttr.Description;
        }

        #region コマンド

        /// <summary>
        /// 入力フォルダ・ファイル選択がフォルダかどうか変更時のコマンド
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        partial void OnIsInputTypeFolderChanging(bool oldValue, bool newValue)
        {
            this.InputFolderOrFilePath = string.Empty;
        }

        /// <summary>
        /// 入力フォルダ・ファイル選択時のコマンド
        /// </summary>
        [RelayCommand]
        private void SelectInputFolderOrFilePath()
        {
            if (this.IsInputTypeFolder)
            {
                var defaultPath = ConfigFileManager.GetValue(ConfigFileManager.KeyInputFolderPath);
                if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
                {
                    defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                }

                // フォルダ選択ダイアログを準備
                var dialog = new Microsoft.Win32.OpenFolderDialog()
                {
                    Title = "フォルダ選択",
                    Multiselect = false,
                    InitialDirectory = defaultPath,
                };

                // フォルダ選択ダイアログを表示
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var folderPath = dialog.FolderName;

                var files = Directory.GetFiles(folderPath, "*.gml");

                if (files.Length == 0)
                {
                    MessageBoxUtility.ShowWarning("選択したフォルダにGMLファイルが含まれていません。", Properties.Resources.WindowTitle);
                    return;
                }

                this.InputFolderOrFilePath = folderPath;
                this.targetFilePathList.Clear();
                this.targetFilePathList.AddRange(files);

                ConfigFileManager.SetValue(ConfigFileManager.KeyInputFolderPath, this.InputFolderOrFilePath);
            }
            else
            {
                var defaultPath = Path.GetDirectoryName(ConfigFileManager.GetValue(ConfigFileManager.KeyInputFilePath)); // 親フォルダ
                if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
                {
                    defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                }

                // ファイル選択ダイアログを準備
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "ファイル選択",
                    Multiselect = false,
                    Filter = "CityGML|*.gml",
                    InitialDirectory = defaultPath,
                };

                // フォルダ選択ダイアログを表示
                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                this.InputFolderOrFilePath = dialog.FileName;
                this.targetFilePathList.Clear();
                this.targetFilePathList.Add(dialog.FileName);

                ConfigFileManager.SetValue(ConfigFileManager.KeyInputFilePath, this.InputFolderOrFilePath);
            }
        }

        /// <summary>
        /// 出力フォルダ選択時のコマンド
        /// </summary>
        [RelayCommand]
        private void SelectOutputFolderPath()
        {
            var defaultPath = ConfigFileManager.GetValue(ConfigFileManager.KeyOutputFolderPath);
            if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
            {
                defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            }

            // フォルダ選択ダイアログを準備
            var dialog = new Microsoft.Win32.OpenFolderDialog()
            {
                Title = "フォルダ選択",
                Multiselect = false,
                InitialDirectory = defaultPath,
            };

            // フォルダ選択ダイアログを表示
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var folderPath = dialog.FolderName;

            var currentPath = Directory.GetCurrentDirectory();
            if (folderPath.StartsWith(currentPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBoxUtility.ShowWarning("アプリケーションフォルダ内のフォルダは選択できません。", Properties.Resources.WindowTitle);
                return;
            }

            this.OutputFolderPath = folderPath;

            ConfigFileManager.SetValue(ConfigFileManager.KeyOutputFolderPath, this.OutputFolderPath);
        }

        /// <summary>
        /// 「地域（平面直角座標系）の詳細」ダイアログを開くコマンド
        /// </summary>
        [RelayCommand]
        private void OpenCoordinateSystemNumberDetailDialog()
        {
            var vm = new CoordinateSystemNumberDetailDialogViewModel(this.CoordinateSystemNumbers, this.SelectedCoordinateSystemNumber);
            var dialog = new CoordinateSystemNumberDetailDialog(vm)
            {
                Owner = Application.Current.MainWindow,
            };

            var res = dialog.ShowDialog();
            if (res.HasValue && res.Value)
            {
                this.SelectedCoordinateSystemNumber = vm.GetResult();
            }
        }

        /// <summary>
        /// 「防火構造の詳細」ダイアログを開くコマンド
        /// </summary>
        [RelayCommand]
        private void OpenFireproofStructureTypeDetailDialog()
        {
            var dialog = new FireproofStructureTypeDetailDialog()
            {
                Owner = Application.Current.MainWindow,
            };

            dialog.ShowDialog();
        }

        /// <summary>
        /// 変換を開始するコマンド
        /// </summary>
        /// <param name="convertType">変換種類</param>
        [RelayCommand]
        private void StartConvert(ConvertType convertType)
        {
            if (!this.Validate())
            {
                return;
            }

            this.ExecuteAsync(convertType);
        }

        /// <summary>
        /// 作業フォルダパス（CSV）をエクスプローラーで開くコマンド
        /// </summary>
        [RelayCommand]
        private void OpenCSVFolder()
        {
            Process.Start("explorer.exe", this.OutputCSVFolderPath);
        }

        #endregion コマンド
    }
}
