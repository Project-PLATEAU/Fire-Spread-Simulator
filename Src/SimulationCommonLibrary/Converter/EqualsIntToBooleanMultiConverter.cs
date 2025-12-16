using System.Globalization;
using System.Windows.Data;

namespace SimulationCommonLibrary.Converter
{
    /// <summary>
    /// values[0]のint = values[1]のint の場合にtrueを返すコンバーター
    /// </summary>
    internal class EqualsIntToBooleanMultiConverter : IMultiValueConverter
    {
        /// <summary>
        /// 複数の値をターゲット値に変換します。
        /// </summary>
        /// <param name="values">バインディング ソースから渡される値の配列</param>
        /// <param name="targetType">バインディング ターゲットの型</param>
        /// <param name="parameter">XAML で指定された追加のパラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>変換後の値</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not int i)
            {
                return false;
            }

            if (values[1] is not int j)
            {
                return false;
            }

            return i == j;
        }

        /// <summary>
        /// ターゲット値を複数のソース値に逆変換します。（実装しません）
        /// </summary>
        /// <param name="value">バインディング ターゲットから渡される値</param>
        /// <param name="targetTypes">バインディング ソースの型配列</param>
        /// <param name="parameter">XAML で指定された追加のパラメータ</param>
        /// <param name="culture">カルチャ情報</param>
        /// <returns>逆変換後の値の配列</returns>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
