using System.Globalization;
using System.Windows.Data;

namespace SimulationCommonLibrary.Converter
{
    /// <summary>
    /// valueのboolを反転するコンバーター
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class BooleanInverseConverter : IValueConverter
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
            if (value is bool b)
            {
                return !b;
            }

            return false;
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
            if (value is bool b)
            {
                return !b;
            }

            return false;
        }
    }
}
