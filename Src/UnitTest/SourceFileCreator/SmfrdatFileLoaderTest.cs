using SimulationSourceFileCreator.Controller;

namespace UnitTest.SourceFileCreator;

/// <summary>
/// SmfrdatFileLoaderテストクラス
/// </summary>
[TestClass]
public class SmfrdatFileLoaderTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SourceFileCreator", "TestData", "SmfrdatFileLoaderTest");

    /// <summary>
    /// Cleanup
    /// </summary>
    [TestCleanup]
    public void Cleanup()
    {
        // カレントディレクトリの設定を元に戻す
        // ※ Testメソッド内でのカレントディレクトリの設定を考慮して
        //    自身の実行exeファイルの場所をカレントフォルダに設定します。
        var appPath = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
        if (!string.IsNullOrEmpty(appPath))
        {
            // カレントディレクトリに設定
            Directory.SetCurrentDirectory(appPath);
        }
    }

    /// <summary>
    /// <see cref="SmfrdatFileLoader.CorrectOrRemoveInvalidShape"/>のテスト
    /// </summary>
    /// <param name="folderName">フォルダ名</param>
    /// <param name="expectedIsSuccess">期待値：成否</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("ファイル不正", false)]
    [DataRow("補正無し", true)]
    [DataRow("建物自体の削除", true)]
    [DataRow("平面形状種の下端高の補正", true)]
    [DataRow("平面形状種の形状反転_index0", true)]
    [DataRow("平面形状種の形状反転_index1", true)]
    [DataRow("平面形状種の形状不正_index0", true)]
    [DataRow("平面形状種の形状不正_index1", true)]
    [DataRow("平面形状種の頂点0件_index0", true)]
    [DataRow("平面形状種の頂点0件_index1", true)]
    public void CorrectOrRemoveInvalidShapeTest(string folderName, bool expectedIsSuccess)
    {
        // カレントディレクトリに設定
        var currentDir = Path.Combine(TestDataFolder, "CorrectOrRemoveInvalidShapeTest", folderName);
        Directory.SetCurrentDirectory(currentDir);

        var absolutePath = Directory.GetCurrentDirectory();

        var result = SmfrdatFileLoader.CorrectOrRemoveInvalidShape();

        Assert.AreEqual(expectedIsSuccess, result);

        var createdFilePath = Path.Combine(absolutePath, "GeneFile", "gene_out", "smfrdat.txt");
        var expectedFilePath = Path.Combine(absolutePath, "GeneFile", "gene_out", "smfrdat_expected.txt");

        if (expectedIsSuccess)
        {
            UnitTestHelper.CheckEqualsContent(expectedFilePath, createdFilePath);
        }

        // 作成したファイルの削除
        File.Delete(createdFilePath);
    }
}
