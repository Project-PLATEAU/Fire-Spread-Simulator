using System.Windows;
using System.Windows.Controls;

namespace SimulationSourceFileCreator.View
{
    /// <summary>
    /// GridTextListControl.xaml の相互作用ロジック
    /// </summary>
    public partial class GridTextListControl : UserControl
    {
        /// <summary>
        /// 文字列1の依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty Text1Property =
            DependencyProperty.Register(
                "Text1",                              // プロパティ名
                typeof(string),                       // プロパティの型
                typeof(GridTextListControl),          // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(string.Empty));  // 初期値

        /// <summary>
        /// 文字列1の前に付属する太字テキストの依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty Text1BeforeBoldProperty =
            DependencyProperty.Register(
                "Text1BeforeBold",                    // プロパティ名
                typeof(string),                       // プロパティの型
                typeof(GridTextListControl),          // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(string.Empty));  // 初期値

        /// <summary>
        /// 文字列1の後に付属する太字テキストの依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty Text1AfterBoldProperty =
            DependencyProperty.Register(
                "Text1AfterBold",                     // プロパティ名
                typeof(string),                       // プロパティの型
                typeof(GridTextListControl),          // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(string.Empty));  // 初期値

        /// <summary>
        /// 文字列2の依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty Text2Property =
            DependencyProperty.Register(
                "Text2",                              // プロパティ名
                typeof(string),                       // プロパティの型
                typeof(GridTextListControl),          // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(string.Empty));  // 初期値

        /// <summary>
        /// 文字列3の依存関係プロパティを識別します。
        /// </summary>
        public static readonly DependencyProperty Text3Property =
            DependencyProperty.Register(
                "Text3",                              // プロパティ名
                typeof(string),                       // プロパティの型
                typeof(GridTextListControl),          // プロパティを所有する型＝このクラスの名前
                new PropertyMetadata(string.Empty));  // 初期値

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GridTextListControl()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 文字列1の依存関係プロパティ
        /// </summary>
        public string Text1
        {
            get { return (string)this.GetValue(Text1Property); }
            set { this.SetValue(Text1Property, value); }
        }

        /// <summary>
        /// 文字列1の前に付属する太字テキストの依存関係プロパティ
        /// </summary>
        public string Text1BeforeBold
        {
            get { return (string)this.GetValue(Text1BeforeBoldProperty); }
            set { this.SetValue(Text1BeforeBoldProperty, value); }
        }

        /// <summary>
        /// 文字列1の後に付属する太字テキストの依存関係プロパティ
        /// </summary>
        public string Text1AfterBold
        {
            get { return (string)this.GetValue(Text1AfterBoldProperty); }
            set { this.SetValue(Text1AfterBoldProperty, value); }
        }

        /// <summary>
        /// 文字列2の依存関係プロパティ
        /// </summary>
        public string Text2
        {
            get { return (string)this.GetValue(Text2Property); }
            set { this.SetValue(Text2Property, value); }
        }

        /// <summary>
        /// 文字列3の依存関係プロパティ
        /// </summary>
        public string Text3
        {
            get { return (string)this.GetValue(Text3Property); }
            set { this.SetValue(Text3Property, value); }
        }
    }
}
