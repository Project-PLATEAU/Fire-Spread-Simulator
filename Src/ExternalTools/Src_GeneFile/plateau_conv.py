# 標準ライブラリ
from sys import float_info
# from copy import deepcopy
# import argparse
import json
# 追加ライブラリ
import numpy
from shapely import Point, Polygon, LineString, distance
# 追加ライブラリ(発注者提供)
from plateaupy.plbldg import Building as plBuilding

# 追加ライブラリ(LOD2建物対応業務)
from plateau_conv_lod2 import bldg_height_lod2, grnd_height_lod2, smfrHeimenLst_lod2
from logger import Logger, Trace

# 前業務で作成したクラス
from smfr_io import *
from plateau_load import load_plateau_bldg
# 前業務で追加修正したクラス（以前納品したcrdtrns.py に 複数ポイント一括変換の関数などを追加）
from crdtrns import CrdTrnsFromEPSG, EPSG, PL_NO
""" launch.json で 環境変数PROJ_LIBの設定が必要
[launch.json] 13行目
    "env": { "PROJ_LIB" : "./env_r5nilim/Library/share/proj", }, # proj.db ファイルの収録パス名を指定
"""

def smfrhmn_to_poly(hmn:smfrheimen) -> Polygon:
    return Polygon(shell=[(v.x, v.y) for v in hmn.smfrhmnvtxs])

def bldg_height(plbld:plBuilding) -> float:
    """【総高さ算定】
    plateaupy.plbldg.Building.lod1Solid のＺ座標の最小と最大から総高さを求める

    Parameters
    ----------
    plbld : plBuilding
        plateaupy.plbldg.Buildingクラス

    Returns
    -------
    float
        総高さ(m)=max(Z) - min(Z)
        ※lod1Solidが[]の場合は-9999.9を返す
    """
    if len(plbld.lod1Solid)==0:
        return -9999.9
    zlst = [z for lrng in plbld.lod1Solid for (x,y,z) in lrng]
    min_z = min(zlst)
    max_z = max(zlst)
    return max_z - min_z

def grnd_height(plbld:plBuilding, bldHght:float) -> float:
    """【地盤高取得】
    plateaupy.plbldg.Building.lod0FootPrintのＺ座標
    または
    plateaupy.plbldg.Building.lod0RoofEdgeのＺ座標－総高さ

    Parameters
    ----------
    plbld : plBuilding
        plateaupy.plbldg.Buildingクラス

    bldHght : float
        総高さ

    Returns
    -------
    float
        地盤高(m), (lod0FootPrint.Z) or (lod0RoofEdge.Z - bldHght)
        ※lod0FootPrint,lod0RoofEdgeが[]の場合は-9999.9を返す
    """
    if len(plbld.lod0FootPrint) > 0:
        return min([z for lrng in plbld.lod0FootPrint for (x,y,z) in lrng])

    elif len(plbld.lod0RoofEdge) > 0:
        # ▼▼▼▼▼ ここからが修正箇所です ▼▼▼▼▼

        # 元のコードはコメントとして残します
        # return max([z for lrng in plbld.lod0RoofEdge for (x,y,z) in lrng]) - bldHght

        # ↓↓↓ デバッグプリントを追加した新しいコード ↓↓↓
        max_z_roof = max([z for lrng in plbld.lod0RoofEdge for (x,y,z) in lrng])
        grnd_val = max_z_roof - bldHght

        print(f"--- Ground Level Calculation for ID: {plbld.id} ---")
        print(f"  - Method: Using RoofEdge subtraction")
        print(f"  - Max Z of RoofEdge: {max_z_roof}")
        print(f"  - Relative Building Height (bldHght): {bldHght}")
        print(f"  - Calculated Ground Level (Result = Roof Z - bldHght): {grnd_val}")
        print(f"-------------------------------------------------------")

        return grnd_val
        # ▲▲▲▲▲ ここまでが修正箇所です ▲▲▲▲▲
    else:
        return -9999.9

