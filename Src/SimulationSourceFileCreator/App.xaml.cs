using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using SimulationCommonLibrary.Utility;
using SimulationSourceFileCreator.Model;

namespace SimulationSourceFileCreator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// ロガー
        /// </summary>
        private static log4net.ILog? logger = null;

        /// <summary>
        /// ミューテックス
        /// </summary>
        private readonly Mutex mutex = new Mutex(false, "SimulationSourceFileCreator_Mutex");

        /// <summary>
        /// アプリケーションが起動しているかどうか
        /// </summary>
        private bool isStartUp = false;

        /// <summary>
        /// メイン関数
        /// </summary>
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Logger.Error($"エラーが発生しました。アプリケーションを終了します。sender = {sender}, args = {args}");
                MessageBoxUtility.ShowError("エラーが発生しました。アプリケーションを終了します。", "ファイル作成ツール");
                Current.Shutdown();
            };

            // カレントディレクトリの設定
            // ※ 相対パスによるショートカットからの起動を考慮して
            //    自身の実行exeファイルの場所をカレントフォルダに設定します。
            var appPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            if (!string.IsNullOrEmpty(appPath))
            {
                // カレントディレクトリに設定
                Directory.SetCurrentDirectory(appPath);
                App.Logger.Debug($"カレントディレクトリに設定 = {appPath}");
            }

            // エンコーディングプロバイダーの登録
            // ※ .NET(Core系)は デフォルトで shift-jis(sjis) に対応したエンコーディングプロバイダーが
            //    登録されていないため Encoding.RegisterProvider メソッドで明示的に登録する必要があります。
            //    プロバイダーは1度登録すると、プログラムが終了するまで有効です。
            //    プロバイダーを登録したプログラムのみ有効で、他のプログラムや環境などには影響しません。
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 設定ファイルの複製（既にあれば何もしない）
            ElementAddSetting.Initialize(ConstSystemPath.SettingFilePath);

            // workspaceフォルダの作成（既にあれば何もしない）
            Directory.CreateDirectory(ConstSystemPath.WorkspaceCSVFolderPath);
            Directory.CreateDirectory(ConstSystemPath.WorkspaceGMLFolderPath);
        }

        /// <summary>
        /// ロガー
        /// </summary>
        internal static log4net.ILog Logger
        {
            get
            {
                if (logger == null)
                {
                    log4net.Config.XmlConfigurator.Configure(new FileInfo("log4net.config"));
                    logger = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName ?? string.Empty);
                }

                return logger;
            }
        }

        /// <summary>
        /// アプリケーションの起動時に呼び出されます。
        /// </summary>
        /// <param name="e">アプリケーションの起動に関するイベントデータ</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                this.isStartUp = this.mutex.WaitOne(0, false);

                if (!this.isStartUp)
                {
                    // 既に起動している場合　→　メッセージボックスを表示してアプリケーションを終了
                    MessageBoxUtility.ShowWarning("アプリケーションは既に起動しています。", "ファイル作成ツール");
                    Current.Shutdown();
                    return;
                }
            }
            catch (AbandonedMutexException)
            {
                // 正しく開放されずに破棄されていたという通知なので無視
                this.isStartUp = true;
            }

            base.OnStartup(e);
        }

        /// <summary>
        /// アプリケーションの終了時に呼び出されます。
        /// </summary>
        /// <param name="e"> アプリケーション終了に関するイベントデータ</param>
        protected override void OnExit(ExitEventArgs e)
        {
            if (this.isStartUp)
            {
                // ミューテックスを解放
                this.mutex?.ReleaseMutex();
            }

            this.mutex?.Close();
            base.OnExit(e);
        }
    }
}
