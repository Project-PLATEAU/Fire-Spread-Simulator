using System.IO;
using System.Text;
using log4net;

namespace SimulationCommonLibrary.Utility
{
    /// <summary>
    /// ファイルのユーティリティクラス
    /// </summary>
    public static class FileUtility
    {
        /// <summary>
        /// ファイルを複製して内容を書き換えます。
        /// </summary>
        /// <param name="sourceFilePath">複製元ファイルパス</param>
        /// <param name="destFilePath">複製先ファイルパス</param>
        /// <param name="rewriteAction">書き換え関数</param>
        /// <param name="encoding">エンコーディング</param>
        /// <returns>成否</returns>
        /// <exception cref="ArgumentNullException">sourceFilePath、destFilePath、rewriteAction または encoding が null または empty の場合</exception>
        public static bool CopyAndRewrite(string sourceFilePath, string destFilePath, Func<string, string> rewriteAction, Encoding encoding)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                throw new ArgumentNullException(nameof(sourceFilePath));
            }

            if (string.IsNullOrEmpty(destFilePath))
            {
                throw new ArgumentNullException(nameof(destFilePath));
            }

            ArgumentNullException.ThrowIfNull(rewriteAction);

            ArgumentNullException.ThrowIfNull(encoding);

            if (!File.Exists(sourceFilePath))
            {
                return false;
            }

            if (sourceFilePath.Equals(destFilePath))
            {
                return false;
            }

            try
            {
                using (var sr = new StreamReader(sourceFilePath, encoding))
                using (var sw = new StreamWriter(destFilePath, false, encoding))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();

                        line = rewriteAction(line ?? string.Empty);

                        sw.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                ILog log = LogManager.GetLogger("SimulationCommonLibrary");
                log.Error("ファイルの複製と書き換えに失敗しました。", ex);
                return false;
            }

            return true;
        }
    }
}
