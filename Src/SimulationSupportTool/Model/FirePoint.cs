using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulationSupportTool.MVVM;

namespace SimulationSupportTool.Model
{
    /// <summary>
    /// 出火点のモデルクラス
    /// </summary>
    public partial class FirePoint : ViewModelBase
    {
        /// <summary>
        /// 番号
        /// </summary>
        [ObservableProperty]
        private int no;

        /// <summary>
        /// 出火時間（単位：分）
        /// </summary>
        [ObservableProperty]
        private string startMinutes = "0";

        /// <summary>
        /// 階数
        /// </summary>
        [ObservableProperty]
        private int? selectedStory;

        /// <summary>
        /// 階数リスト
        /// </summary>
        public List<int> StoryList { get; private set; } = [];

        /// <summary>
        /// 出火点の座標（単位：度）
        /// lat、lng の順の配列
        /// </summary>
        public double[] Coordinate { get; private set; } = [];

        /// <summary>
        /// 建物ID
        /// </summary>
        public string BldgId { get; private set; } = string.Empty;

        /// <summary>
        /// 出火時間の指定にエラーがあるかどうか
        /// </summary>
        public bool HasErrorStartMinutes { get; private set; }

        /// <summary>
        /// JavaScriptの出火点からC#の出火点を作成します。
        /// </summary>
        /// <param name="firePointResult">JavaScriptの出火点</param>
        /// <returns>C#の出火点</returns>
        internal static FirePoint CreateFromResult(JsFirePointResult firePointResult)
        {
            var storyList = new List<int>();

            var story = firePointResult.Building.Story;
            if (0 < story && story != 9999)
            {
                storyList = Enumerable.Range(1, story).ToList();
            }

            var firePoint = new FirePoint()
            {
                No = firePointResult.No,
                StartMinutes = "0",
                SelectedStory = storyList.Count == 0 ? null : storyList[0],
                StoryList = storyList,
                Coordinate = [firePointResult.PointCoordinate.Lat, firePointResult.PointCoordinate.Lon],
                BldgId = firePointResult.Building != null ? firePointResult.Building.BldgId : string.Empty,
            };

            return firePoint;
        }

        /// <summary>
        /// 経緯度が同じかどうかを判定します。
        /// </summary>
        /// <param name="other">比べる対象</param>
        /// <returns>true = 同じ、false = 異なる</returns>
        internal bool EqualsCoordinate(FirePoint other)
        {
            if (this.Coordinate == null || this.Coordinate.Length != 2)
            {
                return false;
            }

            if (other.Coordinate == null || other.Coordinate.Length != 2)
            {
                return false;
            }

            return this.Coordinate[0] == other.Coordinate[0] && this.Coordinate[1] == other.Coordinate[1];
        }

        /// <summary>
        /// 出火時間（単位：分）の変更時のコマンド
        /// </summary>
        /// <param name="simulationTimeTotalMinutes">シミュレーション時間（単位：分）</param>
        [RelayCommand]
        internal void StartMinutesChanged(int simulationTimeTotalMinutes)
        {
            this.HasErrorStartMinutes = false;
            var error = string.Empty;

            if (string.IsNullOrEmpty(this.StartMinutes)
                || !int.TryParse(this.StartMinutes, out var minute)
                || minute < 0
                || 2939 < minute
                || simulationTimeTotalMinutes < minute)
            {
                this.HasErrorStartMinutes = true;
                error = "Error";
            }

            this.UpdateError(nameof(this.StartMinutes), error);
        }
    }
}
