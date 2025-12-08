using Newtonsoft.Json;

namespace SimulationCommonLibrary.Model
{
    /// <summary>
    /// GISデータ変換ツール設定ファイルのモデルクラス
    /// </summary>
    public class ResultFileConvSetting
    {
        /// <summary>
        /// シミュレーションデータフォルダパス
        /// </summary>
        [JsonProperty("inputSimulationSourceFolderPath")]
        public string InputSimulationSourceFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// シミュレーション結果フォルダパス
        /// </summary>
        [JsonProperty("inputSimulationResultFolderPath")]
        public string InputSimulationResultFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// GISデータ出力フォルダパス
        /// </summary>
        [JsonProperty("outputGisDataFolderPath")]
        public string OutputGisDataFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 建物延焼情報ファイル（CZMLファイル）を出力するかどうか
        /// </summary>
        [JsonProperty("isOutputBuilding")]
        public bool IsOutputBuilding { get; set; }

        /// <summary>
        /// 延焼経路情報ファイル（CZMLファイル）を出力するかどうか
        /// </summary>
        [JsonProperty("isOutputFirePath")]
        public bool IsOutputFirePath { get; set; }

        /// <summary>
        /// CZMLファイルの高さを楕円体高にするかどうか<br/>
        /// true = 楕円体高、false = 標高
        /// </summary>
        [JsonProperty("isEllipsoidHeight")]
        public bool IsEllipsoidHeight { get; set; }
    }
}
