using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulationSupportTool.MVVM;

namespace SimulationSupportTool.Model
{
    /// <summary>
    /// 風向・風速のモデルクラス
    /// </summary>
    public partial class WindCondition : ViewModelBase
    {
        /// <summary>
        /// 番号
        /// </summary>
        [ObservableProperty]
        private int no;

        /// <summary>
        /// 開始時間（単位：分）
        /// </summary>
        [ObservableProperty]
        private string startMinutes = "0";

        /// <summary>
        /// 風向（単位：°）
        /// </summary>
        [ObservableProperty]
        private double windDirection = 0d;

        /// <summary>
        /// 風速（単位：m/s）
        /// </summary>
        [ObservableProperty]
        private double windSpeed = 0d;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="no">番号</param>
        public WindCondition(int no)
        {
            this.no = no;
        }

        /// <summary>
        /// 開始時間の指定にエラーがあるかどうか
        /// </summary>
        public bool HasErrorStartMinutes { get; private set; }

        /// <summary>
        /// 風向の指定にエラーがあるかどうか
        /// </summary>
        public bool HasErrorWindDirection { get; private set; }

        /// <summary>
        /// 風速の指定にエラーがあるかどうか
        /// </summary>
        public bool HasErrorWindSpeed { get; private set; }

        /// <summary>
        /// 開始時間（単位：分）の変更時のコマンド
        /// </summary>
        /// <param name="simulationTimeTotalMinutes">シミュレーション時間（単位：分）</param>
        [RelayCommand]
        internal void StartMinutesChanged(int simulationTimeTotalMinutes)
        {
            this.HasErrorStartMinutes = false;
            var error = string.Empty;

            if (string.IsNullOrEmpty(this.StartMinutes)
                || !int.TryParse(this.StartMinutes, out var minute)
                || minute < 0 || 2939 < minute
                || simulationTimeTotalMinutes < minute)
            {
                this.HasErrorStartMinutes = true;
                error = "Error";
            }

            this.UpdateError(nameof(this.StartMinutes), error);
        }

        /// <summary>
        /// 風向（単位：°）の変更時のコマンド
        /// </summary>
        /// <param name="hasError">エラーがあるかどうか</param>
        [RelayCommand]
        private void WindDirectionChanged(bool hasError)
        {
            this.HasErrorWindDirection = hasError;
        }

        /// <summary>
        /// 風速（単位：m/s）の変更時のコマンド
        /// </summary>
        /// <param name="hasError">エラーがあるかどうか</param>
        [RelayCommand]
        private void WindSpeedChanged(bool hasError)
        {
            this.HasErrorWindSpeed = hasError;
        }
    }
}
