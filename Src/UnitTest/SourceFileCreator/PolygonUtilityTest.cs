using SimulationSourceFileCreator.Model;
using SimulationSourceFileCreator.Utility;

namespace UnitTest.SourceFileCreator;

/// <summary>
/// PolygonUtilityテストクラス
/// </summary>
[TestClass]
public class PolygonUtilityTest
{
    /// <summary>
    /// 実データ（頂点6点）所沢市 53395313 自己交差なし bldg_bbc5a7c1-6021-4c41-b3f4-b33f1c6c1ba9
    /// </summary>
    private const string TOKOROZAWASHI =
        "-37488.200000,-25835.200000 -37490.310000,-25818.770000 -37499.810000,-25819.970000 " +
        "-37498.230000,-25832.460000 -37500.230000,-25832.710000 -37499.720000,-25836.670000";

    /// <summary>
    /// 実データ（頂点22点）寄居町 54391135 自己交差なし bldg_cf51ae7f-42e2-48e5-9ca4-b336eb4c5c21
    /// </summary>
    private const string YORIIMACHI1 =
        "-57532.92347237221,12678.971518320306 -57526.43774696213,12680.58327364113 -57526.74789247348,12681.831143482936 " +
        "-57525.90914426634,12682.039409581286 -57525.96788808756,12682.275761792056 -57523.9946242567,12682.76600616513 " +
        "-57523.30690032105,12679.99859502203 -57522.699272438855,12680.149550470378 -57522.5735953701,12679.643654520945 " +
        "-57517.5030027634,12680.903035835734 -57517.62875595777,12681.409239329683 -57517.26027570392,12681.50078236925 " +
        "-57519.96013713202,12692.365545432836 -57526.00687541775,12690.863334498801 -57525.903942333214,12690.449130251129 " +
        "-57529.138195391635,12689.64560184583 -57529.00650052496,12689.115666160938 -57534.573938597416,12687.732473620523 " +
        "-57533.98405585355,12685.358271093723 -57534.86394672339,12685.139611167235 -57533.6763361279,12680.360648754267 " +
        "-57533.29238752467,12680.4560382533";

    /// <summary>
    /// 実データ（頂点19点）寄居町 54391135 自己交差あり bldg_4b116c85-55e9-49c7-ad4c-1eb04ad18a0e
    /// </summary>
    private const string YORIIMACHI2 =
        "-57581.49913112498,12872.12976663047 -57572.368199412456,12878.200380249038 -57577.425982384586,12889.149219759773 " +
        "-57577.54158527027,12889.095817582947 -57582.4791632663,12899.78892167634 -57589.85768750494,12897.376916439278 " +
        "-57590.14951272433,12898.309713485098 -57591.7804790011,12897.808771191156 -57591.534821757974,12896.810263025201 " +
        "-57591.85435329545,12896.730369329498 -57591.1727617035,12894.001906907959 -57590.989012839025,12894.047850344577 " +
        "-57590.583866735244,12892.40169609473 -57583.674263792156,12877.437861433025 -57583.927501629914,12877.319363749166 " +
        "-57582.50557896866,12887.089042072099 -57584.08195911642,12890.502941904446 -57583.576812761305,12890.736161513794 " +
        "-57582.000404751634,12887.32220134545";

    /// <summary>
    /// <see cref="PolygonUtility.CalculatePolygonOrientation"/>のテスト
    /// </summary>
    /// <param name="expectedResult">期待値：符号付き面積</param>
    /// <param name="pointsText">座標X,座標Y の頂点座標群</param>
    [TestMethod]
    [DataRow(0, "",            DisplayName = "頂点なし")]
    [DataRow(0, "0,0",         DisplayName = "1点")]
    [DataRow(0, "0,0 0,0",     DisplayName = "1点 始点終点同一")]
    [DataRow(0, "0,0 0,1",     DisplayName = "2点")]
    [DataRow(0, "0,0 0,1 0,0", DisplayName = "2点 始点終点同一")]

    [DataRow(-0.50, "0,0 0,1  1,0",                   DisplayName = "三角形 時計回り")]
    [DataRow(-0.50, "0,0 0,1  1,0 0,0",               DisplayName = "三角形 時計回り 始点終点同一")]
    [DataRow(0.50,  "0,0 0,1 -1,0",                   DisplayName = "三角形 反時計回り")]
    [DataRow(0.50,  "0,0 0,1 -1,0 0,0",               DisplayName = "三角形 反時計回り 始点終点同一")]
    [DataRow(-1.00, "0,0 0,1  1,1  1,0",              DisplayName = "四角形")]
    [DataRow(-1.00, "0,0 0,1  1,1  1,0 0,0",          DisplayName = "四角形 始点終点同一")]
    [DataRow(1.00,  "0,0 0,1 -1,1 -1,0",              DisplayName = "四角形")]
    [DataRow(1.00,  "0,0 0,1 -1,1 -1,0 0,0",          DisplayName = "四角形 始点終点同一")]
    [DataRow(-0.75, "0,0 0,1  1,1  0.5,0.5  1,0",     DisplayName = "五角形")]
    [DataRow(-0.75, "0,0 0,1  1,1  0.5,0.5  1,0 0,0", DisplayName = "五角形 始点終点同一")]
    [DataRow(0.75,  "0,0 0,1 -1,1 -0.5,0.5 -1,0",     DisplayName = "五角形")]
    [DataRow(0.75,  "0,0 0,1 -1,1 -0.5,0.5 -1,0 0,0", DisplayName = "五角形 始点終点同一")]

