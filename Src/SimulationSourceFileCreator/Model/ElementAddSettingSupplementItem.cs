namespace SimulationSourceFileCreator.Model
{
    /// <summary>
    /// 要素追加設定ファイルのデータ補完モデルクラス<br/>
    /// KOZO：耐火構造、建物構造、地上階数、延床面積、建築面積 を使用<br/>
    /// MOKU：建物構造 を使用<br/>
    /// YOTO：利用目的 を使用<br/>
    /// </summary>
    public class ElementAddSettingSupplementItem
    {
        /// <summary>
        /// 耐火構造
        /// </summary>
        public GetElement? Taika { get; set; } = null;

        /// <summary>
        /// 建物構造
        /// </summary>
        public GetElement? Tatemono { get; set; } = null;

        /// <summary>
        /// 地上階数
        /// </summary>
        public GetElement? Kaisu { get; set; } = null;

        /// <summary>
        /// 延床面積
        /// </summary>
        public GetElement? Nobeyuka { get; set; } = null;

        /// <summary>
        /// 建築面積
        /// </summary>
        public GetElement? Kenchiku { get; set; } = null;

        /// <summary>
        /// 利用目的
        /// </summary>
        public GetElement? Mokuteki { get; set; } = null;
    }
}
