namespace SimulationSourceFileCreator.Model
{
#pragma warning disable SA1402 // File may only contain a single type

    /// <summary>
    /// データ変換ツール実行結果ファイルの建物情報のモデルクラス
    /// </summary>
    internal class BldgSmfrdat
    {
        private readonly int bldgIdIndex = 0;           // 0 建物ID
        private readonly int shapeCountIndex = 2;       // 2 平面形状種数
        private readonly int attachmentCountIndex = 3;  // 3 付属面数
        private readonly int openingCountIndex = 4;     // 4 開口部数

        /// <summary>
        /// 行データ
        /// </summary>
        internal string LineData { get; private set; } = string.Empty;

        /// <summary>
        /// 平面形状種リスト
        /// </summary>
        internal List<BldgShape> BldgShapes { get; private set; } = [];

        /// <summary>
        /// 付属面リスト
        /// </summary>
        internal List<string> BldgAttachments { get; private set; } = [];

        /// <summary>
        /// 開口部リスト
        /// </summary>
        internal List<string> BldgOpenings { get; private set; } = [];

        /// <summary>
        /// 頂点数0件の平面形状種があるかどうか
        /// </summary>
        internal bool HasZeroPointShape => this.BldgShapes.Any(s => s.PointCount == 0);

        /// <summary>
        /// 建物情報を作成します。
        /// </summary>
        /// <param name="line">行データ</param>
        /// <returns>建物情報</returns>
        internal static BldgSmfrdat? Create(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var bldg = new BldgSmfrdat
            {
                LineData = line,
            };

            return bldg;
        }

        /// <summary>
        /// 行データから指定インデックスの値を取得します。
        /// </summary>
        /// <typeparam name="T">取得データ型</typeparam>
        /// <param name="line">行データ</param>
        /// <param name="index">インデックス</param>
        /// <returns>取得した値</returns>
        /// <exception cref="NotSupportedException">対応していないデータ型が指定された場合に発生</exception>
        internal static T? GetItemValue<T>(string line, int index)
        {
            var words = line.Split(',');
            if (words.Length < index + 1)
            {
                return default;
            }

            Type t = typeof(T);

            if (t == typeof(int))
            {
                if (!int.TryParse(words[index], out var value))
                {
                    return (T)(object)-1;
                }

                return (T)(object)value;
            }

            if (t == typeof(double))
            {
                if (!double.TryParse(words[index], out var value))
                {
                    return (T)(object)-1;
                }

                return (T)(object)value;
            }

            if (t == typeof(string))
            {
                return (T)(object)words[index];
            }

            throw new NotSupportedException($"型 [{t}] はサポートしません。");
        }

        /// <summary>
        /// 行データの指定インデックスに値を設定します。
        /// </summary>
        /// <param name="line">行データ</param>
        /// <param name="index">インデックス</param>
        /// <param name="value">設定する値</param>
        /// <returns>設定後の行データ</returns>
        internal static string SetItemValue(string line, int index, string value)
        {
            var words = line.Split(',');
            if (words.Length < index + 1)
            {
                return line;
            }

            words[index] = value;
            return string.Join(',', words);
        }

        /// <summary>
        /// 建物IDを取得します。
        /// </summary>
        /// <returns>建物ID</returns>
        internal string GetBldgId()
        {
            var bldgId = GetItemValue<string>(this.LineData, this.bldgIdIndex);
            return bldgId ?? string.Empty;
        }

        /// <summary>
        /// 平面形状種数を取得します。
        /// </summary>
        /// <returns>平面形状種数</returns>
        internal int GetShapeCount()
        {
            return GetItemValue<int>(this.LineData, this.shapeCountIndex);
        }

        /// <summary>
        /// 付属面数を取得します。
        /// </summary>
        /// <returns>付属面数</returns>
        internal int GetAttachmentCount()
        {
            return GetItemValue<int>(this.LineData, this.attachmentCountIndex);
        }

        /// <summary>
        /// 開口部数を取得します。
        /// </summary>
        /// <returns>開口部数</returns>
        internal int GetOpeningCount()
        {
            return GetItemValue<int>(this.LineData, this.openingCountIndex);
        }

        /// <summary>
        /// 平面形状種数を設定します。
        /// </summary>
        /// <param name="shapeCount">平面形状種数</param>
        internal void SetShapeCount(int shapeCount)
        {
            this.LineData = SetItemValue(this.LineData, this.shapeCountIndex, shapeCount.ToString());
        }
    }

    /// <summary>
    /// 平面形状種のモデルクラス
    /// </summary>
    internal class BldgShape
    {
        private readonly int pointCountIndex = 0;   // 0 頂点数
        private readonly int bottomHeightIndex = 1; // 1 平面形状下端高さ
        private readonly int topHeightIndex = 2;    // 2 平面形状上端高さ
        private readonly int floorNumIndex = 4;     // 4 階数

        /// <summary>
        /// 行データ
        /// </summary>
        internal string LineData { get; set; } = string.Empty;

        /// <summary>
        /// 0 頂点数
        /// </summary>
        internal int PointCount => BldgSmfrdat.GetItemValue<int>(this.LineData, this.pointCountIndex);

        /// <summary>
        /// 頂点リスト
        /// </summary>
        internal List<BldgShapePoint> BldgShapePoints { get; set; } = [];

