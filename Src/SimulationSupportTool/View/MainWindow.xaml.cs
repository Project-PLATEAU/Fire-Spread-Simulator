using System.Windows;
using Microsoft.Web.WebView2.Core;
using SimulationSupportTool.Controller;

namespace SimulationSupportTool.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();

            this.InitializeAsync();

            var vm = new MainWindowViewModel(this.firePointDataGrid, this.windConditionDataGrid)
            {
                WindowActivate = () =>
                {
                    this.Activate();
                },

                ScrollToBottom = () =>
                {
                    this.conditionScrollViewer.ScrollToBottom();
                },
            };

            this.DataContext = vm;
        }

        /// <summary>
        /// 非同期で初期化を実行します。
        /// </summary>
        private async void InitializeAsync()
        {
            // 初期化完了イベント追加
            this.webView2.CoreWebView2InitializationCompleted += this.WebView2InitializationCompleted;

            var options = new CoreWebView2EnvironmentOptions("--allow-file-access-from-files");
            var environment = await CoreWebView2Environment.CreateAsync(null, null, options);
            await this.webView2.EnsureCoreWebView2Async(environment);
        }

        /// <summary>
        /// 初期化完了イベントイベントハンドラ
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクト</param>
        /// <param name="e">イベント情報</param>
        private void WebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // ブラウザとしての管理者ツール等の機能を無効化
                this.webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                this.webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                this.webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
                this.webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;

                var viewPath = System.IO.Path.GetFullPath(@"Resources/map.html");
                this.webView2.CoreWebView2.Navigate(viewPath);
                this.webView2.CoreWebView2.NavigationCompleted += this.WebView2NavigationCompleted;
            }
        }

        /// <summary>
        /// ナビゲーションが完了イベントハンドラ
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクト</param>
        /// <param name="e">イベント情報</param>
        private void WebView2NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            CsToJs.Instance.Initialize(this.webView2);

            if (this.DataContext is MainWindowViewModel vm)
            {
                JsToCs.Instance.Initialize(vm);

                // JavaScriptからC#のメソッドが実行できる様に仕込む
                this.webView2.CoreWebView2.AddHostObjectToScript("csProcess", JsToCs.Instance);

                vm.IsInitializedWindow = true;
            }
        }

        /// <summary>
        /// ウィンドウが閉じられる直前に呼び出されるイベントハンドラ
        /// </summary>
        /// <param name="sender">イベントを発生させたオブジェクト</param>
        /// <param name="e">イベント情報</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DataContext is MainWindowViewModel vm)
            {
                vm.WindowClosing(e);
            }
        }
    }
}