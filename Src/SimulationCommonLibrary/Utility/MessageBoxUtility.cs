using System.Windows;

namespace SimulationCommonLibrary.Utility
{
    /// <summary>
    /// メッセージボックスのユーティリティクラス
    /// </summary>
    public static class MessageBoxUtility
    {
        /// <summary>
        /// 情報メッセージボックスを表示します。
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="title">タイトル</param>
        public static void ShowInformation(string message, string title)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// 警告メッセージボックスを表示します。
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="title">タイトル</param>
        public static void ShowWarning(string message, string title)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// エラーメッセージボックスを表示します。
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="title">タイトル</param>
        public static void ShowError(string message, string title)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// 確認メッセージボックスを表示します。
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="title">タイトル</param>
        /// <returns>OKかどうか（ture:OK, false:キャンセル）</returns>
        public static bool ShowQuestion(string message, string title)
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question,
                MessageBoxResult.Cancel).Equals(MessageBoxResult.OK);
        }
    }
}