def num_floor(bldgHght:float, fstflrHght:float, stdFlrHght:float) -> int:
    """【階数算定】

    Parameters
    ----------
    bldgHght : float
        総高さ
    fstflrHght : float
        １階床高
    stdFlrHght : float
        標準階高(cfgファイルより取得)

    Returns
    -------
    int
        建物階数, １階未満の場合は１階に切り上げる
    """
    # 建物階数
    n_flr = int((bldgHght - fstflrHght) // stdFlrHght)
    n_flr = n_flr if n_flr > 0 else 1
    return n_flr

def coord_trns(plbld:plBuilding, epsg:EPSG) -> plBuilding:
    """座標変換(緯度経度⇒公共座標)

    Parameters
    ----------
    plbld : plBuilding
        入力plateau.Buildingクラス(緯度経度座標,EPSG:6668=JGD2011)
    epsg : EPSG
        変換先EPSG(第１(EPSG:6669)～第19(EPSG:6687)を指定)

    Returns
    -------
    plBuilding
        出力plateau.Buildingクラス(公共座標,EPSG:6669～6687)
    """
    from copy import deepcopy
    # 作業用Buildingを生成する
    wrkb = deepcopy(plbld)
    # 座標変換クラスの生成
    ct = CrdTrnsFromEPSG(EPSG.JGD2011_BL, epsg)

    # lod0RoofEdge, lod0FootPrint, lod1Solid の座標変換
    for mmbr in ['lod0RoofEdge', 'lod0FootPrint', 'lod1Solid']:
        lrnglst = getattr(plbld, mmbr)
        if len(lrnglst) > 0:
            wrklst = []
            for lrng in lrnglst:
                blzlst = [(b,l,z) for (b,l,z) in lrng]
                xyzlst = ct.blzs2xyzs(blzlst)
                wrklst.append(numpy.array([numpy.array(vct) for vct in xyzlst]))

            setattr(wrkb, mmbr, wrklst)

    # lod2ground, lod2roof, lod2window の座標変換
    for mmbr in ['lod2ground', 'lod2roof', 'lod2window']:
        lrngdct = getattr(plbld, mmbr)
        if len(lrngdct) > 0:
            wrkdct = {}
            for (id, lrnglst) in lrngdct.items():
                wrklst = []
                for lrng in lrnglst:
                    blzlst = [(b,l,z) for (b,l,z) in lrng]
                    xyzlst = ct.blzs2xyzs(blzlst)
                    wrklst.append(numpy.array([numpy.array(vct) for vct in xyzlst]))

                wrkdct[id] = wrklst

            setattr(wrkb, mmbr, wrkdct)

    # lod2wall の座標変換
    lrngdct = plbld.lod2wall
    if len(lrngdct) > 0:
        wrkdct = {}
        for (id, lrnglst) in lrngdct.items():
            # 座標変換
            wrklst = []
            # lrnglst[0]:座標配列
            # lrnglst[1]:interiorループ開始インデックス
            for lrng in lrnglst[0]:
                blzlst = [(b,l,z) for (b,l,z) in lrng]
                xyzlst = ct.blzs2xyzs(blzlst)
                wrklst.append(numpy.array([numpy.array(vct) for vct in xyzlst]))

            wrkdct[id] = (wrklst, lrnglst[1])

        wrkb.lod2wall = wrkdct

    return wrkb

def smfrHeimenLst(xybld:plBuilding, bldgHght:float, grndHght:float, fstflrHght:float, hasRoof:bool, nflr:int) -> list:
    """【平面形状種算定】
    plateau.Building(公共座標)から、平面形状種リストを生成する

    Parameters
    ----------
    xybld : plBuilding
        plateau.Buildingクラス(公共座標変換済み)
    bldgHght : float
        建物総高さ(m)
    grndHght : float
        地盤高さ(m)
    fstflrHght : float
        １階床高さ(m)
    hasRoof : bool
        True=屋上あり, False=屋上なし
    nflr : int
        建物階数

    Returns
    -------
    list
        [smfrheimen_0, smfrheimen_1, .. , smfrheimen_n-1]
    """
    lsthmn = []
    # plateau の gml:Polygon は、外周(exterior)が反時計回り、内周(interior)が時計回りなので、頂点の並び順はそのままとする

    # lod0FootPrint あり
    if len(xybld.lod0FootPrint) > 0:
        for lrng in xybld.lod0FootPrint:
            xylst = lrng[:,:2]
            # 下座標Ｚは、lod0FootPrint.Z
            lwzlst = lrng[:,2]
            # 上座標Ｚは、下座標＋総高さ
            upzlst = lrng[:,2] + bldgHght
            # 平面形状種頂点座標リスト生成
            hmnvtxlst = [smfrheimenvtx(x,y,lz,uz) for ((x,y),lz,uz) in zip(xylst,lwzlst,upzlst)]
            # 開始頂点の開始フラグをセットする
            hmnvtxlst[0].flg_loopstart = 1
            # 平面形状種クラス生成
            wrkhmn = smfrheimen()
            wrkhmn.n_vtx = len(hmnvtxlst)
            wrkhmn.lwr_hght = fstflrHght
            wrkhmn.upr_hght = max(upzlst)
            wrkhmn.rf_hght = wrkhmn.upr_hght if hasRoof else 0.0 # 平面形状種：屋根高さの設定
            wrkhmn.n_flr = nflr
            wrkhmn.zaishitu = 1
            wrkhmn.smfrhmnvtxs = hmnvtxlst
            lsthmn.append(wrkhmn)

    # lod0RoofEdge あり
    elif len(xybld.lod0RoofEdge) > 0:
        for lrng in xybld.lod0RoofEdge:
            xylst = lrng[:,:2]
            # 下座標Ｚは、地盤高さ
            lwzlst = numpy.array([grndHght] * len(lrng))
            # 上座標Ｚは、lod0RoofEdge.Z
            upzlst = lrng[:,2]
            # 平面形状種頂点座標リスト生成
            hmnvtxlst = [smfrheimenvtx(x,y,lz,uz) for ((x, y),lz, uz) in zip(xylst,lwzlst,upzlst)]
            # 開始頂点の開始フラグをセットする
            hmnvtxlst[0].flg_loopstart = 1
            # 平面形状種クラス生成
            wrkhmn = smfrheimen()
            wrkhmn.n_vtx = len(hmnvtxlst)
            wrkhmn.lwr_hght = fstflrHght
            wrkhmn.upr_hght = max(upzlst)
            wrkhmn.rf_hght = wrkhmn.upr_hght if hasRoof else 0.0 # 平面形状種：屋根高さの設定
            wrkhmn.n_flr = nflr
            wrkhmn.zaishitu = 1
            wrkhmn.smfrhmnvtxs = hmnvtxlst
            lsthmn.append(wrkhmn)

    else:
        pass

    return lsthmn

def smfrFuzokuLst(xybld:plBuilding) -> list:
    """【付属面算定】
    plateau.Building(公共座標)から、付属面リストを生成する

    Parameters
    ----------
    xybld : plBuilding
        plateau.Buildingクラス(公共座標変換済み)

    Returns
    -------
    list
        [smfrfuzoku_0, smfrfuzoku_1, .. , smfrfuzoku_n-1]
    """
    fzklst = []
    # lod2roof を付属面に変換する
    if len(xybld.lod2roof) > 0:
        for (id, lrnglst) in xybld.lod2roof.items():
            for lrng in lrnglst:
                # 付属面頂点リスト生成
                fzkvtzlst = [smfrfuzokuvtx(x,y,z) for (x,y,z) in lrng]
                # 付属面クラス生成
                fzk = smfrfuzoku()
                fzk.n_vtx = len(fzkvtzlst)
                fzk.thick = 0.0
                fzk.zaishitu = 0
                fzk.smfrfzkvtxs = fzkvtzlst
                fzklst.append(fzk)

    # lod2wall を付属面に変換する(壁面(exterior)のみ、窓面(interior)は変換しない)
    if len(xybld.lod2wall) > 0:
        for (id, lrnglst) in xybld.lod2wall.items():
            # lod2wallのexteriorのみを付属面に登録する
            # interior開始ノードの一つ前のインデックス, interiorの開始フラグがない場合は終端ノード
            stpindx = lrnglst[1][0] if len(lrnglst[1]) > 0 else len(lrnglst[0])
            # interior 開始ノードより前のノードで、付属面頂点リスト生成
            fzkvtzlst = [smfrfuzokuvtx(x,y,z) for (x,y,z) in lrnglst[0][0][:stpindx,:]]
            # 付属面クラス生成
            fzk = smfrfuzoku()
            fzk.n_vtx = len(fzkvtzlst)
            fzk.thick = 0.0
            fzk.zaishitu = 0
            fzk.smfrfzkvtxs = fzkvtzlst
            fzklst.append(fzk)

    return fzklst

def remove_duplex_terminal_node(vtxlst:list) -> list:
    """座標リストについて、開始点と重なる終端点を除いたリストを生成する

    Parameters
    ----------
    vtxlst : list
        [numpy.array(xyz_0), numpy.array(xyz_1), .. , numpy.array(xyz_n-1)]

    Returns
    -------
    list
        終端点が開始点と重なる場合は、終端点をのぞいたリストを返す
        [numpy.array(xyz_0), numpy.array(xyz_1), .. , numpy.array(xyz_n-2)]
    """
    # 開始と終端が重なる場合は、終端-1まで出力する
    if numpy.array_equal(vtxlst[0], vtxlst[-1]):
        trmnd = len(vtxlst) - 1
    else:
        trmnd = len(vtxlst)

    return vtxlst[:trmnd]

def centroid_3d(xyzlst:list) -> numpy.array:
    """重心座標を求める

    Parameters
    ----------
    xyzlst : list
        入力座標リスト

    Returns
    -------
    list

    """
    # 開始点と重なる終端点を除く
    rmvlst = remove_duplex_terminal_node(xyzlst)
    # 算術平均を求める
    return numpy.average(rmvlst, axis=0)

def dist_pnt_heimen(cntr:numpy.array, sfhmn:smfrheimen) -> float:
    """３次元窓面重心座標と平面形状種の距離を求める

    Parameters
    ----------
    cntr : numpy.array
        ３次元窓面重心座標
    sfhmn : smfrheimen
        市街地火災シミュレーション用データ：平面形状種クラス

    Returns
    -------
    float
        平面形状種ポリゴンと窓面重心ポイントの距離,
        ※窓面重心のＺ座標が平面形状種のＺ範囲外の場合はfloat.maxを返す
    """
    lzlst = [sfhmnvt.lz for sfhmnvt in sfhmn.smfrhmnvtxs]
    min_z = min(lzlst)
    uzlst = [sfhmnvt.uz for sfhmnvt in sfhmn.smfrhmnvtxs]
    # max_z = min(uzlst)
    max_z = max(uzlst)
    # 評価点Ｚ座標がＺ座標最小より小さいか、Ｚ座標最大より大きい場合は、距離を最大値とする
    if cntr[2] < min_z or cntr[2] > max_z:
        from sys import float_info
        return float_info.max
    # 平面形状種と評価点の距離を求める
    else:
        from shapely import Point, distance
        wndpnt = Point(cntr[:2])
        # hmnply = Polygon([numpy.array([hmnvtx.x,hmnvtx.y]) for hmnvtx in sfhmn.smfrhmnvtxs])
        from plateau_conv_lod2 import smfrhmn_to_mltpoly
        hmnply = smfrhmn_to_mltpoly(sfhmn)
        return distance(wndpnt, hmnply)

def search_heimen(cntr:numpy.array, smfrhmnlst:list) -> int:
    """３次元窓面重心から、最も近い平面形状種のインデックス番号を返す

    Parameters
    ----------
    cntr : numpy.array
        ３次元窓面重心座標
    smfrhmnlst : list
        市街地火災シミュレーション用データ：平面形状種リスト

    Returns
    -------
    int
        窓面重心に最も近い平面形状種リストインデックス番号[0～n-1], n=リスト長さ
    """
    # 窓面重心と平面形状種の距離を求める
    dstlst = [(i, dist_pnt_heimen(cntr, sfhmn)) for i, sfhmn in enumerate(smfrhmnlst)]
    # 最短距離とそのインデックス番号を求める
    mintpl = sorted(dstlst, key=lambda tpl: tpl[1])[0]
    # 最短距離のインデックスを返す
    return mintpl[0]

def search_hekimen(cntr:numpy.array, smfrhmn:smfrheimen) -> int:
    """窓面重心から、最も近い壁面インデックス番号を返す

    Parameters
    ----------
    cntr : numpy.array
        窓面重心座標※プログラム内で２次元に変換
    smfrhmn : smfrheimen
        市街地火災シミュレーション用データ：平面形状種クラス

    Returns
    -------
    int
        窓面重心に最も近い壁面リストインデックス番号[0～n-1], n=頂点数
    """
    from shapely import Point, LineString, distance
    # 窓面重心Point
    wndpnt = Point(cntr[:2])
    # 平面形状種の壁面ごとに線分LineStringを生成する
    vtxlst = [numpy.array([shvtx.x,shvtx.y]) for shvtx in smfrhmn.smfrhmnvtxs]
    rmvlst = remove_duplex_terminal_node(vtxlst)
    edglst = [LineString([v, vtxlst[i+1 if i+1 < len(vtxlst) else 0]]) for i, v in enumerate(rmvlst)]
    # 重心と壁面の距離を求める
    dstlst = [(i, distance(wndpnt, edg)) for (i, edg) in enumerate(edglst)]
    # 最短距離とそのインデックス番号を求める
    mintpl = sorted(dstlst, key=lambda tpl: tpl[1])[0]

    return mintpl[0]

def hrz_kaikou(wnd:list) -> list:
    print(wnd)
    # 窓面重心座標を求める
    wnd_cntr = centroid_3d(wnd)
    # 重心からの相対座標を求める
    rlwnd = [w - wnd_cntr for w in wnd]
    # 窓面座標左下座標を求める
    lftbtm_dr = numpy.array([-1.0, -1.0])
    prjlst = [numpy.dot(rw[:2], lftbtm_dr) for rw in rlwnd]
    prj_max = max(prjlst)
    lftbtm_idx = [i for (i, prj) in enumerate(prjlst) if prj == prj_max][0]
    (x1, y1, z1) = wnd[lftbtm_idx]
    # 窓面座標左下ノードの次ノードを右下座標とする
    rgtbtm_idx = lftbtm_idx + 1
    if rgtbtm_idx >= len(wnd):
        rgtbtm_idx = rgtbtm_idx % (len(wnd) - 1)
    (x2, y2) = wnd[rgtbtm_idx]
    # 窓面座標右下ノードの次ノードを右上座標とする
    rgttop_idx = rgtbtm_idx + 1
    if rgttop_idx >= len(wnd):
        rgttop_idx = rgttop_idx % (len(wnd) - 1)
    # Ｚ２=DIST(右上座標,右下座標)
    z2 = numpy.linalg.norm(wnd[rgttop_idx][:2] - wnd[rgtbtm_idx][:2])

    return [x1,y1,z1,x2,y2,z2]

def vrt_kaikou(wnd:list, hmn:smfrheimen, blng_hkm:int) -> list:
    # 所属壁面番号から壁面座標を取得する
    edgnds = [blng_hkm, (blng_hkm + 1)%len(hmn.smfrhmnvtxs)]
    edglst = [numpy.array([hmn.smfrhmnvtxs[i].x,hmn.smfrhmnvtxs[i].y]) for i in edgnds]
    # 壁面方向ベクトルを生成する
    edg = edglst[1] - edglst[0]
    edg_ln = numpy.linalg.norm(edg)
    if edg_ln == 0.0:
        return None
    edg_dr = edg / edg_ln
    # 窓ベクトルの壁面投影距離を求めて、最小と最大を取得する
    wdlst = [numpy.dot((w[:2] - edglst[0]),edg_dr) for w in wnd]
    (wd_min, wd_max) = (min(wdlst), max(wdlst))
    if wd_min < 0.0:
        wd_min = 0.0
    elif wd_min > edg_ln:
        wd_min = edg_ln
    if wd_max < 0.0:
        wd_max = 0.0
    elif wd_max > edg_ln:
        wd_max = edg_ln
    if wd_min >= wd_max:
        return None
    # 最小距離から左下座標を求める
    (wx1, wy1) = wd_min * edg_dr + edglst[0]
    # 最大距離から右下座標を求める
    (wx2, wy2) = wd_max * edg_dr + edglst[0]
    # 窓面Ｚの最小と最大を取得する
    wzlst = [w[2] for w in wnd]
    (wz1, wz2) = (min(wzlst), max(wzlst))
    if wz1 < hmn.lwr_hght:
        wz1 = hmn.lwr_hght
    elif wz1 > hmn.upr_hght:
        wz1 = hmn.upr_hght
    if wz2 < hmn.lwr_hght:
        wz2 = hmn.lwr_hght
    elif wz2 > hmn.upr_hght:
        wz2 = hmn.upr_hght
    if wd_min >= wd_max:
        return None

    return [wx1,wy1,wz1,wx2,wy2,wz2]

def smfrKaikouLst(xybld:plBuilding, smfrhmnlst:list) -> list:
    """【開口部算定】
    plateau.Building(公共座標)から、合計開口部リストを生成する

    Parameters
    ----------
    xybld : plBuilding
        plateau.Buildingクラス(公共座標変換済み)
    smfrhmnlst : list
        平面形状種リスト

    Returns
    -------
    list
        [smfrkaikou_0, smfrkaikou_1, .. , smfrkaikou_n-1]
    """
    from plateau_conv_lod2 import search_heimen_hrz

    kakolst = []
    if len(xybld.lod2window) > 0 and len(xybld.smwindow) > 0:
        # print(xybld.id)
        for (id, lrnglst) in xybld.lod2window.items():
            # lod2window はシングルポリゴン,四角形を前提とする
            smwnd = lrnglst[0]
            # 窓面重心点を算定する
            wndcntr = centroid_3d(smwnd)
            # idをキーに材質IDと開口部方向を取得する
            swndct = xybld.smwindow.get(id)
            kako = smfrkaikou()
            kako.zaishitu = int(swndct['sim:materialReferenceType'])
            kako.vrt_hrz = float(swndct['sim:directionOfWindow'])

            # 方向がゼロの場合 -- 水平面（屋上）
            if kako.vrt_hrz == 0.0:
                # 窓面重心と平面形状種リストから、窓面の所属平面番号を取得する
                # kako.blng_hmn = search_heimen(wndcntr, smfrhmnlst)
                kako.blng_hmn = search_heimen_hrz(wndcntr, smfrhmnlst)
                sfhmn = smfrhmnlst[kako.blng_hmn]
                # 所属壁面番号は-1で固定
                kako.blng_hkm = -1
                # 開口部座標を求める
                xyzlst = hrz_kaikou(smwnd, sfhmn)
                if xyzlst is None:
                    continue
                (kako.x1,kako.y1,kako.z1,kako.x2,kako.y2,kako.z2) = xyzlst

            # 方向が１の場合 -- 垂直面（壁面）
            elif kako.vrt_hrz == 1.0:
                # 窓面重心と平面形状種リストから、窓面の所属平面番号を取得する
                kako.blng_hmn = search_heimen(wndcntr, smfrhmnlst)
                sfhmn = smfrhmnlst[kako.blng_hmn]
                # 窓面重心と平面形状種から、窓面の所属「壁面」番号を取得する
                kako.blng_hkm = search_hekimen(wndcntr, sfhmn)
                # 開口部座標を求める
                xyzlst = vrt_kaikou(smwnd, sfhmn, kako.blng_hkm)
                # 開口部座標が求められなかった場合は出力しない
                if xyzlst is None:
                    continue

                (kako.x1,kako.y1,kako.z1,kako.x2,kako.y2,kako.z2) = xyzlst

            # 方向が0,1以外の場合は処理しない
            else:
                continue

            # 生成した合計開口部を登録する
            kakolst.append(kako)

    return kakolst

def to_smfrBldg(plbld:plBuilding, epsg:EPSG, stdbshght:float, stdflrhght:float, fzkout:bool,
                smplfy_angl:float=179.9, smplfy_vdst:float=5.0e-3, mltplygn_out:bool=True) -> smfrbldg:


    # ▼▼▼ このデバッグコードを関数の先頭に追加 ▼▼▼
    print(f"--- Raw Data Check for ID: {plbld.id} ---")
    print(f"  - Raw plbld.measuredHeight value: {plbld.measuredHeight}")
    print(f"---------------------------------------------")
    # ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    # ...（以降、元の関数のコードが続く）



    """【１棟処理】
    plateau.Building を 火災シミュレーションの建物データへ変換する

    Parameters
    ----------
    plbld : plBuilding
        plateau.Buildingクラス(緯度経度座標)
    epsg : EPSG
        変換先座標系のEPSG番号
    stdbshght : float
        標準基礎高さ(m)
    stdflrhght : float
        標準階高さ(m)
    fzkout : bool
        True=付属面出力あり, False=付属面出力なし
    smplfy_angl : float
        断面ポリゴン単純化処理：角度の閾値, 閾値以上の場合は直線とみなして取り除く, defaults by 179.9
    smplfy_vdst : float
        断面ポリゴン単純化処理：距離の閾値, 閾値以下の場合は直線とみなして取り除く, defaults by 5.0e-3,
    mltplygn_out : bool
        スライス断面複数ポリゴン出力有無, True=あり, False=なし, defaults by True

    Returns
    -------
    smfrbldg
        火災シミュレーション建物データクラス
    """
    _lod2flg = True
    print(Trace.execution_location(), plbld.attr.get('建物ID'))

    # 総高さ（ここでは、地表からの相対高さ）取得
    # plateau.measuredHeight = 計測高さ,
    # "建築物の属性「計測高さ」は、「計測により得られた建築物の地上の最低点から最高点までの高さ」である。"
    # https://www.mlit.go.jp/plateau/file/libraries/doc/plateau_doc_0002_ver01.pdf, 79コマ目
    # lod2roofなどがあり、LOD2処理フラグがセットされている場合
    # 計測高さが設定されている場合
    if plbld.measuredHeight is not None:
        # 計測高さを取得する
        msrdHght = float(plbld.measuredHeight)
        # LOD2データの有無を判定する
        if len(plbld.lod2roof)>0 and len(plbld.lod2wall)>0 and len(plbld.lod2ground)>0 and _lod2flg:
            # LOD2総高さを求める
            lod2Hght = bldg_height_lod2(plbld)
            # 計測高さがLOD2総高さ以下の場合、計測高さを採用する
            bldgHght = msrdHght if msrdHght <= lod2Hght else lod2Hght
        else:
            # 建物総高さを計測高さとする
            bldgHght = msrdHght
            # # LOD2データがなければ、LOD1高さを求める
            # lod1Hght = bldg_height(plbld)
            # if lod1Hght > 0.0:
            #     # 計測高さがLOD1総高さ以下の場合、計測高さを採用する
            #     bldgHght = msrdHght if msrdHght <= lod1Hght else lod1Hght
            # else:
            #     bldgHght = msrdHght
    else:
        # LOD1から建物総高さを求める
        bldgHght = bldg_height(plbld)

    # lod2groundがあり、LOD2処理フラグがセットされている場合
    if len(plbld.lod2ground) > 0 and _lod2flg:
        # 地盤高取得(LOD2参照あり)
        grndHght = grnd_height_lod2(plbld)
    else:
        # 地盤高取得(従来処理呼び出し)
        grndHght = grnd_height(plbld, bldgHght)

# ...
    if plbld.measuredHeight is not None:
        msrdHght = float(plbld.measuredHeight)
        if len(plbld.lod2roof)>0 and len(plbld.lod2wall)>0 and len(plbld.lod2ground)>0 and _lod2flg:
            lod2Hght = bldg_height_lod2(plbld)

            # ▼▼▼ このデバッグコードを追加 ▼▼▼
            print(f"--- LOD2 Height Debug for ID: {plbld.id} ---")
            print(f"  - GML measuredHeight: {msrdHght}")
            print(f"  - Calculated LOD2 Height: {lod2Hght}")
            print(f"-------------------------------------------------")
            # ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            # 計測高さがLOD2総高さ以下の場合、計測高さを採用する
            bldgHght = msrdHght if msrdHght <= lod2Hght else lod2Hght
    # ...



    # 総高さ（相対高さ）に、地盤高さを加える
    bldgHght = bldgHght + grndHght

    # 標準基礎高取得(cfgファイルより取得)
    bsHght = stdbshght
    # １階床高さ
    fstflrHght = grndHght + bsHght
    # 階数取得
    if plbld.storeysAboveGround is not None:
        nFlr = int(plbld.storeysAboveGround)
    else:
        nFlr = num_floor(bldgHght, fstflrHght, stdflrhght)

    # 総高さ（相対高さ）に、地盤高さを加える
    bldgHght = bldgHght + grndHght



    # 緯度経度BL⇒公共XY座標変換
    xybld = coord_trns(plbld, epsg)

    # lod2roofなどがあり、LOD2処理フラグがセットされている場合
    if len(xybld.lod2roof)>0 and len(xybld.lod2wall)>0 and len(xybld.lod2ground)>0 and _lod2flg:

        # ▼▼▼ デバッグ追加：LOD2（詳細）ルートへ進んだことを記録 ▼▼▼
        print(f"[ROUTE-CHECK] ID: {plbld.id}  ===>  【LOD2】 (詳細処理を実行します)")
        # ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲


        # 平面形状算定(LOD2参照あり)
        hmnlst = smfrHeimenLst_lod2(xybld, bldgHght, grndHght, fstflrHght, fzkout, nFlr, tol_angl=smplfy_angl, tol_vdst=smplfy_vdst)
    else:

        # ▼▼▼ デバッグ追加：LOD1（簡易）ルートへ進んだことを記録 ▼▼▼
        print(f"[ROUTE-CHECK] ID: {plbld.id}  ===>  [LOD1] (簡易処理でスキップします)")
        # ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲


        # 平面形状算定
        hmnlst = smfrHeimenLst(xybld, bldgHght, grndHght, fstflrHght, fzkout, nFlr)

    # 付属面出力(cfgファイルで出力有無を指定)
    fzklst = list()
    if fzkout:
        fzklst = smfrFuzokuLst(xybld)

    # 開口部算定
    kakolst = smfrKaikouLst(xybld, hmnlst)

    smfrbld = smfrbldg()
    smfrbld.bldid =  plbld.id
    smfrbld.flrhght = float(plbld.smcityfiresimulation['sim:floorHeight'])
    smfrbld.kouzou = int(plbld.smcityfiresimulation['sim:fireproofStructureCityFireSimulationType'])
    smfrbld.shubetu = int(plbld.smcityfiresimulation['sim:buildingStructureCityFireSimulationType'])
    smfrbld.youto = int(plbld.smcityfiresimulation['sim:usageCityFireSimulation'])
    # GML に bldg:yearOfConstructionがない場合は、ゼロとする
    smfrbld.cnstrct_yr = int(plbld.yearOfConstruction) if plbld.yearOfConstruction is not None else 0
    smfrbld.bouka = int(plbld.smcityfiresimulation['sim:districtsAndZonesCityFireSimulationType'])
    smfrbld.jishin = int(plbld.smcityfiresimulation['sim:earthquakeType'])
    smfrbld.smfrhmns = hmnlst
    smfrbld.n_heimen = len(smfrbld.smfrhmns)
    smfrbld.smfrfzks = fzklst
    smfrbld.n_fuzoku = len(smfrbld.smfrfzks)
    smfrbld.smfrkakos = kakolst
    smfrbld.n_kaikou = len(smfrbld.smfrkakos)

    return smfrbld

def plateau_to_simfire(igmlpth:str, osmfrfn:str, oepsg:int, std_base_height:float, std_floor_height:float, fuzoku_out:bool,
                       smplfy_angl:float=179.9, smplfy_dst:float=5.0e-3, mltplygn_out:bool=True, **kwargs) -> None:
    """【simfireデータ生成】
    plateau.GMLファイルから、火災シミュレーション用データファイルを作成する

    Parameters
    ----------
    igmlpth : str
        plateau.GMLファイル格納パス名
    osmfrfn : str
        出力先火災シミュレーション用データファイル名
    oepsg : int
        出力先公共座標系EPSG番号(EPSG:6669～6687)
    stdbshght : float
        標準基礎高さ(m)
    stdflrhght : float
        標準階高さ(m)
    fzkout : bool
        True=付属面出力あり, False=付属面出力なし
    smplfy_angl:float=179.9,

    smplfy_dst:float=5.0e-3,

    mltplygn_out:bool=True,

    """
    # plateau.GMLファイルを読み込む
    dctplbld = load_plateau_bldg(igmlpth)
    # GML建物データがない場合は終了する
    if len(dctplbld) == 0:
        return
    # plbldg.Buildingリストを生成する
    plbldglst = [plbldg for plb in dctplbld.values() for plbldg in plb.buildings]

    # plbldg.Buildingを火災シミュレーション用建物データへ変換する
    smfrbldlst = [to_smfrBldg(plbldg, oepsg, std_base_height, std_floor_height, fuzoku_out) for plbldg in plbldglst]

    # 市街地火災シミュレーション用データファイル出力
    sfinpt = smfrinput()
    sfinpt.hdr_inf = smfrheaderinfo()
    plno = PL_NO.fromEPSG(oepsg)
    sfinpt.hdr_inf.projection = f'XY{plno:02n}'
    sfinpt.smfrbldgs = smfrbldlst
    sfinpt.n_bldgs = len(smfrbldlst)
    with open(osmfrfn, 'wt', encoding='cp932') as fout:
        fout.write(sfinpt.to_smfrstr())
    pass

def main() -> None:
    """引数は plateau_conv.cfg で設定している
    """
    # import argparse
    # argprsr = argparse.ArgumentParser(description='plateau.GML -> simfire data converter.')
    # argprsr.add_argument('igmlpth', nargs='+', type=str, help='入力GML収録パス')
    # argprsr.add_argument('osmfrfn', type=str, help='出力火災シミュレーション用データファイル')
    # argprsr.add_argument('oepsg', type=int, help='出力座標EPSG番号(6669～6687)')
    # argprsr.add_argument('--std-base-height', type=float, help='標準基礎高さ')
    # argprsr.add_argument('--std-floor-height', type=float, help='標準階高さ')
    # argprsr.add_argument('--fuzoku-out', action='store_true', help='付属面の出力有無')
    # args = argprsr.parse_args()
    # kwarg = vars(args)
    # with open('plateau_conv.cfg','wt',encoding='cp932') as fcfg:
    #     json.dump(dct, fcfg, indent=4, )

    with open('cfg/plateau_conv.cfg','rt',encoding='cp932') as fcfg:
        kwargs = json.load(fcfg)

    # Loggerクラス初期化
    Logger.init(kwargs.get('errlog_fn'))

    plateau_to_simfire(**kwargs)

    pass

if __name__ == '__main__':
    main()
    pass