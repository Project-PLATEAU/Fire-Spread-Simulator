using System.Globalization;
using System.Windows.Data;

namespace SimulationCommonLibrary.Converter
{
    /// <summary>
    /// valueのstringがNullまたはEmptyの場合にtrueを返すコンバーター
    /// </summary>
    [ValueConversion(typeof(string), typeof(bool))]
    internal class NullStringToBooleanConverter : IValueConverter
    {
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
            if (value is not string text)
            {
                return false;
            }

            return string.IsNullOrEmpty(text);
        }

        /// <summary>
        /// 値を逆変換します。（実装しません）
        /// </summary>
        /// <param name="value">バインディング ターゲットから渡される値</param>
        /// <param name="targetType">バインディング ソースの型</param>
        /// <param name="parameter">XAML で指定された追加のパラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>変換後の値</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
