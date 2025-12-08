using DotSpatial.Projections;
using log4net;

namespace SimulationCommonLibrary.Utility
{
    /// <summary>
    /// GISのユーティリティクラス
    /// </summary>
    public static class GisUtility
    {
        /// <summary>
        /// WGS84の地理座標系に相当するEPSG
        /// </summary>
        private static readonly ProjectionInfo Proj4326 = ProjectionInfo.FromEpsgCode(4326);

        /// <summary>
        /// 平面直角座標を経緯度に変換する
        /// </summary>
        /// <param name="x">平面直角座標 x</param>
        /// <param name="y">平面直角座標 y</param>
        /// <param name="z">平面直角座標 z</param>
        /// <param name="longitude">経度</param>
        /// <param name="latitude">緯度</param>
        /// <param name="height">高さ</param>
        /// <param name="seriesNumber">平面直角座標系での系番号</param>
        /// <returns>成否</returns>
        public static bool TryParseXYToLatLng(double x, double y, double z, out double longitude, out double latitude, out double height, int seriesNumber)
        {
            try
            {
                // 経緯度→平面直角座標
                double[] xyArray = [x, y];
                double[] zArray = [z];

                // 設定ファイルの系番号から対応する日本測地系2011（JGD2011）におけるESPGに変換。
                var proj = ProjectionInfo.FromEpsgCode(ConvertSeriesNumberToEpsgCode(seriesNumber));
                Reproject.ReprojectPoints(xyArray, zArray, proj, Proj4326, 0, 1);

                // 変換結果を設定
                longitude = xyArray[0];
                latitude = xyArray[1];
                height = zArray[0];
                return true;
            }
            catch (Exception ex)
            {
                ILog log = LogManager.GetLogger("SimulationCommonLibrary");
                log.Error("平面直角座標を経緯度に変換に失敗しました。", ex);
            }

            // 変換失敗時は非数を設定
            longitude = double.NaN;
            latitude = double.NaN;
            height = double.NaN;
            return false;
        }

        /// <summary>
        /// 系番号を日本測地系2011（JGD2011）におけるESPGに変換する
        /// </summary>
        /// <param name="seriesNumber">測地系</param>
        /// <returns>ESPG</returns>
        public static int ConvertSeriesNumberToEpsgCode(int seriesNumber)
        {
            int epsgCode = 0;

            // 日本測地系2011（JGD2011）におけるESPGに変換。
            switch (seriesNumber)
            {
                case 1:
                    epsgCode = 6669;
                    break;
                case 2:
                    epsgCode = 6670;
                    break;
                case 3:
                    epsgCode = 6671;
                    break;
                case 4:
                    epsgCode = 6672;
                    break;
                case 5:
                    epsgCode = 6673;
                    break;
                case 6:
                    epsgCode = 6674;
                    break;
                case 7:
                    epsgCode = 6675;
                    break;
                case 8:
                    epsgCode = 6676;
                    break;
                case 9:
                    epsgCode = 6677;
                    break;
                case 10:
                    epsgCode = 6678;
                    break;
                case 11:
                    epsgCode = 6679;
                    break;
                case 12:
                    epsgCode = 6680;
                    break;
                case 13:
                    epsgCode = 6681;
                    break;
                case 14:
                    epsgCode = 6682;
                    break;
                case 15:
                    epsgCode = 6683;
                    break;
                case 16:
                    epsgCode = 6684;
                    break;
                case 17:
                    epsgCode = 6685;
                    break;
                case 18:
                    epsgCode = 6686;
                    break;
                case 19:
                    epsgCode = 6687;
                    break;
            }

            return epsgCode;
        }
    }
}
