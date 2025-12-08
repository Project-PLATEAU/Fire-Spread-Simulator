using SimulationCommonLibrary.Utility;

namespace UnitTest.CommonLibrary;

/// <summary>
/// DirectoryUtilityテストクラス
/// </summary>
[TestClass]
public class DirectoryUtilityTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    internal static string TestDataFolder => Path.Combine(@"..\..\..\CommonLibrary", "TestData", "DirectoryUtilityTest");

    /// <summary>
    /// 正常
    /// </summary>
    /// <param name="targetFolderName">対象フォルダ名</param>
    /// <param name="targetExtension">対象拡張子</param>
    /// <param name="expectedFileCount">残りのファイル数</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("test_folder1", "*.*",   0)]
    [DataRow("test_folder1", "*",     0)]
    [DataRow("test_folder1", "*.csv", 2)]
    [DataRow("test_folder1", "*.ini", 3)]
    public void TestMethod_Success(string targetFolderName, string targetExtension, int expectedFileCount)
    {
        // テスト用フォルダの準備
        var destDir = Path.Combine(TestDataFolder, targetFolderName);
        CopyTestDirectory(destDir);

        // 実行前の件数確認（サブディレクトリのファイルはカウントしない）
        var fileCount = Directory.GetFiles(destDir, "*", SearchOption.TopDirectoryOnly).Length;
        Assert.AreEqual(3, fileCount);

        // 実行
        var isSuccess = DirectoryUtility.CleanupDirectory(destDir, targetExtension);
        Assert.IsTrue(isSuccess);

        // 実行後の件数確認（サブディレクトリのファイルはカウントしない）
        fileCount = Directory.GetFiles(destDir, "*", SearchOption.TopDirectoryOnly).Length;
        Assert.AreEqual(expectedFileCount, fileCount);

        // テスト用フォルダの削除
        Directory.Delete(destDir, true);
    }

    /// <summary>
    /// 不正：引数不正
    /// </summary>
    /// <param name="targetFolderPath">対象フォルダパス</param>
    /// <param name="targetExtension">対象拡張子</param>
    /// <param name="expectedExceptionItem">Exceptionの対象の引数名</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow(null,          "*.csv", "targetFolderPath")]
    [DataRow("",            "*.csv", "targetFolderPath")]
    [DataRow("test_folder2", null,   "targetExtension")]
    [DataRow("test_folder2", "", 　　"targetExtension")]
    public void TestMethod_Exception(string targetFolderPath, string targetExtension, string expectedExceptionItem)
    {
        var ex = Assert.ThrowsException<ArgumentNullException>(() => DirectoryUtility.CleanupDirectory(targetFolderPath, targetExtension));
        Assert.IsTrue(ex.Message.Contains(expectedExceptionItem));
    }

    /// <summary>
    /// 失敗：対象フォルダがない
    /// </summary>
    [TestMethod]
    public void TestMethod_Failure()
    {
        var destDir = Path.Combine(TestDataFolder, "test_folder3");

        var isSuccess = DirectoryUtility.CleanupDirectory(destDir, "*.csv");
        Assert.IsFalse(isSuccess);
    }

    private static void CopyTestDirectory(string destDir)
    {
        var sourceDir = Path.Combine(TestDataFolder, "org_folder");
        CopyDirectory(sourceDir, destDir);
    }

    private static void CopyDirectory(string sourceDir, string destDir)
    {
        // コピー先フォルダが存在しない場合は作成
        Directory.CreateDirectory(destDir);

        // ファイルをコピー
        foreach (var filePath in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(filePath);
            var destFilePath = Path.Combine(destDir, fileName);
            File.Copy(filePath, destFilePath, overwrite: true);
        }

        // サブフォルダを再帰的にコピー
        foreach (var subDir in Directory.GetDirectories(sourceDir))
        {
            var subDirName = Path.GetFileName(subDir);
            var destSubDir = Path.Combine(destDir, subDirName);
            CopyDirectory(subDir, destSubDir);
        }
    }
}
