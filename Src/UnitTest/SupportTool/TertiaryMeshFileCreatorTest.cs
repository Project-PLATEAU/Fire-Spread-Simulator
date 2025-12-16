using SimulationSupportTool.Controller;

namespace UnitTest.SupportTool;

/// <summary>
/// TertiaryMeshFileCreatorテストクラス
/// </summary>
[TestClass]
public class TertiaryMeshFileCreatorTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SupportTool", "TestData", "TertiaryMeshFileCreatorTest");

    /// <summary>
    /// <see cref="TertiaryMeshFileCreator.Create"/>のテスト
    /// </summary>
    /// <param name="folderName">シミュレーションデータフォルダ名</param>
    /// <param name="expectedIsSuccess">期待値：成否</param>
    /// <param name="expectedFileName">期待値：結果ファイル名</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("正常",                                   true,  "TertiaryMesh_expected_2件.geojson")]
    [DataRow("フォルダが存在しない",                   false, "ファイルなし")]
    [DataRow("対象ファイルが1件もない",                false, "ファイルなし")]
    [DataRow("ファイル名が短い",                       true,  "TertiaryMesh_expected_1件.geojson")]
    [DataRow("メッシュ番号にあたる部分が数値ではない", true,  "TertiaryMesh_expected_1件.geojson")]
    public void CreateTest(string folderName, bool expectedIsSuccess, string expectedFileName)
    {
        // カレントディレクトリに設定
        Directory.SetCurrentDirectory(TestDataFolder);

        var folderPath = Path.Combine(TestDataFolder, "input_sim_data", folderName);

        var creator = new TertiaryMeshFileCreator();
        var result = creator.Create(folderPath);

        Assert.AreEqual(expectedIsSuccess, result);

        var createdFilePath = Path.Combine(TestDataFolder,  "workspace", "TertiaryMesh.geojson");
        var expectedFilePath = Path.Combine(TestDataFolder, "workspace", expectedFileName);

        if (expectedIsSuccess)
        {
            UnitTestHelper.CheckEqualsContent(expectedFilePath, createdFilePath);
        }

        // 作成したファイルの削除
        File.Delete(createdFilePath);
    }
}
