using System.Windows;
using System.Windows.Controls;

namespace SimulationSourceFileCreator.View
{
    /// <summary>
    /// GridTextControl.xaml の相互作用ロジック
    /// </summary>
    public partial class GridTextControl : UserControl
    {
        /// <summary>
        /// 文字列の依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text",                               // プロパティ名
                typeof(string),                       // プロパティの型
                typeof(GridTextControl),              // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(string.Empty));  // 初期値

        /// <summary>
        /// 水平方向配置の依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty TextHorizontalAlignmentProperty =
            DependencyProperty.Register(
                "TextHorizontalAlignment",                        // プロパティ名
                typeof(HorizontalAlignment),                      // プロパティの型
                typeof(GridTextControl),                          // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(HorizontalAlignment.Left));  // 初期値

        /// <summary>
        /// マージンの依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(
                "TextMargin",                             // プロパティ名
                typeof(Thickness),                        // プロパティの型
                typeof(GridTextControl),                  // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(new Thickness(2)));  // 初期値

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GridTextControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 文字列の依存関係プロパティ
        /// </summary>
        public string Text
        {
            get { return (string)this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        /// <summary>
        /// 水平方向配置の依存関係プロパティ
        /// </summary>
        public HorizontalAlignment TextHorizontalAlignment
        {
            get { return (HorizontalAlignment)this.GetValue(TextHorizontalAlignmentProperty); }
            set { this.SetValue(TextHorizontalAlignmentProperty, value); }
        }

        /// <summary>
        /// マージンの依存関係プロパティ
        /// </summary>
        public Thickness TextMargin
        {
            get { return (Thickness)this.GetValue(TextMarginProperty); }
            set { this.SetValue(TextMarginProperty, value); }
        }
    }
}
