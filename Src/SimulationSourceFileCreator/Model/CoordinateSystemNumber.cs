namespace SimulationSourceFileCreator.Model
{
    /// <summary>
    /// 平面直角座標系のモデルクラス
    /// </summary>
    public class CoordinateSystemNumber
    {
        /// <summary>
        /// 番号
        /// </summary>
        public int SeriesNumber { get; set; }

        /// <summary>
        /// 番号（表示用）
        /// </summary>
        public string DisplayNumber { get; set; } = string.Empty;

        /// <summary>
        /// 対象エリア
        /// </summary>
        public string TargetArea { get; set; } = string.Empty;

        /// <summary>
        /// 対象エリアの詳細
        /// </summary>
        public string TargetAreaDetails { get; set; } = string.Empty;
    }
}
