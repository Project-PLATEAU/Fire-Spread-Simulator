using System.Xml.Serialization;

namespace SimulationSourceFileCreator.Model
{
    /// <summary>
    /// 要素追加設定ファイルの取得要素設定のモデルクラス
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "<保留中>")]
    public class GetElement()
    {
        /// <summary>
        /// キー（CSVに値を出力する対象の列名）
        /// </summary>
        [XmlAttribute]
        public string KeyName { get; set; } = string.Empty;

        /// <summary>
        /// 取得タイプ
        /// </summary>
        [XmlAttribute]
        public int TargetType { get; set; } = -1;

        /// <summary>
        /// 取得タイプに応じた値
        /// </summary>
        [XmlAttribute]
        public string TargetValue { get; set; } = string.Empty;

        /// <summary>
        /// 要素名
        /// </summary>
        [XmlIgnore]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// 属性名
        /// </summary>
        [XmlIgnore]
        public string AttributeName { get; set; } = string.Empty;

        /// <summary>
        /// 属性値
        /// </summary>
        [XmlIgnore]
        public string AttributeValue { get; set; } = string.Empty;

        /// <summary>
        /// 固定値<br/>
        /// ※取得タイプ3の時のみ使用
        /// </summary>
        [XmlIgnore]
        public string FixedValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// 要素追加設定ファイルの削除要素設定のモデルクラス
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "<保留中>")]
    public class RemoveElement
    {
        /// <summary>
        /// 要素名（対象の要素のパス）
        /// </summary>
        [XmlAttribute]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// 要素の値の有効範囲最小値（境界値を含む）
        /// </summary>
        [XmlAttribute]
        public int CheckMinValue { get; set; }

        /// <summary>
        /// 要素の値の有効範囲最大値（境界値を含む）
        /// </summary>
        [XmlAttribute]
        public int CheckMaxValue { get; set; }
    }

    /// <summary>
    /// 要素追加設定ファイルの追加要素設定のモデルクラス
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "<保留中>")]
    public class AddElement()
    {
        /// <summary>
        /// キー（CSVから値を取得する対象の列名）
        /// </summary>
        [XmlAttribute]
        public string KeyName { get; set; } = string.Empty;

        /// <summary>
        /// 要素名（追加する要素のパス）
        /// </summary>
        [XmlAttribute]
        public string TagName { get; set; } = string.Empty;

        /// <summary>
        /// 初期値（CSVに対象の列名が無い場合の初期値）
        /// </summary>
        [XmlAttribute]
        public int DefaultValue { get; set; }
    }
}
