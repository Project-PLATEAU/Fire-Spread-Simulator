using System.Numerics;
using SimulationSourceFileCreator.Model;

namespace SimulationSourceFileCreator.Utility
{
    /// <summary>
    /// ポリゴンのユーティリティクラス
    /// </summary>
    internal static class PolygonUtility
    {
        /// <summary>
        /// 浮動小数誤差
        /// </summary>
        private const double EPS = 1e-12;

        /// <summary>
        /// ポリゴンの符号付き面積を取得します。
        /// </summary>
        /// <param name="bldgShapePoints">ポリゴンの頂点列</param>
        /// <returns>符号付き面積</returns>
        /// <remarks>
        /// 上から見て反時計回り：正、時計回り：負
        /// </remarks>
        internal static double CalculatePolygonOrientation(List<BldgShapePoint> bldgShapePoints)
        {
            var n = bldgShapePoints.Count;

            if (n == 0)
            {
                return 0d;
            }

            var start = bldgShapePoints[0];
            var end = bldgShapePoints[n - 1];

            if (start.X.Equals(end.X) && start.Y.Equals(end.Y))
            {
                // 始点と終点が同一の場合は最後の1点を判定から除外する
                n--;
            }

            var sum = 0d;
            for (int i = 0; i < n; i++)
            {
                var p1 = bldgShapePoints[i];
                var p2 = bldgShapePoints[(i + 1) % n]; // 次の頂点（リストの最後と最初を繋ぐ）

                sum += (p1.X * p2.Y) - (p2.X * p1.Y);
            }

            return sum / 2d;
        }

        /// <summary>
        /// ポリゴンが自己交差しているかを判定します。
        /// </summary>
        /// <param name="bldgShapePoints">ポリゴンの頂点列</param>
        /// <returns>true：自己交差あり、false：自己交差なし</returns>
        /// <remarks>
        /// - 計算量は O(n^2) です。<br/>
        /// - 隣接辺の端点共有は自己交差とみなしません。<br/>
        /// - 共線オーバーラップは自己交差とみなします。<br/>
        /// - 頂点数が3以下の場合は常に false を返します。<br/>
        /// </remarks>
        internal static bool HasSelfIntersection(List<BldgShapePoint> bldgShapePoints)
        {
            var points = new List<Vector2>();
            foreach (var point in bldgShapePoints)
            {
                points.Add(new Vector2((float)point.X, (float)point.Y));
            }

            var n = points.Count;

            if (n == 0)
            {
                return false;
            }

            var start = points[0];
            var end = points[n - 1];

            if (start.X.Equals(end.X) && start.Y.Equals(end.Y))
            {
                // 始点と終点が同一の場合は最後の1点を判定から除外する
                n--;
            }

            if (n < 4)
            {
                return false; // 三角形は自己交差しない
            }

            for (var i = 0; i < n; i++)
            {
                var i2 = (i + 1) % n;
                var a1 = points[i];
                var a2 = points[i2];

                for (var j = i + 1; j < n; j++)
                {
                    var j2 = (j + 1) % n;
                    var b1 = points[j];
                    var b2 = points[j2];

                    // 隣接辺は除外（共有頂点は交差とみなさない）
                    var sharesVertex = (i == j) || (i2 == j) || (i == j2) || (i2 == j2);
                    if (sharesVertex)
                    {
                        continue;
                    }

                    if (SegmentsIntersect(a1, a2, b1, b2))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///  2つの線分 (p1, q1) と (p2, q2) が交差するかを判定します。
        /// </summary>
        /// <param name="p1">線分1の始点</param>
        /// <param name="p2">線分1の終点</param>
        /// <param name="q1">線分2の始点</param>
        /// <param name="q2">線分2の終点</param>
        /// <returns>true：交差あり、false：交差なし</returns>
        private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            var pv = p2 - p1;
            var qv = q2 - q1;

            var v1 = p1 - q1;
            var v2 = p2 - q1;
            var v3 = q1 - p1;
            var v4 = q2 - p1;
            return Cross(qv, v1) * Cross(qv, v2) < 0 && Cross(pv, v3) * Cross(pv, v4) < 0;
        }

        /// <summary>
        /// 外積を取得します。
        /// </summary>
        /// <param name="p1">線分1</param>
        /// <param name="p2">線分2</param>
        /// <returns>外積</returns>
        private static float Cross(Vector2 p1, Vector2 p2)
        {
            return (p1.X * p2.Y) - (p2.X * p1.Y);
        }
    }
}