    [DataRow(-1.00, "0,0 0,1 1,1 1,1 1,0",      DisplayName = "連続する同一点あり")]
    [DataRow(-0.50, "0,0 0,1 1,1 0,0.5 1,0",    DisplayName = "辺に接する点あり")]
    [DataRow(-0.25, "0,0 0,1 1,1 -0.5,0.5 1,0", DisplayName = "自己交差している")]

    [DataRow(166.95945000648499, TOKOROZAWASHI, DisplayName = "実データ（頂点6点）所沢市 53395313 自己交差なし")]
    [DataRow(156.04012322425842, YORIIMACHI1,   DisplayName = "実データ（頂点22点）寄居町 54391135 自己交差なし")]
    [DataRow(246.18561363220215, YORIIMACHI2,   DisplayName = "実データ（頂点19点）寄居町 54391135 自己交差あり")]
    public void CalculatePolygonOrientationTest(double expectedResult, string pointsText)
    {
        var bldgShapePoints = this.CreateBldgShapePoints(pointsText);

        var result = PolygonUtility.CalculatePolygonOrientation(bldgShapePoints);

        Assert.AreEqual(expectedResult, result);
    }

    /// <summary>
    /// <see cref="PolygonUtility.HasSelfIntersection"/>のテスト
    /// </summary>
    /// <param name="expectedResult">期待値：自己交差しているかどうか</param>
    /// <param name="pointsText">座標X,座標Y の頂点座標群</param>
    [TestMethod]
    [DataRow(false, "",            DisplayName = "頂点なし")]
    [DataRow(false, "0,0",         DisplayName = "1点")]
    [DataRow(false, "0,0 0,0",     DisplayName = "1点 始点終点同一")]
    [DataRow(false, "0,0 0,1",     DisplayName = "2点")]
    [DataRow(false, "0,0 0,1 0,0", DisplayName = "2点 始点終点同一")]

    [DataRow(false, "0,0 0,1  1,0",                   DisplayName = "三角形 時計回り")]
    [DataRow(false, "0,0 0,1  1,0 0,0",               DisplayName = "三角形 時計回り 始点終点同一")]
    [DataRow(false, "0,0 0,1 -1,0",                   DisplayName = "三角形 反時計回り")]
    [DataRow(false, "0,0 0,1 -1,0 0,0",               DisplayName = "三角形 反時計回り 始点終点同一")]
    [DataRow(false, "0,0 0,1  1,1  1,0",              DisplayName = "四角形")]
    [DataRow(false, "0,0 0,1  1,1  1,0 0,0",          DisplayName = "四角形 始点終点同一")]
    [DataRow(false, "0,0 0,1 -1,1 -1,0",              DisplayName = "四角形")]
    [DataRow(false, "0,0 0,1 -1,1 -1,0 0,0",          DisplayName = "四角形 始点終点同一")]
    [DataRow(false, "0,0 0,1  1,1  0.5,0.5  1,0",     DisplayName = "五角形")]
    [DataRow(false, "0,0 0,1  1,1  0.5,0.5  1,0 0,0", DisplayName = "五角形 始点終点同一")]
    [DataRow(false, "0,0 0,1 -1,1 -0.5,0.5 -1,0",     DisplayName = "五角形")]
    [DataRow(false, "0,0 0,1 -1,1 -0.5,0.5 -1,0 0,0", DisplayName = "五角形 始点終点同一")]

    [DataRow(false, "0,0 0,1 1,1 1,1 1,0",      DisplayName = "連続する同一点あり")]
    [DataRow(false, "0,0 0,1 1,1 0,0.5 1,0",    DisplayName = "辺に接する点あり")]
    [DataRow(true,  "0,0 0,1 1,1 -0.5,0.5 1,0", DisplayName = "自己交差している")]

    [DataRow(false, TOKOROZAWASHI, DisplayName = "実データ（頂点6点）所沢市 53395313 自己交差なし")]
    [DataRow(false, YORIIMACHI1,   DisplayName = "実データ（頂点22点）寄居町 54391135 自己交差なし")]
    [DataRow(true,  YORIIMACHI2,   DisplayName = "実データ（頂点19点）寄居町 54391135 自己交差あり")]
    public void HasSelfIntersectionTest(bool expectedResult, string pointsText)
    {
        var bldgShapePoints = this.CreateBldgShapePoints(pointsText);

        var result = PolygonUtility.HasSelfIntersection(bldgShapePoints);

        Assert.AreEqual(expectedResult, result);
    }

    private List<BldgShapePoint> CreateBldgShapePoints(string pointsText)
    {
        var bldgShapePoints = new List<BldgShapePoint>();

        var pointLines = pointsText.Split(' ');
        foreach (var pointLine in pointLines)
        {
            if (string.IsNullOrEmpty(pointLine))
            {
                continue;
            }

            bldgShapePoints.Add(BldgShapePoint.Create(pointLine));
        }

        return bldgShapePoints;
    }
}
