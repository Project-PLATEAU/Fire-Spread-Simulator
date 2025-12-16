using System.Text;
using SimulationCommonLibrary.Utility;

namespace UnitTest.CommonLibrary;

/// <summary>
/// FileUtilityテストクラス
/// </summary>
[TestClass]
public class FileUtilityTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    internal static string TestDataFolder => Path.Combine(@"..\..\..\CommonLibrary", "TestData", "FileUtilityTest");

    /// <summary>
    /// 正常
    /// </summary>
    [TestMethod]
    public void CopyAndRewriteTest_Success()
    {
        var orgFilePath = Path.Combine(TestDataFolder, "org_file.txt");
        var destFilePath = Path.Combine(TestDataFolder, "test_file1.txt");
        var expectedFilePath = Path.Combine(TestDataFolder, "test_file1_expected.txt");

        var isSuccess = FileUtility.CopyAndRewrite(
            orgFilePath,
            destFilePath,
            (line) =>
            {
                if (line.StartsWith("BB"))
                {
                    return "Changed Line";
                }

                return line;
            },
            new UTF8Encoding(false));
        Assert.IsTrue(isSuccess);

        UnitTestHelper.CheckEqualsContent(expectedFilePath, destFilePath);

        // 複製したテスト用ファイルの削除
        File.Delete(destFilePath);
    }

    /// <summary>
    /// 不正：引数不正
    /// </summary>
    /// <param name="sourceFilePath">複製元ファイルパス</param>
    /// <param name="destFilePath">複製先ファイルパス</param>
    /// <param name="rewriteActionIsNull">書き換え関数をnullにするかどうか</param>
    /// <param name="encodingIsNull">エンコーディングをnullにするかどうか</param>
    /// <param name="expectedExceptionItem">Exceptionの対象の引数名</param>
    [TestMethod]
    [DataRow(null,           "test_file2.txt", false, false, "sourceFilePath")]
    [DataRow("",             "test_file2.txt", false, false, "sourceFilePath")]
    [DataRow("org_file.txt", null,             false, false, "destFilePath")]
    [DataRow("org_file.txt", "",               false, false, "destFilePath")]
    [DataRow("org_file.txt", "test_file2.txt", true,  false, "rewriteAction")]
    [DataRow("org_file.txt", "test_file2.txt", false, true,  "encoding")]
    public void CopyAndRewriteTest_Exception(string sourceFilePath, string destFilePath, bool rewriteActionIsNull, bool encodingIsNull, string expectedExceptionItem)
    {
        Func<string, string> rewriteAction = (line) => line;
        if (rewriteActionIsNull)
        {
            rewriteAction = null;
        }

        UTF8Encoding encoding = new UTF8Encoding(false);
        if (encodingIsNull)
        {
            encoding = null;
        }

        var ex = Assert.ThrowsException<ArgumentNullException>(
            () => FileUtility.CopyAndRewrite(sourceFilePath, destFilePath, rewriteAction, encoding));
        Assert.IsTrue(ex.Message.Contains(expectedExceptionItem));
    }

    /// <summary>
    /// 失敗
    /// </summary>
    /// <param name="sourceFileName">複製元ファイル名</param>
    /// <param name="destFileName">複製先ファイル名</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("org_file_dummy.txt", "test_file3.txt")] // 複製元ファイルがない
    [DataRow("org_file.txt",       "org_file.txt")]   // 複製元ファイルと複製先ファイルが同じ
    public void CopyAndRewriteTest_Failure(string sourceFileName, string destFileName)
    {
        var orgFilePath = Path.Combine(TestDataFolder, sourceFileName);
        var destFilePath = Path.Combine(TestDataFolder, destFileName);

        var isSuccess = FileUtility.CopyAndRewrite(orgFilePath, destFilePath, (line) => line, new UTF8Encoding(false));
        Assert.IsFalse(isSuccess);
    }
}
