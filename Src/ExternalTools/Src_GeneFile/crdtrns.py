import os
import sys

# --- Conda環境のGISライブラリのパスを明示的に設定 ---
# このコードブロックを、他のimport文よりも前に記述します。
try:
    # 現在のConda環境のルートパスを取得
    conda_env_path = sys.prefix

    # PROJライブラリのデータパスを設定
    proj_lib_path = os.path.join(conda_env_path, 'Library', 'share', 'proj')
    if os.path.exists(proj_lib_path):
        os.environ['PROJ_LIB'] = proj_lib_path
        print(f"PROJ_LIBを '{proj_lib_path}' に設定しました。")

    # GDALライブラリのデータパスを設定
    gdal_data_path = os.path.join(conda_env_path, 'Library', 'share', 'gdal')
    if os.path.exists(gdal_data_path):
        os.environ['GDAL_DATA'] = gdal_data_path
        print(f"GDAL_DATAを '{gdal_data_path}' に設定しました。")

except Exception as e:
    print(f"GIS関連の環境変数設定中にエラーが発生しました: {e}")
# --- ここまで ---


# これ以降に、元々のimport文やコードが続く
# from shapely import Point, Polygon, ...
# import plateaupy
# ...


from enum import IntEnum
from osgeo import osr
""" osr の利用のため、launch.json で 環境変数PROJ_LIBの設定が必要
[launch.json] 13行目
    "env": { "PROJ_LIB" : "./env_r5nilim/Library/share/proj", }, # proj.db ファイルの収録パス名を指定
"""

class EPSG(IntEnum):

    JGD2011_BL = 6668
    """JGD2011,緯度経度
    """
    JGD2011_PL01 = 6669
    """JGD2011,平面直角座標01系：長崎県
    """
    JGD2011_PL02 = 6670
    """JGD2011,平面直角座標02系：福岡県 佐賀県 熊本県 大分県 宮崎県
    """
    JGD2011_PL03 = 6671
    """JGD2011,平面直角座標03系：山口県 島根県 広島県
    """
    JGD2011_PL04 = 6672
    """JGD2011,平面直角座標04系：香川県 愛媛県 徳島県 高知県
    """
    JGD2011_PL05 = 6673
    """JGD2011,平面直角座標05系：兵庫県 鳥取県 岡山県
    """
    JGD2011_PL06 = 6674
    """JGD2011,平面直角座標06系：京都府 大阪府 福井県 滋賀県 三重県 奈良県 和歌山県
    """
    JGD2011_PL07 = 6675
    """JGD2011,平面直角座標07系：石川県 富山県 岐阜県 愛知県
    """
    JGD2011_PL08 = 6676
    """JGD2011,平面直角座標08系：新潟県 長野県 山梨県 静岡県
    """
    JGD2011_PL09 = 6677
    """JGD2011,平面直角座標09系：東京都の一部 市町村、２３区、大島町、利島村、新島村、神津島村、三宅村、御蔵島村、八丈町、青ケ島村
    福島県 栃木県 茨城県 埼玉県 千葉県 群馬県 神奈川県
    """
    JGD2011_PL10 = 6678
    """JGD2011,平面直角座標10系：青森県 秋田県 山形県 岩手県 宮城県
    """
    JGD2011_PL11 = 6679
    """JGD2011,平面直角座標11系：北海道の一部 小樽市、函館市、伊達市、北斗市、豊浦町、壮瞥町、洞爺湖町、
    北海道後志総合振興局の所管区域、北海道渡島総合振興局の所管地域、北海道檜山振興局の所管区域
    """
    JGD2011_PL12 = 6680
    """JGD2011,平面直角座標12系：北海道の一部 北海道（11系及び13系に規定する区域を除く。 ）
    """
    JGD2011_PL13 = 6681
    """JGD2011,平面直角座標13系：北海道の一部 北見市、帯広市、釧路市、網走市、根室市、美幌町、津別町、斜里町、清里町、小清水町、
    訓子府町、置戸町、佐呂間町、大空町、北海道十勝総合振興局の所管区域、北海道釧路総合振興局の所管区域、北海道根室振興局の所管区域
    """
    JGD2011_PL14 = 6682
    """JGD2011,平面直角座標14系：東京都の一部 小笠原村(聟島列島、父島列島、母島列島、硫黄島)
    """
    JGD2011_PL15 = 6683
    """JGD2011,平面直角座標15系：沖縄県の一部 那覇市、石川市、具志川市、宜野湾市、浦添市、名護市、糸満市、沖繩市、国頭村、大宜味村、東村、
    今帰仁村、本部町、恩納村、宜野座村、金武町、伊江村、与那城町、勝連町、読谷村、嘉手納町、北谷町、北中城村、中城村、西原町、豊見城村、
    東風平町、具志頭村、玉城村、知念村、佐敷町、与那原町、大里村、南風原町、仲里村、具志川村、渡嘉敷村、座間味村、粟国村、渡名喜村、伊平屋村、伊是名村
    """
    JGD2011_PL16 = 6684
    """JGD2011,平面直角座標16系：沖縄県の一部 平良市、石垣市、城辺町、下地町、上野村、伊良部町、多良間村、竹富町、与那国町
    """
    JGD2011_PL17 = 6685
    """JGD2011,平面直角座標17系：沖縄県の一部 南大東村、北大東村
    """
    JGD2011_PL18 = 6686
    """JGD2011,平面直角座標18系：東京都の一部 小笠原村(沖ノ鳥島)
    """
    JGD2011_PL19 = 6687
    """JGD2011,平面直角座標19系：東京都の一部 小笠原村 (南鳥島)
    """

