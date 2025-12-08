using System.Windows;

namespace SimulationSourceFileCreator.View
{
    /// <summary>
    /// CoordinateSystemNumberDetailDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class CoordinateSystemNumberDetailDialog : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="vm">ViewModel</param>
        public CoordinateSystemNumberDetailDialog(CoordinateSystemNumberDetailDialogViewModel vm)
        {
            this.InitializeComponent();

            // 画面を閉じる処理をセット
            vm.CloseAction = () =>
            {
                this.DialogResult = true;
                this.Close();
            };

            this.DataContext = vm;
        }
    }
}
