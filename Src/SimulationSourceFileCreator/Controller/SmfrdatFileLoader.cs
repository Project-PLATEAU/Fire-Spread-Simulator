using System.IO;
using System.Text;
using SimulationSourceFileCreator.Model;
using SimulationSourceFileCreator.Utility;

namespace SimulationSourceFileCreator.Controller
{
    /// <summary>
    /// データ変換ツール実行結果ファイルの操作クラス
    /// </summary>
    internal class SmfrdatFileLoader
    {
        /// <summary>
        /// データ補正を行います。
        /// </summary>
        /// <returns>成否</returns>
        internal static bool CorrectOrRemoveInvalidShape()
        {
            var currentDir = Directory.GetCurrentDirectory();
            var sourceFilePath = Path.Combine(currentDir, "GeneFile", "gene_out", "smfrdat_source.txt");
            var destFilePath = Path.Combine(currentDir, "GeneFile", "gene_out", "smfrdat.txt");

            var removeBldgCount = 0;
            var isBldgStart = false;

            using (var sr = new StreamReader(sourceFilePath, new UTF8Encoding(false)))
            using (var sw = new StreamWriter(destFilePath, false, new UTF8Encoding(false)))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (!line.Contains("bldg"))
                    {
                        if (isBldgStart)
                        {
                            App.Logger.Error("データ変換ツール「GeneFile/plateau_conv.exe」の結果ファイルの形式不備");
                            return false;
                        }

                        sw.WriteLine(line);
                        continue;
                    }

                    isBldgStart = true;

                    // 1棟分のデータ収集
                    var bldg = GetBldgSmfrdat(line, sr);
                    if (bldg == null)
                    {
                        return false;
                    }

                    // 1棟分のデータチェック（平面形状種の形状チェック）
                    CheckAndModifyOrientation(bldg);

                    // 1棟分のデータチェック（平面形状種の頂点数チェック）
                    if (bldg.HasZeroPointShape)
                    {
                        // データ修正
                        var isSuccess = RemoveZeroPointShape(bldg);

                        if (!isSuccess)
                        {
                            removeBldgCount++;
                            continue;
                        }
                    }

                    // 1棟分のデータ出力
                    sw.WriteLine(bldg.LineData);
                    foreach (var shape in bldg.BldgShapes)
                    {
                        if (bldg.GetShapeCount() != 1)
                        {
                            // 平面形状種が複数ある場合は平面形状下端高さを調整する
                            shape.EditBottomHeight();
                        }

                        sw.WriteLine(shape.LineData);
                        foreach (var point in shape.BldgShapePoints)
                        {
                            sw.WriteLine(point.LineData);
                        }
                    }

                    foreach (var attachment in bldg.BldgAttachments)
                    {
                        sw.WriteLine(attachment);
                    }

                    foreach (var opening in bldg.BldgOpenings)
                    {
                        sw.WriteLine(opening);
                    }
                }
            }

            // 建物自体を削除した場合
            if (removeBldgCount != 0)
            {
                var tempFilePath = Path.Combine(currentDir, "GeneFile", "gene_out", "smfrdat.temp");

                // ヘッダーの建物件数を修正
                using (var sr = new StreamReader(destFilePath, new UTF8Encoding(false)))
                using (var sw = new StreamWriter(tempFilePath, false, new UTF8Encoding(false)))
                {
                    var lineCount = 0;

                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();

                        if (lineCount == 5)
                        {
                            _ = int.TryParse(line, out var bldgCount);
                            bldgCount -= removeBldgCount;

                            line = bldgCount.ToString();
                        }

                        sw.WriteLine(line);

                        lineCount++;
                    }
                }

