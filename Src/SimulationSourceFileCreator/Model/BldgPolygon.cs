namespace SimulationSourceFileCreator.Model
{
    /// <summary>
    /// CityGMLファイルの建物ポリゴンのモデルクラス
    /// </summary>
    internal class BldgPolygon
    {
        /// <summary>
        /// 外側境界（外周）
        /// </summary>
        internal List<BldgPolygonPos> Exterior { get; set; } = [];

        /// <summary>
        /// 内側境界（穴）
        /// </summary>
        internal List<List<BldgPolygonPos>> Holes { get; set; } = [];

        /// <summary>
        /// 位置情報
        /// </summary>
        internal struct BldgPolygonPos
        {
            /// <summary>
            /// 経度（単位：度）
            /// </summary>
            public double Longitude;

            /// <summary>
            /// 緯度（単位：度）
            /// </summary>
            public double Latitude;

            /// <summary>
            /// 高さ（単位：m）
            /// </summary>
            public double Height;

            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="longitude">経度（単位：度）</param>
            /// <param name="latitude">緯度（単位：度）</param>
            /// <param name="height">高さ（単位：m）</param>
            public BldgPolygonPos(double longitude, double latitude, double height)
            {
                this.Longitude = longitude;
                this.Latitude = latitude;
                this.Height = height;
            }

            /// <summary>
            /// Nan
            /// </summary>
            public static BldgPolygonPos NaN => new BldgPolygonPos(double.NaN, double.NaN, double.NaN);
        }
    }
}
