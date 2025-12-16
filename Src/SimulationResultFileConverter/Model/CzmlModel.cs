using Newtonsoft.Json;

namespace SimulationResultFileConverter.Model
{
    /// <summary>
    /// 建物延焼情報ファイルの建物情報のモデルクラス
    /// </summary>
    internal class CzmlModel
    {
        /// <summary>
        /// 建物ID
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// ジオイド高（単位：m）
        /// </summary>
        [JsonProperty("geoidHeight")]
        public double GeoidHeight { get; set; }

        /// <summary>
        /// 親要素の建物ID
        /// </summary>
        [JsonProperty("parent")]
        public string Parent { get; set; } = string.Empty;
    }
}
