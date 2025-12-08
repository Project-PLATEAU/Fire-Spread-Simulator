namespace SimulationSupportTool.Model
{
    /// <summary>
    /// JavaScriptの出火点のモデルクラス
    /// </summary>
    public class JsFirePointResult
    {
        /// <summary>
        /// 番号
        /// </summary>
        public int No { get; set; }

        /// <summary>
        /// 出火点の位置
        /// </summary>
        public JsLatLon PointCoordinate { get; set; } = new JsLatLon();

        /// <summary>
        /// 出火点がシミュレーション範囲内にあるかどうか
        /// </summary>
        public bool IsPointInSimulationRange { get; set; }

        /// <summary>
        /// 出火点が含まれている建物
        /// </summary>
        public JsBuilding? Building { get; set; }
    }
}
