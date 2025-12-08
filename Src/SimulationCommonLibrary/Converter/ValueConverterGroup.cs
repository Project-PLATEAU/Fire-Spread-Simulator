using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace SimulationCommonLibrary.Converter
{
    /// <summary>
    /// 複数の <see cref="IValueConverter"/> を連結して順番に適用するコンバーター
    /// 各コンバーターの <c>Convert</c> メソッドを順に呼び出し、最終的な結果を返します。
    /// WPF のバインディングで複数の変換処理を組み合わせたい場合に使用します。
    /// </summary>
    /// <remarks>
    /// ConvertBack は逆順に各コンバーターのConvertBackを呼び出しますが、
    /// 個々のコンバーターが双方向変換に対応している必要があります。
    /// </remarks>
    [ContentProperty(nameof(Converters))]
    public class ValueConverterGroup : IValueConverter
    {
        /// <summary>
        /// 連結するコンバーター
        /// </summary>
        public Collection<IValueConverter> Converters { get; } = [];

        /// <summary>
        /// 値を変換します。
        /// </summary>
        /// <param name="value">バインディング ソースから渡される値</param>
        /// <param name="targetType">バインディング ターゲットの型</param>
        /// <param name="parameter">XAML で指定された追加のパラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>変換後の値</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value;

            if (this.Converters == null)
            {
                return result;
            }

            foreach (var conv in this.Converters)
            {
                result = conv.Convert(result, targetType, parameter, culture);
            }

            return result;
        }

        /// <summary>
        /// 値を逆変換します。
        /// </summary>
        /// <param name="value">バインディング ターゲットから渡される値</param>
        /// <param name="targetType">バインディング ソースの型</param>
        /// <param name="parameter">XAML で指定された追加のパラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>変換後の値</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = value;

            if (this.Converters == null)
            {
                return result;
            }

            foreach (var conv in this.Converters.Reverse())
            {
                result = conv.ConvertBack(result, targetType, parameter, culture);
            }

            return result;
        }
    }
}
