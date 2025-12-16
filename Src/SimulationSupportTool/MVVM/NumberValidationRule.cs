using System.Globalization;
using System.Windows.Controls;

namespace SimulationSupportTool.MVVM
{
    /// <summary>
    /// 数値入力を検証するための <see cref="ValidationRule"/> 実装クラス
    /// </summary>
    public class NumberValidationRule : ValidationRule
    {
        /// <summary>
        /// 有効範囲　最小値（境界地を含む）
        /// </summary>
        public double Min { get; set; }

        /// <summary>
        /// 有効範囲　最大有値（境界地を含む）
        /// </summary>
        public double Max { get; set; }

        /// <summary>
        /// 入力値が数値でかつ有効範囲内かどうかを検証します。
        /// </summary>
        /// <param name="value">バインディング ソースから渡される値</param>
        /// <param name="cultureInfo">カルチャ情報</param>
        /// <returns>
        /// 入力値が数値でかつ有効範囲内の場合は <see cref="ValidationResult.ValidResult"/> を返します。
        /// それ以外の場合はエラーメッセージを含む <see cref="ValidationResult"/> を返します。
        /// </returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value == null || value is not string str)
            {
                return new ValidationResult(false, $"入力無し or 文字列ではない");
            }

            if (str.EndsWith('.'))
            {
                return new ValidationResult(false, $"小数の値の入力途中");
            }

            if (str.Equals("-0"))
            {
                return new ValidationResult(false, $"マイナスの残骸");
            }

            if (!double.TryParse(str, out var number))
            {
                return new ValidationResult(false, $"数値(double)を表す文字列ではない");
            }

            if (number < this.Min || this.Max < number)
            {
                return new ValidationResult(false, $"数値が有効範囲外: {this.Min}-{this.Max}");
            }

            return ValidationResult.ValidResult;
        }
    }
}