class PL_NO:
    __epsg_plno = dict([[epsg, plno] for plno, epsg in enumerate(range(EPSG.JGD2011_PL01, EPSG.JGD2011_PL19+1), 1)])

    @classmethod
    def fromEPSG(cls, epsg:EPSG) -> int:
        return cls.__epsg_plno.get(epsg)

class CrdTrns:
    def __init__(self, srcSrs:osr.SpatialReference, dstSrs:osr.SpatialReference) -> None:
        """初期化

        Parameters
        ----------
        srcSrs : osr.SpatialReference
            変換元座標系
        dstSrs : osr.SpatialReference
            変換先座標系
        """
        # 入力座標系
        self.__srcSrs = srcSrs
        # 出力座標系
        self.__dstSrs = dstSrs
        # 座標変換の準備
        self.__crdtrns = osr.CreateCoordinateTransformation(self.__srcSrs, self.__dstSrs)

    def trnspnt(self, crd1: float, crd2:float) -> list:
        """座標変換

        Parameters
        ----------
        crd1 : float
            変換元座標１(地理⇒平面)B 緯度(度), (平面⇒地理)x(m)[東西方向]
        crd2 : float
            変換元座標２(地理⇒平面)L 経度(度), (平面⇒地理)y(m)[南北方向]

        Returns
        -------
        list
            変換先座標(地理⇒平面)[x東西,y南北], (平面⇒地理)[B,L]
        """
        # 地理座標⇒平面投影座標
        if (self.__srcSrs.IsGeographic()==1 and self.__dstSrs.IsProjected()==1):
            return self.bl2xy(crd1, crd2)

        # 平面投影座標⇒地理座標
        elif (self.__srcSrs.IsProjected()==1 and self.__dstSrs.IsGeographic()==1):
            return self.xy2bl(crd1, crd2)

        # 座標変換
        else:
            tpl = self.__crdtrns.TransformPoint(crd1, crd2)[0:2]
            return tpl

    def bl2xy(self, lat_deg: float, lng_deg:float) -> list:
        """緯度経度⇒平面投影座標変換

        Parameters
        ----------
        lat_deg : float
            緯度(度)
        lng_deg : float
            経度(度)

        Returns
        -------
        list
            [x(m)[東西方向], y(m)[南北方向]]
        """
        # 座標変換
        yx = self.__crdtrns.TransformPoint(lat_deg, lng_deg)
        return [yx[1], yx[0]]

    def blzs2xyzs(self, bllst:list) -> list:
        """緯度経度⇒平面投影座標変換

        Parameters
        ----------
        bllst : list
            [[緯度(度)_0,経度(度)_0,Z_0(m)], [緯度(度)_1,経度(度)_1,Z_1(m)], .. , [緯度(度)_n-1,経度(度)_n-1,Z_n-1(m)]]

        Returns
        -------
        list
            [[x(m)[東西]_0, y(m)[南北]_0, Z_0(m)], [x(m)_1, y(m)_1, Z_1(m)], .. , [x(m)_n-1, y(m)_n-1, ,Z_n-1(m)]]
        """
        # 座標変換
        yxzlst = self.__crdtrns.TransformPoints(bllst)
        return [[x,y,z] for (y,x,z) in yxzlst]

    def xy2bl(self, x_m:float, y_m: float) -> list:
        """_summary_

        Parameters
        ----------
        x_m : float
            東西方向X(m), 平面直角座標Y座標
        y_m : float
            南北方向Y(m), 平面直角座標X座標

        Returns
        -------
        list
            [緯度(度),経度(度)]
        """
        # 座標変換
        bl = self.__crdtrns.TransformPoint(y_m, x_m)
        return [bl[0], bl[1]]

def SrsFromEPSG(epsg: int) -> osr.SpatialReference:
    """EPSGコードを指定してSpatialReferenceを生成する

    Parameters
    ----------
    epsg : int
        EPSGコード

    Returns
    -------
    osr.SpatialReference
        EPSGコードを設定した地理座標参照系クラス
    """
    srs = osr.SpatialReference()
    srs.ImportFromEPSG(epsg)
    return srs

def CrdTrnsFromEPSG(srcEpsg:int, dstEpsg:int) -> CrdTrns:
    """EPSG番号を指定してクラス初期化

    Parameters
    ----------
    srcEpsg : int
        変換元座標系EPSG番号
    dstEpsg : int
        変換先座標系EPSG番号
    """
    # 入力座標系
    srcSrs = SrsFromEPSG(srcEpsg)
    # 出力座標系
    dstSrs = SrsFromEPSG(dstEpsg)
    # 座標変換の準備
    return CrdTrns(srcSrs, dstSrs)

if __name__ == '__main__':
    # bl2xy = CrdTrns(EPSG.JGD2011_BL, EPSG.JGD2011_PL05)
    # print(bl2xy.trnspnt(34.652461,135.139522))
    # xy2bl = CrdTrns(EPSG.JGD2011_PL05, EPSG.JGD2011_BL)
    # print(xy2bl.trnspnt(73897.56818,-149138.9077))
    # crdtrns = CrdTrnsFromEPSG(EPSG.JGD2011_BL, EPSG.JGD2011_PL09)
    # hoge = crdtrns.blzs2xyzs([[35.0, 140], [36.0, 140.0]])
    # __epsg_plno = dict([(epsg,plno) for plno, epsg in enumerate(range(EPSG.JGD2011_PL01, EPSG.JGD2011_PL19+1), 1)])
    pass