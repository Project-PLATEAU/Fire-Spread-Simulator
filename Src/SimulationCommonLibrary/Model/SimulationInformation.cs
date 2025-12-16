using Newtonsoft.Json;

namespace SimulationCommonLibrary.Model
{
    /// <summary>
    /// シミュレーション実行情報ファイルのモデルクラス
    /// </summary>
    public class SimulationInformation
    {
        /// <summary>
        /// シミュレーション時間（単位：分）
        /// </summary>
        [JsonProperty("simulationTimeTotalMinutes")]
        public int SimulationTimeTotalMinutes { get; set; } = 0;

        /// <summary>
        /// シミュレーション範囲のメッシュ番号の配列
        /// </summary>
        [JsonProperty("selectedSimulationRangeMeshNumbers")]
        public string[] SelectedSimulationRangeMeshNumbers { get; set; } = [];

        /// <summary>
        /// シミュレーション実行開始日時（実行日付のUTC基準の0:00）
        /// </summary>
        [JsonProperty("simulationStartDateTime")]
        public DateTime SimulationStartDateTime { get; set; } = DateTime.MinValue;
    }
}
