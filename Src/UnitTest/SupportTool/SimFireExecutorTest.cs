using System.Reflection;
using SimulationSupportTool.Controller;
using SimulationSupportTool.Model;

namespace UnitTest.SupportTool;

/// <summary>
/// SimFireExecutorテストクラス
/// </summary>
[TestClass]
public class SimFireExecutorTest
{
    /// <summary>
    /// テストデータフォルダパス
    /// </summary>
    private static string TestDataFolder => Path.Combine(@"..\..\..\SupportTool", "TestData", "SimFireExecutorTest");

    /// <summary>
    /// <see cref="SimFireExecutor.CreateSimulationIniFile"/>のテスト
    /// </summary>
    [TestMethod]
    public void CreateSimulationIniFileTest()
    {
        // 実行
        var workingFolderPath = Path.Combine(TestDataFolder, "working_folder");

        var instance = this.CreateInstance(workingFolderPath, string.Empty, string.Empty, string.Empty, string.Empty);
        var method = typeof(SimFireExecutor).GetMethod("CreateSimulationIniFile", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (bool?)method.Invoke(instance, [1]);

        // 結果確認
        Assert.IsTrue(result);

        var createdFilePath = Path.Combine(workingFolderPath, "simfire.ini");
        var expectedFilePath = Path.Combine(workingFolderPath, "simfire_expected.ini");

        UnitTestHelper.CheckEqualsContent(expectedFilePath, createdFilePath);

        // 作成したファイルの削除
        File.Delete(createdFilePath);
    }

    /// <summary>
    /// <see cref="SimFireExecutor.CreateFirePointDatFile"/>のテスト
    /// </summary>
    /// <param name="startMinutes">出火時間</param>
    /// <param name="expectedIsSuccess">期待値：成否</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("10", true)]
    [DataRow("aa", false)]
    public void CreateFirePointDatFileTest(string startMinutes, bool expectedIsSuccess)
    {
        // 引数データ準備
        var firePointList = new List<FirePoint>
        {
             FirePoint.CreateFromResult(new JsFirePointResult() { Building = new JsBuilding() { BldgId = "Test001", }, }),
             FirePoint.CreateFromResult(new JsFirePointResult() { Building = new JsBuilding() { BldgId = "Test002", }, }),
             FirePoint.CreateFromResult(new JsFirePointResult() { Building = new JsBuilding() { BldgId = "Test003", }, }),
        };

        firePointList[0].StartMinutes = startMinutes;
        firePointList[0].SelectedStory = 1;

        firePointList[1].StartMinutes = "20";
        firePointList[1].SelectedStory = 2;

        firePointList[2].StartMinutes = "30";
        firePointList[2].SelectedStory = 3;

        // 実行
        var simConditionFolderPath = Path.Combine(TestDataFolder, "sim_cond");

        var instance = this.CreateInstance(string.Empty, string.Empty, simConditionFolderPath, string.Empty, string.Empty);
        var method = typeof(SimFireExecutor).GetMethod("CreateFirePointDatFile", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (bool?)method.Invoke(instance, [firePointList]);

        // 結果確認
        Assert.AreEqual(expectedIsSuccess, result);

        var createdFilePath = Path.Combine(simConditionFolderPath, "outbreak.dat");
        var expectedFilePath = Path.Combine(simConditionFolderPath, "outbreak_expected.dat");

        if (expectedIsSuccess)
        {
            UnitTestHelper.CheckEqualsContent(expectedFilePath, createdFilePath);
        }

        // 作成したファイルの削除
        File.Delete(createdFilePath);
    }

    /// <summary>
    /// <see cref="SimFireExecutor.CreateWindConditionDatFile"/>のテスト
    /// </summary>
    /// <param name="startMinutes">時間</param>
    /// <param name="expectedIsSuccess">期待値：成否</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("10", true)]
    [DataRow("aa", false)]
    public void CreateWindConditionDatFileTest(string startMinutes, bool expectedIsSuccess)
    {
        // 引数データ準備
        // ※時刻の順でソートされることも確認する
        var windConditionList = new List<WindCondition>()
        {
            new WindCondition(0) { StartMinutes = "0",          WindDirection = 0d,     WindSpeed = 0d },
            new WindCondition(1) { StartMinutes = "30",         WindDirection = 130.5d, WindSpeed = 13.5d },
            new WindCondition(2) { StartMinutes = startMinutes, WindDirection = 110.5d, WindSpeed = 11.5d },
            new WindCondition(3) { StartMinutes = "20",         WindDirection = 120.5d, WindSpeed = 12.5d },
        };

        // 実行
        var simConditionFolderPath = Path.Combine(TestDataFolder, "sim_cond");

        var instance = this.CreateInstance(string.Empty, string.Empty, simConditionFolderPath, string.Empty, string.Empty);
        var method = typeof(SimFireExecutor).GetMethod("CreateWindConditionDatFile", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (bool?)method.Invoke(instance, [windConditionList]);

        // 結果確認
        Assert.AreEqual(expectedIsSuccess, result);

        var createdFilePath = Path.Combine(simConditionFolderPath, "weather.dat");
        var expectedFilePath = Path.Combine(simConditionFolderPath, "weather_expected.dat");

        if (expectedIsSuccess)
        {
            UnitTestHelper.CheckEqualsContent(expectedFilePath, createdFilePath);
        }

        // 作成したファイルの削除
        File.Delete(createdFilePath);
    }

    /// <summary>
    /// <see cref="SimFireExecutor.CreateSimulationMapFiles"/>のテスト
    /// </summary>
    /// <param name="folderName">シミュレーションデータフォルダ名</param>
    /// <param name="expectedIsSuccess">期待値：成否</param>
    [TestMethod]
    [DoNotParallelize]
    [DataRow("正常",                   true)]
    [DataRow("平面直角座標系が異なる", true)]
    [DataRow("先頭行の件数不正",       false)]
    [DataRow("ファイル不足_builds",    false)]
    [DataRow("ファイル不足_rooms",     false)]
    public void CreateSimulationMapFilesTest(string folderName, bool expectedIsSuccess)
    {
        // 引数データ準備
        var sourceFolderPath = Path.Combine(TestDataFolder, "input_sim_data", folderName);
        var selectedSimulationRangeMeshNumbers = new string[] { "47300284", "47300285" };

        // 実行
        var simMapFolderPath = Path.Combine(TestDataFolder, "sim_map");

        var instance = this.CreateInstance(string.Empty, string.Empty, string.Empty, simMapFolderPath, string.Empty);
        var method = typeof(SimFireExecutor).GetMethod("CreateSimulationMapFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (bool?)method.Invoke(instance, [sourceFolderPath, selectedSimulationRangeMeshNumbers]);

        // 結果確認
        Assert.AreEqual(expectedIsSuccess, result);

        var createdFilePath_builds = Path.Combine(simMapFolderPath, "builds.dat");
        var expectedFilePath_builds = Path.Combine(simMapFolderPath, "builds_expected.dat");

        var createdFilePath_rooms = Path.Combine(simMapFolderPath, "rooms.dat");
        var expectedFilePath_rooms = Path.Combine(simMapFolderPath, "rooms_expected.dat");

        if (expectedIsSuccess)
        {
            UnitTestHelper.CheckEqualsContent(expectedFilePath_builds, createdFilePath_builds, 3);
            UnitTestHelper.CheckEqualsContent(expectedFilePath_rooms, createdFilePath_rooms, 3);
        }

        // 作成したファイルの削除
        File.Delete(createdFilePath_builds);
        File.Delete(createdFilePath_rooms);
    }

    /// <summary>
    /// <see cref="SimFireExecutor.CreateSimulationInformationFile"/>のテスト
    /// </summary>
    [TestMethod]
    public void CreateSimulationInformationFileTest()
    {
        // 引数データ準備
        var selectedSimulationRangeMeshNumbers = new string[] { "47300284", "47300285" };

        // 実行
        var simOutputFolderPath = Path.Combine(TestDataFolder, "sim_out");

        var instance = this.CreateInstance(string.Empty, string.Empty, string.Empty, string.Empty, simOutputFolderPath);
        var method = typeof(SimFireExecutor).GetMethod("CreateSimulationInformationFile", BindingFlags.Instance | BindingFlags.NonPublic);
        var result = (bool?)method.Invoke(instance, [1, selectedSimulationRangeMeshNumbers]);

        // 結果確認
        Assert.IsTrue(result);

        var createdFilePath = Path.Combine(simOutputFolderPath, "sim_info.txt");
        var expectedFilePath = Path.Combine(simOutputFolderPath, "sim_info_expected.txt");

        UnitTestHelper.CheckEqualsContent(expectedFilePath, createdFilePath, 6);

        // 作成したファイルの削除
        File.Delete(createdFilePath);
    }

    private object? CreateInstance(string workingFolderPath, string exeFilePath, string simConditionFolderPath, string simMapFolderPath, string simOutputFolderPath)
    {
        var ctor = typeof(SimFireExecutor).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [typeof(string), typeof(string), typeof(string), typeof(string), typeof(string)], // 引数型の配列
            null);

        Assert.IsNotNull(ctor);

        var instance = ctor.Invoke([workingFolderPath, exeFilePath, simConditionFolderPath, simMapFolderPath, simOutputFolderPath]);
        Assert.IsNotNull(instance);

        return instance;
    }
}
