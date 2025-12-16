using Newtonsoft.Json;

namespace SimulationResultFileConverter.Model
{
#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// 建物延焼情報ファイルの建物情報のサブモデルクラス
    /// </summary>
    internal class CzmlSubModel
    {
        /// <summary>
        /// 親要素の建物ID
        /// </summary>
        [JsonProperty("parent")]
        public string Parent { get; set; } = string.Empty;

        /// <summary>
        /// CZMLポリゴン情報
        /// </summary>
        [JsonProperty("polygon")]
        public CzmlPolygon? Polygon { get; set; }
    }

    /// <summary>
    /// CZMLポリゴン情報のモデルクラス
    /// </summary>
    internal class CzmlPolygon
    {
        /// <summary>
        /// CZML特性情報
        /// </summary>
        [JsonProperty("material")]
        public CzmlMaterial? Material { get; set; }

        /// <summary>
        /// CZML位置情報
        /// </summary>
        [JsonProperty("positions")]
        public CzmlPositions? Positions { get; set; }

        /// <summary>
        /// 高さ情報を個別に利用するかどうか
        /// </summary>
        [JsonProperty("perPositionHeight")]
        public bool PerPositionHeight { get; set; }
    }

    /// <summary>
    /// CZML特性情報のモデルクラス
    /// </summary>
    internal class CzmlMaterial
    {
        /// <summary>
        /// CZML単一色情報
        /// </summary>
        [JsonProperty("solidColor")]
        public CzmlSolidColor? SolidColor { get; set; }
    }

    /// <summary>
    /// CZML単一色情報のモデルクラス
    /// </summary>
    internal class CzmlSolidColor
    {
        /// <summary>
        /// CZML色情報
        /// </summary>
        [JsonProperty("color")]
        public CzmlColor? Color { get; set; }
    }

    /// <summary>
    /// CZML色情報のモデルクラス
    /// </summary>
    internal class CzmlColor
    {
        /// <summary>
        /// 参照情報
        /// </summary>
        [JsonProperty("reference")]
        public string Reference { get; set; } = string.Empty;
    }

    /// <summary>
    /// CZML位置情報のモデルクラス
    /// </summary>
    internal class CzmlPositions
    {
        /// <summary>
        /// 経度（単位：度）、緯度（単位：度）、高さ（単位：m）
        /// </summary>
        [JsonProperty("cartographicDegrees")]
        public double[] CartographicDegrees { get; set; } = [];
    }

#pragma warning restore SA1402 // File may only contain a single type
}
