using System.Windows;

namespace SimulationSupportTool.View
{
    /// <summary>
    /// WindDirectionSettingDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class WindDirectionSettingDialog : Window
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="vm">ViewModel</param>
        public WindDirectionSettingDialog(WindDirectionSettingDialogViewModel vm)
        {
            this.InitializeComponent();

            // 画面を閉じる処理をセット
            vm.CloseAction = () =>
            {
                this.DialogResult = true;
                this.Close();
            };

            vm.CompassCanvas = this.compassCanvas;
            vm.CreateCompassNick();

            this.DataContext = vm;
        }
    }
}
