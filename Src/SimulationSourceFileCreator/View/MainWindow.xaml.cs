using System.Windows;

namespace SimulationSourceFileCreator.View
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

            if (this.DataContext is MainWindowViewModel vm)
            {
                vm.WindowActivate = () =>
                {
                    this.Activate();
                };
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