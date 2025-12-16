using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulationSourceFileCreator.Model;

namespace SimulationSourceFileCreator.View
{
    /// <summary>
    /// CoordinateSystemNumberDetailDialogのViewModel
    /// </summary>
    public partial class CoordinateSystemNumberDetailDialogViewModel : ObservableObject
    {
        /// <summary>
        /// 選択中の平面直角座標系
        /// </summary>
        [ObservableProperty]
        private int selectedCoordinateSystemNumber = 9;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="coordinateSystemNumbers">平面直角座標系のリスト</param>
        /// <param name="selectedCoordinateSystemNumber">選択中の平面直角座標系</param>
        public CoordinateSystemNumberDetailDialogViewModel(List<CoordinateSystemNumber> coordinateSystemNumbers, int selectedCoordinateSystemNumber)
        {
            this.SelectedCoordinateSystemNumber = selectedCoordinateSystemNumber;
            this.CoordinateSystemNumbers = coordinateSystemNumbers;
        }

        /// <summary>
        /// 平面直角座標系のリスト
        /// </summary>
        public List<CoordinateSystemNumber> CoordinateSystemNumbers { get; set; }

        /// <summary>
        /// ウィンドウを閉じるAction
        /// </summary>
        internal Action? CloseAction { get; set; }

        /// <summary>
        /// 結果を取得します。
        /// </summary>
        /// <returns>選択中の平面直角座標系</returns>
        internal int GetResult()
        {
            return this.SelectedCoordinateSystemNumber;
        }

        #region コマンド

        /// <summary>
        /// 平面直角座標系選択時のコマンド
        /// </summary>
        /// <param name="coordinateSystemNumber">選択した平面直角座標系</param>
        [RelayCommand]
        private void SelectSeriesNumber(CoordinateSystemNumber coordinateSystemNumber)
        {
            this.SelectedCoordinateSystemNumber = coordinateSystemNumber.SeriesNumber;
        }

        /// <summary>
        /// 選択（ウィンドウを閉じる）コマンド
        /// </summary>
        [RelayCommand]
        private void Select()
        {
            this.CloseAction?.Invoke();
        }

        #endregion コマンド
    }
}
