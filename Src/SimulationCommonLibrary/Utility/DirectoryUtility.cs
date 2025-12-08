using System.IO;
using log4net;

namespace SimulationCommonLibrary.Utility
{
    /// <summary>
    /// ディレクトリのユーティリティクラス
    /// </summary>
    public static class DirectoryUtility
    {
        /// <summary>
        /// フォルダから指定の拡張子のファイルを削除します。
        /// </summary>
        /// <param name="targetFolderPath">対象フォルダパス</param>
        /// <param name="targetExtension">対象拡張子</param>
        /// <returns>成否</returns>
        /// <exception cref="ArgumentNullException">targetFolderPath または targetExtension が null または empty の場合</exception>
        public static bool CleanupDirectory(string targetFolderPath, string targetExtension)
        {
            if (string.IsNullOrEmpty(targetFolderPath))
            {
                throw new ArgumentNullException(nameof(targetFolderPath));
            }

            if (string.IsNullOrEmpty(targetExtension))
            {
                throw new ArgumentNullException(nameof(targetExtension));
            }

            if (!Directory.Exists(targetFolderPath))
            {
                ILog log = LogManager.GetLogger("SimulationCommonLibrary");
                log.Error($"フォルダのクリーンアップに失敗しました。対象のフォルダありません。targetFolderPath = {targetFolderPath}");
                return false;
            }

            try
            {
                var files = Directory.GetFiles(targetFolderPath, targetExtension, SearchOption.TopDirectoryOnly);

                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                ILog log = LogManager.GetLogger("SimulationCommonLibrary");
                log.Error("フォルダのクリーンアップに失敗しました。", ex);
                return false;
            }

            return true;
        }
    }
}