        /// <summary>
        /// 平面形状種を作成します。
        /// </summary>
        /// <param name="line">行データ</param>
        /// <returns>平面形状種</returns>
        internal static BldgShape? Create(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var bldgShape = new BldgShape
            {
                LineData = line,
            };

            return bldgShape;
        }

        /// <summary>
        /// 階数を取得します。
        /// </summary>
        /// <returns>階数</returns>
        internal int GetFloorNum()
        {
            return BldgSmfrdat.GetItemValue<int>(this.LineData, this.floorNumIndex);
        }

        /// <summary>
        /// 階数を加算します。
        /// </summary>
        /// <param name="addfloorNum">加算する階数</param>
        internal void AddFloorNum(int addfloorNum)
        {
            var floorNum = BldgSmfrdat.GetItemValue<int>(this.LineData, this.floorNumIndex);

            floorNum += addfloorNum;

            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.floorNumIndex, floorNum.ToString());
        }

        /// <summary>
        /// 平面形状下端高さを編集します。<br/>
        /// ※0.03だけマイナスします。
        /// </summary>
        internal void EditBottomHeight()
        {
            var lowerEnd = BldgSmfrdat.GetItemValue<double>(this.LineData, this.bottomHeightIndex);

            lowerEnd += 0.3d;

            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.bottomHeightIndex, lowerEnd.ToString());
        }

        /// <summary>
        /// 平面形状下端高さを取得します。
        /// </summary>
        /// <returns>平面形状下端高さ</returns>
        internal double GetBottomHeight()
        {
            return BldgSmfrdat.GetItemValue<double>(this.LineData, this.bottomHeightIndex);
        }

        /// <summary>
        /// 平面形状下端高さを設定します。<br/>
        /// ※子要素の平面形状種の頂点の平面形状下端高さにも設定します。
        /// </summary>
        /// <param name="bottomHeight">平面形状下端高さ</param>
        internal void SetBottomHeight(double bottomHeight)
        {
            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.bottomHeightIndex, bottomHeight.ToString());

            foreach (var bldgShapePoint in this.BldgShapePoints)
            {
                bldgShapePoint.SetBottomZ(bottomHeight);
            }
        }

        /// <summary>
        /// 平面形状上端高さを取得します。
        /// </summary>
        /// <returns>平面形状上端高さ</returns>
        internal double GetTopHeight()
        {
            return BldgSmfrdat.GetItemValue<double>(this.LineData, this.topHeightIndex);
        }

        /// <summary>
        /// 平面形状上端高さを設定します。<br/>
        /// ※子要素の平面形状種の頂点の平面形状上端高さにも設定します。
        /// </summary>
        /// <param name="topHeight">平面形状上端高さ</param>
        internal void SetTopHeight(double topHeight)
        {
            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.topHeightIndex, topHeight.ToString());

            foreach (var bldgShapePoint in this.BldgShapePoints)
            {
                bldgShapePoint.SetTopZ(topHeight);
            }
        }

        /// <summary>
        /// 平面形状種の頂点を削除（0件）にします。
        /// </summary>
        internal void ClearBldgShapePoints()
        {
            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.pointCountIndex, "0");
            this.BldgShapePoints.Clear();
        }
    }

    /// <summary>
    /// 平面形状種の頂点のモデルクラス
    /// </summary>
    internal class BldgShapePoint
    {
        private readonly int xIndex = 0;        // 0 座標X
        private readonly int yIndex = 1;        // 1 座標Y
        private readonly int bottomZIndex = 2;  // 2 下座標Z
        private readonly int topZIndex = 3;     // 3 上座標Z
        private readonly int roopFlagIndex = 4; // ループ開始フラグ

        /// <summary>
        /// 行データ
        /// </summary>
        internal string LineData { get; private set; } = string.Empty;

        /// <summary>
        /// 0 座標X
        /// </summary>
        internal double X => BldgSmfrdat.GetItemValue<double>(this.LineData, this.xIndex);

        /// <summary>
        /// 1 座標Y
        /// </summary>
        internal double Y => BldgSmfrdat.GetItemValue<double>(this.LineData, this.yIndex);

        /// <summary>
        /// 平面形状種の頂点を作成します。
        /// </summary>
        /// <param name="line">行データ</param>
        /// <returns>平面形状種の頂点</returns>
        internal static BldgShapePoint? Create(string? line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var bldgShapePoint = new BldgShapePoint
            {
                LineData = line,
            };

            return bldgShapePoint;
        }

        /// <summary>
        /// 下座標Zを設定します。
        /// </summary>
        /// <param name="bottomHeight">下座標Z</param>
        internal void SetBottomZ(double bottomHeight)
        {
            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.bottomZIndex, bottomHeight.ToString());
        }

        /// <summary>
        /// 上座標Zを設定します。
        /// </summary>
        /// <param name="topHeight">上座標Z</param>
        internal void SetTopZ(double topHeight)
        {
            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.topZIndex, topHeight.ToString());
        }

        /// <summary>
        /// ループ開始フラグを設定します。
        /// </summary>
        /// <param name="roopFlag">ループ開始頂点は1、その他は0</param>
        internal void SetRoopFlag(int roopFlag)
        {
            this.LineData = BldgSmfrdat.SetItemValue(this.LineData, this.roopFlagIndex, roopFlag.ToString());
        }
    }

#pragma warning restore SA1402 // File may only contain a single type
}