                // 元のファイルを一時ファイルで置き換える
                File.Delete(destFilePath);
                File.Move(tempFilePath, destFilePath);
            }

            return true;
        }

        /// <summary>
        /// 地上階数を収集します。
        /// </summary>
        /// <param name="bldgAboveFloorNumDict">建物IDと地上階数のディクショナリ（key = bldgId、vaule = 地上階数）</param>
        /// <returns>成否</returns>
        internal static bool CollectAboveFloorNum(out Dictionary<string, int> bldgAboveFloorNumDict)
        {
            bldgAboveFloorNumDict = []; // key = bldgId、vaule = 地上階数

            var currentDir = Directory.GetCurrentDirectory();
            var filePath = Path.Combine(currentDir, "GeneFile", "gene_out", "smfrdat.txt");

            using (var sr = new StreamReader(filePath, new UTF8Encoding(false)))
            {
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();

                    if (!line.Contains("bldg"))
                    {
                        continue;
                    }

                    // 1棟分のデータ収集
                    var bldg = GetBldgSmfrdat(line, sr);
                    if (bldg == null)
                    {
                        return false;
                    }

                    var totalFloorNum = 0;
                    foreach (var bldgShape in bldg.BldgShapes)
                    {
                        var floorNum = bldgShape.GetFloorNum();
                        totalFloorNum += floorNum;
                    }

                    var bldgId = bldg.GetBldgId();

                    if (bldgAboveFloorNumDict.ContainsKey(bldgId))
                    {
                        // ここには来ないはず
                        App.Logger.Error($"ファイルの内容不備（同じBldgIdが複数記載されている）filePath = {filePath}");
                        return false;
                    }

                    bldgAboveFloorNumDict.Add(bldgId, totalFloorNum);
                }
            }

            return true;
        }

        /// <summary>
        /// 1棟分の建物情報を収集します。
        /// </summary>
        /// <param name="line">最初の1行目のデータ</param>
        /// <param name="sr">読み込みストリーム<see cref="StreamReader"/></param>
        /// <returns>建物情報</returns>
        private static BldgSmfrdat? GetBldgSmfrdat(string line, StreamReader sr)
        {
            var bldg = BldgSmfrdat.Create(line);
            if (bldg == null)
            {
                return null;
            }

            for (var i = 0; i < bldg.GetShapeCount(); i++)
            {
                var shapeLine = sr.ReadLine();
                var bldgShape = BldgShape.Create(shapeLine);
                if (bldgShape == null)
                {
                    return null;
                }

                bldg.BldgShapes.Add(bldgShape);

                for (var j = 0; j < bldgShape.PointCount; j++)
                {
                    var pointLine = sr.ReadLine();
                    var bldgShapePoint = BldgShapePoint.Create(pointLine);
                    if (bldgShapePoint == null)
                    {
                        return null;
                    }

                    bldgShape.BldgShapePoints.Add(bldgShapePoint);
                }
            }

            for (var i = 0; i < bldg.GetAttachmentCount(); i++)
            {
                var attachmentLine = sr.ReadLine();
                if (attachmentLine == null)
                {
                    return null;
                }

                bldg.BldgAttachments.Add(attachmentLine);
            }

            for (var i = 0; i < bldg.GetOpeningCount(); i++)
            {
                var openingLine = sr.ReadLine();
                if (openingLine == null)
                {
                    return null;
                }

                bldg.BldgOpenings.Add(openingLine);
            }

            return bldg;
        }

        /// <summary>
        /// 頂点数が0件の平面形状種を削除します。
        /// </summary>
        /// <param name="bldg">建物情報</param>
        /// <returns>成否</returns>
        private static bool RemoveZeroPointShape(BldgSmfrdat bldg)
        {
            var hasValidShape = false;

            foreach (var shape in bldg.BldgShapes)
            {
                if (shape.PointCount != 0)
                {
                    hasValidShape = true;
                    break;
                }
            }

            if (!hasValidShape)
            {
                // すべての平面形状種の頂点数が0件の場合　→　建物自体を削除
                App.Logger.Warn($"建物自体を削除（すべての平面形状種の頂点数が0件）bldgId = {bldg.GetBldgId()}");
                return false;
            }

            // 頂点数が0件の平面形状種がなくなるまで再帰的に実行
            RemoveZeroPointShapeInternal(bldg);

            var shapeCount = bldg.BldgShapes.Count;
            bldg.SetShapeCount(shapeCount);

            return true;
        }

        /// <summary>
        /// 頂点数が0件の平面形状種を削除します。頂点数が0件の平面形状種がなくなるまで再帰的に実行します。
        /// </summary>
        /// <param name="bldg">建物情報</param>
        private static void RemoveZeroPointShapeInternal(BldgSmfrdat bldg)
        {
            var hasInvalidShape = false;

            foreach (var shape in bldg.BldgShapes)
            {
                if (shape.PointCount == 0)
                {
                    hasInvalidShape = true;
                    break;
                }
            }

            if (!hasInvalidShape)
            {
                return;
            }

            for (var index = 0; index < bldg.GetShapeCount(); index++)
            {
                var nowShape = bldg.BldgShapes[index];

                if (nowShape.PointCount == 0)
                {
                    App.Logger.Warn($"平面形状種を削除（平面形状種の頂点数が0件）bldgId = {bldg.GetBldgId()}, index = {index}");

                    if (index == 0)
                    {
                        var nextShape = bldg.BldgShapes[index + 1];
                        nextShape.SetBottomHeight(nowShape.GetBottomHeight());
                        nextShape.AddFloorNum(nowShape.GetFloorNum());
                    }
                    else
                    {
                        var prevShape = bldg.BldgShapes[index - 1];
                        prevShape.SetTopHeight(nowShape.GetTopHeight());
                        prevShape.AddFloorNum(nowShape.GetFloorNum());
                    }

                    bldg.BldgShapes.Remove(nowShape);
                    break;
                }
            }

            RemoveZeroPointShapeInternal(bldg);
        }

        /// <summary>
        /// 平面形状種のポリゴンの形状をチェックして修正します。
        /// </summary>
        /// <param name="bldg">建物情報</param>
        private static void CheckAndModifyOrientation(BldgSmfrdat bldg)
        {
            var index = -1;
            foreach (var shape in bldg.BldgShapes)
            {
                index++;

                if (shape.PointCount == 0)
                {
                    continue;
                }

                var isSelfIntersection = PolygonUtility.HasSelfIntersection(shape.BldgShapePoints);

                if (isSelfIntersection)
                {
                    // 自己交差　→　不正の為、頂点を削除
                    App.Logger.Warn($"平面形状種の頂点を削除（0件にする）（形状不正：自己交差）bldgId = {bldg.GetBldgId()}, index = {index}");
                    shape.ClearBldgShapePoints();
                    continue;
                }

                var orientation = PolygonUtility.CalculatePolygonOrientation(shape.BldgShapePoints);

                if (orientation > 0)
                {
                    // 反時計回り　→　OK（何もしない）
                    continue;
                }

                if (orientation < 0)
                {
                    // 時計回り　→　反転させる
                    App.Logger.Warn($"平面形状種の頂点順を反転（形状不正：頂点が時計回り）bldgId = {bldg.GetBldgId()}, index = {index}");
                    shape.BldgShapePoints.Reverse();
                    shape.BldgShapePoints.First().SetRoopFlag(1);
                    shape.BldgShapePoints.Last().SetRoopFlag(0);
                    continue;
                }

                // それ以外（頂点が一直線上）　→　不正の為、頂点を削除
                App.Logger.Warn($"平面形状種の頂点を削除（0件にする）（形状不正：頂点が一直線上）bldgId = {bldg.GetBldgId()}, index = {index}");
                shape.ClearBldgShapePoints();
            }
        }
    }
}
