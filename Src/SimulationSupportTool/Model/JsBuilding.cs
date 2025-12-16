namespace SimulationSupportTool.Model
{
    /// <summary>
    /// JavaScriptの建物のモデルクラス
    /// </summary>
    public class JsBuilding
    {
        /// <summary>
        /// 建物ID
        /// </summary>
        public string BldgId { get; set; } = string.Empty;

        /// <summary>
        /// 構造
        /// </summary>
        public string Structure { get; set; } = string.Empty;

        /// <summary>
        /// 階数
        /// </summary>
        public int Story { get; set; } = 0;
    }
}
