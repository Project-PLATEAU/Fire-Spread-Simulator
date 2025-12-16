# 標準ライブラリ
from math import acos, degrees
# 追加導入ライブラリ
import numpy
from shapely import coords
from shapely import LineString, Polygon, MultiPolygon, LinearRing
from shapely import (
    distance, get_parts, is_ccw, reverse, polygonize, equals_exact,
    get_num_interior_rings, simplify, force_2d
)
# 追加導入ライブラリ(３次元オブジェクト断面スライス)
import trimesh
# 追加ライブラリ(発注者提供)
from plateaupy.plbldg import Building as plBuilding

# 前業務で作成したクラス
from smfr_io import *
# 前業務で追加修正したクラス（以前納品したcrdtrns.py に 複数ポイント一括変換の関数などを追加）
from crdtrns import EPSG #, CrdTrnsFromEPSG, PL_NO
""" launch.json で 環境変数PROJ_LIBの設定が必要
[launch.json] 13行目
    "env": { "PROJ_LIB" : "./env_r5nilim/Library/share/proj", }, # proj.db ファイルの収録パス名を指定
"""
from logger import Logger, Trace

def bldg_height_lod2(plbld:plBuilding) -> float:
    """【総高さ算定】lod2追加
    plateaupy.plbldg.Building.lod1Solid のＺ座標の最小と最大から総高さを求める

    Parameters
    ----------
    plbld : plBuilding
        plateaupy.plbldg.Buildingクラス

    Returns
    -------
    float
        総高さ(m)=max(Z) - min(Z)
    """
    # 屋上と地盤面の３次元座標配列を取得する
    arylst = [ary[0][:-1] for dct in [plbld.lod2roof,plbld.lod2ground] for ary in dct.values()]
    # 壁面の３次元座標配列を取得する
    for ary in plbld.lod2wall.values():
        # 外周ポリゴン各頂点の３次元座標配列を取得する
        extnd = ary[1][0] if len(ary[1])>0 else len(ary[0][0])
        arylst.append(ary[0][0][:extnd-1])

    # Ｚ座標を取り出す
    zlst = [z for crds in arylst for (_,_,z) in crds]
    min_z = min(zlst)
    max_z = max(zlst)
    return max_z - min_z

def smfrhmn_to_mltpoly(hmn:smfrheimen) -> MultiPolygon:
    """平面形状をshapely.MultiPolygonへ変換する

    Parameters
    ----------
    hmn : smfrheimen
        平面形状クラス

    Returns
    -------
    MultiPolygon
        平面形状より生成したマルチポリゴン
    """
    # ループ開始フラグ==1となる平面形状頂点インデックスを取得する
    idxlst = [n for (n, hv) in enumerate(hmn.smfrhmnvtxs) if hv.flg_loopstart == 1]
    # 終端インデックスを追加する
    idxlst += [len(hmn.smfrhmnvtxs)]
    # エラー：ループ開始フラグ==1がみつからない
    if len(idxlst) < 2:
        return None
    # ループ開始フラグ区切りで平面形状頂点リストを生成する
    hmnvlst = [hmn.smfrhmnvtxs[idxlst[i] : idxlst[i+1]] for i in range(len(idxlst)-1)]
    # 頂点リストからLinearRingを生成する
    rnglst = [LinearRing([(v.x,v.y) for v in vlst]) for vlst in hmnvlst]
    # CCWのリングを検出する
    ccwidx = [i for i, rng in enumerate(rnglst) if is_ccw(rng)]
    ccwidx += [len(rnglst)]
    # ポリゴンリストを生成し、MultiPolygonを生成する
    plys = [Polygon(shell=rnglst[ccwidx[i]], holes=rnglst[ccwidx[i] + 1 : ccwidx[i+1]]) for i in range(len(ccwidx)-1)]
    rslt = MultiPolygon(plys)

    return rslt

def xybldg_to_trimesh(xybldg:plBuilding, cnv_wndw:bool=False) -> trimesh.Trimesh:
    """plateaupy.Buildingの、lod2roof,lod2wall,lod2windowをTrimeshへ変換する

    Parameters
    ----------
    xybldg : Building
        plateaupy.plbldg.Building
    cnv_wndw : bool, optional
        lod2window変換有無, True:変換する, False:変換しない, by default False

    Returns
    -------
    trimesh.Trimesh
        変換したTrimeshオブジェクト
    """
    rslt = trimesh.Trimesh()

    for dct in [xybldg.lod2roof, xybldg.lod2window, xybldg.lod2ground]:
        if cnv_wndw is False and dct == xybldg.lod2window:
            # lod2windowを処理しない場合は、xybldg.lod2windowの処理をとばす
            continue

        for ary in dct.values():
            # ３次元座標配列を取得する
            __crds = ary[0][:-1]
            # 各頂点の法線ベクトルを求める
            nrmlst = [numpy.cross(__crds[i-1]-__crds[i-2],__crds[i]-__crds[i-1]) for i in range(len(__crds))]
            # 平均法線ベクトルを求める
            avnrm = numpy.average(nrmlst, axis=0)
            # 平均法線ベクトル各成分の絶対値の最大を求める
            midx = numpy.argmax(numpy.abs(avnrm))
            # 最大成分を法線ベクトルする平面に投影するため、最大成分の座標を削除した２次元座標を生成する
            idxlst = [i for i in range(3) if i!=midx]
            crd2d = ary[0][:, idxlst]
            # ポリゴン外周座標を取得する
            extnd = len(ary[0])
            ext = crd2d[:extnd]
            # ２次元投影したポリゴンを生成する
            ply = Polygon(ext)
            # 三角形分解メソッド呼び出し
            (vrtcs,fcs) = trimesh.creation.triangulate_polygon(ply)
            # 元の３次元座標に参照して、trimeshを生成する
            __tmpmsh = trimesh.Trimesh(vertices=ary[0],faces=fcs)
            # 結果保存用Trimeshへ追加する
            rslt = trimesh.util.concatenate(rslt, __tmpmsh)

    for ary in xybldg.lod2wall.values():
        # print(f'len(ary[0][0]):{len(ary[0][0])}')
        # print(f'ary[1]:{ary[1]}')
        # 外周ポリゴン各頂点の法線ベクトルを求めるため、準備する
        extnd = ary[1][0] if len(ary[1])>0 else len(ary[0][0])
        __crds = ary[0][0][:extnd-1]
        # 外周ポリゴン各頂点の法線ベクトルを求める
        nrmlst = [numpy.cross(__crds[i-1]-__crds[i-2],__crds[i]-__crds[i-1]) for i in range(len(__crds))]
        # 法線ベクトルの平均を求める
        avnrm = numpy.average(nrmlst, axis=0)
        # 法線ベクトルの成分のうち、最大を求める
        midx = numpy.argmax(numpy.abs(avnrm))
        # 最大成分を軸として、２次元平面(XY or XZ or YZ平面)に投影する
        idxlst = [i for i in range(3) if i!=midx]
        crd2d = ary[0][0][:, idxlst]
        # ポリゴン外周座標を取得する
        ext = crd2d[:extnd]
        # ポリゴン内周座標を取得する
        inr = list()
        if cnv_wndw == True:
            # lod2windowを処理する場合、内周ポリゴンを追加する
            if len(ary[1]) > 0:
                inr = [crd2d[ary[1][i-1]:ary[1][i]] for i in range(1, len(ary[1]))]
                if ary[1][-1] < len(crd2d):
                    inr.append(crd2d[ary[1][-1]:])
        # ２次元投影したポリゴンを生成する
        ply = Polygon(ext, inr)
        # 三角形分解メソッド呼び出し
        (vrtcs,fcs) = trimesh.creation.triangulate_polygon(ply)
        # 元の３次元座標に戻してtrimeshを生成する
        __tmpmsh = trimesh.Trimesh(vertices=ary[0][0],faces=fcs)
        rslt = trimesh.util.concatenate(rslt, __tmpmsh)

    return rslt

def slice_xy_planes(tmsh:trimesh.Trimesh, zlst:list) -> dict:
    """Trimeshと、引数Zリストのxy平面との断面を求める

    Parameters
    ----------
    tmsh : trimesh.Trimesh
        Trimeshオブジェクト
    zlst : list
        [Z0, Z1, .., Zn]：高さリスト

    Returns
    -------
    list
        [0:[断面PoygonZ0_0], 1:[断面PoygonZ1_0,断面PoygonZ1_1,..], 2:.. ]
    """
    # スライス関数の呼び出しに必要なパラメータを準備する
    z0 = zlst[0]
    orgn = list(tmsh.centroid[:2]) + [z0]
    nrm = [0.0, 0.0, 1.0]
    hghts = [z - z0 for z in zlst]
    # スライスメソッド呼び出し, ※結果はポリゴンではなく、ライン(線分)で返される
    (lines, _, _) = trimesh.intersections.mesh_multiplane(tmsh, orgn, nrm, hghts)

    rslt = list()
    # スライス断面について処理を繰り返す
    for i, sgmntlst in enumerate(lines):
        # 断面線分座標はorgnで正規化されているので、元の３次元座標に戻す
        zary = numpy.array([zlst[i]]*2).reshape(2, 1)
        sgmnts = [LineString(numpy.hstack((sgmnt[:,:2]+orgn[:2],zary))) for sgmnt in sgmntlst]
        # shapely.polygonize()で線分からポリゴンを生成する
        plygnz = polygonize(sgmnts)
        # 生成したポリゴンを結果リストへ追加する
        plys = list(get_parts(plygnz))
        # エラー：エラー:shapely.polygonize()メソッドpolygon生成不能
        if len(plys) == 0:
            # print('エラー:shapely.polygonize()メソッドpolygon生成不能')
            Logger.error(Trace.execution_location(), 'エラー:shapely.polygonize()メソッドpolygon生成不能')

        # ポリゴン内部点を生成する
        pnts = [ply.point_on_surface() for ply in plys]
        pnts = [(pt.x, pt.y, zlst[i]) for pt in pnts]

        # trimesh 内外判定を行う
        #   iorslt = tmsh.contains(pnts)
        # 2025/11/25KKC修正エラーデータへの対処 ▼▼▼
        if len(pnts) > 0:
            iorslt = tmsh.contains(pnts)
        else:
            iorslt = [] # 空なら何もしない

        # 内部点がtrimshに含まれるポリゴンを抽出する
        plys = [ply for (ply, __io) in zip(plys, iorslt) if __io]
        # エラー：スライス断面ポリゴンがtrimesh内に含まれない
        if len(plys) == 0:
            # print('エラー:スライス断面ポリゴンがtrimesh内に含まれない')
            Logger.error(Trace.execution_location(), 'エラー:スライス断面ポリゴンがtrimesh内に含まれない')

        # ポリゴンノードの並び順をCCWに揃える
        for i in range(len(plys)):
            plys[i] = plys[i] if is_ccw(plys[i]) else reverse(plys[i])

        rslt.append(plys)

    return rslt


def vtx_angl_dst(crd:coords.CoordinateSequence) -> list:
    """ポリゴンの頂点ごとになす角と前後ノード線分からの距離、傾きを求める

    Parameters
    ----------
    crd : coords.CoordinateSequence
        _description_

    Returns
    -------
    list
        [{'angl':なす角(deg), 'vdst':前後ノードからの離れ, 'grd':前後ノード線分に対する傾き}, {}, ...]
    """
    # 開始ノードと終端ノードが一致しない場合は処理しない
    if crd[0]!=crd[-1]:
        return None
    # 開始ノードと重複する終端ノードを含めない
    npc = numpy.array(crd)
    # 前後ノードを参照するため、先頭に終端ノードの一つ前を追加する
    npc = numpy.vstack([npc[-2], npc])
    # 各辺の長さを求めておく
    edgln = [numpy.linalg.norm(npc[i+1]-npc[i]) for i in range(len(npc)-1)]
    # 各頂点で、前後ノードとの内積を求める
    angl = [numpy.dot(npc[i+1]-npc[i], npc[i-1]-npc[i])/edgln[i]/edgln[i-1] for i in range(1, len(npc)-1)]
    angl = [degrees(acos(-1.0 if a < -1.0 else (1.0 if a > 1.0 else a))) for a in angl]
    # 各頂点前後ノード間の距離を求める
    ndst = [numpy.linalg.norm(npc[i-1]-npc[i+1]) for i in range(1, len(npc)-1)]
    # 前後ノード線分と各頂点の距離を求める
    vdst = [numpy.cross(npc[i]-npc[i-1], npc[i+1]-npc[i])/ndst[i-1] for i in range(1, len(npc)-1)]
    # 前後ノード線分と各頂点の距離から、傾きを求める
    grds = [2.0 * vdst[i] / ndst[i] for i in range(len(vdst))]

    # 各頂点の角度と、前後ノード線分からの離れと傾きを返す
    return list([{'angl':a, 'vdst':v, 'grd':g} for (a,v,g) in zip(angl,vdst,grds)])

def lexsort_xy(crd:coords.CoordinateSequence) -> coords.CoordinateSequence:
    """左下が開始ノードとなるように、並べなおす

    Parameters
    ----------
    crd : coords.CoordinateSequence
        座標配列, ※開始ノードと終端ノードは重複している前提

    Returns
    -------
    coords.CoordinateSequence
        左下から始まる座標配列, ※開始ノードと終端ノードは重複する
    """
    # 開始ノードと終端ノードが一致しない場合は処理しない
    if crd[0]!=crd[-1]:
        return crd

    # 開始ノードと重複する終端ノードを含めない
    npcrd = numpy.array(crd[:-1])
    # X->Yの順で昇順にソートする
    ind = numpy.lexsort((npcrd[:,0],npcrd[:,1]))
    if ind[0] != 0:
        # X,Y最小ノードインデックスから終端までのノード座標配列
        frst = npcrd[ind[0]:, :]
        # インデックスゼロからX,Y最小ノードインデックスまでのノード座標配列
        scnd = npcrd[:ind[0], :]
        # X,Y最小ノードインデックスから始まるノード座標配列を生成する
        npcrd = numpy.concatenate([frst, scnd])

    # 終端ノードとして開始ノードを追加する
    npcrd = numpy.vstack((npcrd, npcrd[0]))

    return coords.CoordinateSequence(npcrd)

def ply_node_shift(src:Polygon) -> Polygon:
    """ポリゴンノードを左下が開始ノードとなるように並べ直す

    Parameters
    ----------
    src : Polygon
        ポリゴン

    Returns
    -------
    Polygon
        左下を開始ノードとするポリゴン
    """
    # 外周ノードをソートする
    extcrd = lexsort_xy(src.exterior.coords)
    # 内周ノードをソートする
    n_intr = get_num_interior_rings(src)
    intrcrds = list()
    for i in range(n_intr):
        intrcrd = lexsort_xy(src.interiors[i].coords)
        intrcrds.append(intrcrd)
        pass
    # ポリゴンを生成する
    rslt = Polygon(shell=extcrd, holes=intrcrds)
    return rslt

def simplify_with_angles(src:Polygon, tol_angl:float=179.9, tol_dst:float=5.0e-3) -> Polygon:
    """各頂点のなす角を利用して、ポリゴンを単純化する
    shapely.simplify()は、線分からの距離だけで判定するため、必要なノードも取り除いてしまう恐れがある。
    そこで以下1)を参考に、頂点のなす角度(≒180度は削除する)を条件に加えて、単純化する
    1) https://github.com/92kns/simple-shapely-simplify-alternative

    Parameters
    ----------
    src : Polygon
        shapely.Polygon
    tol_angl : float
        頂点のなす角が閾値以上の場合は取り除く, defaults by 179.9
    tol_dst : float
        頂点と前後線分からの距離が閾値以下の場合は取り除く, defaults by 5.0e-4

    Returns
    -------
    Polygon
        単純化したshapely.Polygon
    """

    def vldt_vtx(a:float, v:float) -> bool:
        """頂点のなす角と距離の判定条件
        なす角が閾値(tol_angl)以上かつ頂点距離が閾値(tol_dst)未満の場合は除外される
        ⇔ なす角が閾値未満または頂点距離が閾値以上の場合は採用される

        Parameters
        ----------
        a : float
            頂点のなす角(deg)
        v : float
            頂点との距離

        Returns
        -------
        bool
            True:採用, False:除外
        """
        from math import fabs
        # なす角が閾値以上かつ頂点距離が閾値未満の場合は除外される
        # ⇔ なす角が閾値未満または頂点距離が閾値以上の場合は採用される
        return not(a >= tol_angl and fabs(v) < tol_dst)

    # shapely.simplyfy(tol=0.0)で重複ノードを除く
    smpl = simplify(src, tolerance=0.0)
    # 左下ノードを開始ノードとする
    shft = ply_node_shift(smpl)
    # 各ノードの角度と距離、傾きを求める, 座標配列を２次元に変換する
    frc2d = force_2d(shft)
    extcndtn = vtx_angl_dst(frc2d.exterior.coords)
    # 開始ノードは間引かない
    extcrd = [shft.exterior.coords[0]]
    # 条件に合致する座標を抽出する（なす角が閾値以上かつ頂点距離が閾値未満の場合は取り除かれる）
    extcrd += [shft.exterior.coords[i] for i in range(1, len(extcndtn)) if vldt_vtx(extcndtn[i]['angl'], extcndtn[i]['vdst'])]

    # 内周ノードについても抽出処理を行う
    n_intr = get_num_interior_rings(src)
    intrcrds = list()
    for n in range(n_intr):
        intcndtn = vtx_angl_dst(frc2d.interiors[n].coords)
        # 開始ノードは間引かない
        intcrd = [shft.interiors[n].coords[0]]
        # 条件に合致する座標を抽出する（なす角が閾値以上かつ頂点距離が閾値未満の場合は取り除かれる）
        intcrd += [shft.interiors[n].coords[i] for i in range(1, len(intcndtn)) if vldt_vtx(intcndtn[i]['angl'], intcndtn[i]['vdst'])]
        intrcrds.append(intcrd)

    # ポリゴンを生成する
##    rslt = Polygon(shell=extcrd, holes=intrcrds)
##    return rslt

    # KKC修正2025/12/08▼▼▼ 修正：点が4つ未満（3つ以下）になってしまった穴を除外する ▼▼▼
    valid_holes = []
    if intrcrds is not None:
        for hole in intrcrds:
            # 始点と終点が同じで閉じているはずなので、最低4点必要
            if len(hole) >= 4:
                valid_holes.append(hole)

    # 修正後の穴リストを使ってポリゴンを作成
    rslt = Polygon(shell=extcrd, holes=valid_holes)
    # ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    return rslt



def grnd_height_lod2(plbld:plBuilding) -> float:
    """【地盤高取得LOD2】
        plateaupy.plbldg.Building.lod2groundの最小Ｚ座標

    Parameters
    ----------
    plbld : plBuilding
        plateaupy.plbldg.Buildingクラス

    bldHght : float
        総高さ

    Returns
    -------
    float
        地盤高(m), (lod2ground.Z)の最小
    """
    return numpy.min([z for dct in plbld.lod2ground.values() for lrng in dct for (x,y,z) in lrng])

def flr_z_lst(fstflrHght:float, bldgHght:float, nflr:int) -> list:
    """【各階床高さ算定】

    Parameters
    ----------
    fstflrHght : float
        １階床高さ(m)
    bldgHght : float
        建物総高さ(m)
    nflr : int
        建物階数

    Returns
    -------
    list
        [flr1_Z, flr2_Z, .. ]：各階床高さリスト
    """
    return [fstflrHght + i * (bldgHght - fstflrHght)/nflr for i in range(nflr)]

def smfrHeimenLst_lod2(xybld:plBuilding, bldgHght:float, grndHght:float, fstflrHght:float, hasRoof:bool, nflr:int,
                       tol_angl:float=179.9, tol_vdst:float=5.0e-3, outMltplygn:bool=True) -> list:
    """【平面形状種算定（LOD2Solid版）】
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
    tol_angl : float
        simplify_with_angles()に引き渡す、なす角の閾値, defaults by 179.9
    tol_vdst : float
        simplify_with_angles()に引き渡す、頂点距離の閾値, defaults by 5.0e-3
    outMltplygn : bool
        True=スライス時のマルチポリゴンをそのまま出力する, False=マルチポリゴンの中で面積最大を出力する

    Returns
    -------
    list
        [smfrheimen_0, smfrheimen_1, .. , smfrheimen_n-1]
    """
    # 各階の床高さからスライス面までの相対高さ
    __slc_rZ = 1.0
    # １階あたりの高さ確認：1m未満の場合はスライスできない可能性がある
    if (bldgHght-fstflrHght)/nflr < __slc_rZ:
        _flr_hgt = (bldgHght-fstflrHght)/nflr
        Logger.debug(Trace.execution_location(),
            f'建物ID={xybld.attr.get("建物ID")},階高(={_flr_hgt:.2f})がスライス面高{__slc_rZ}より小さい．')

    # lod2roof, lod2wall, lod2ground からTrimeshを生成する
    # 【確認済み】lod2windowは参照しない
    tmsh = xybldg_to_trimesh(xybld, cnv_wndw=False)

    # 各階床高さ判定メソッド呼び出し,
    # ※インデックスに注意, flrzlst[0]=１階, flrzlst[1]=２階 ...
    flrzlst = flr_z_lst(fstflrHght, bldgHght, nflr)
    # 各階天井高さを設定しておく
    ceilzlst = (flrzlst[1:] if len(flrzlst) > 1 else []) + [bldgHght]

    # スライスするXY平面のZ高さリストを生成する
    slc_z_lst = [flrz + __slc_rZ for flrz in flrzlst]
    # 各階床高さ+1.0mのZ断面でスライスする
    Logger.debug(Trace.execution_location(), f'slice_xy_planes():建物ID={xybld.attr.get("建物ID")}')
    slc_rslt = slice_xy_planes(tmsh, slc_z_lst)

    # 最大面積ポリゴンのみを出力する場合
    if outMltplygn is False:
        for i, slcplylst in enumerate(slc_rslt):
            if len(slcplylst) > 1:
                # 面積最大が先頭となるよう、ポリゴンリストをソートし、先頭を取り出す
                maxply = sorted(slcplylst, key=lambda x: x.area, reverse=True)[0]
            else:
                maxply = slcplylst[0]

            # 最大面積ポリゴンのみのリストへ更新する
            slc_rslt[i] = [maxply]

    # 各階のポリゴンを単純化する
    for i, slcplylst in enumerate(slc_rslt):
        slc_rslt[i] = [simplify_with_angles(ply, tol_angl=tol_angl, tol_dst=tol_vdst) for ply in slcplylst]

    # 各階のポリゴンを２次元MultiPolygonに変換し、階数情報や床高さ、天井高さとまとめてタプルを生成する
    lst_flrinfo = [{'poly':MultiPolygon([force_2d(ply) for ply in slcplylst]), 'num_flr':1, 'flr_z':flrzlst[i], 'cil_z':ceilzlst[i]} for (i, slcplylst) in enumerate(slc_rslt)]

    # １つ下の階のポリゴンと比較して、同一形状であれば破棄する処理を２階から順に繰り返す
    for i in range(len(lst_flrinfo)):
        if i == 0:
            continue

        prv_flr = lst_flrinfo[i-1]
        crt_flr = lst_flrinfo[i]
        # １つ下の階のポリゴンと比較して、同一形状であれば破棄する
        if equals_exact(prv_flr['poly'], crt_flr['poly']) == True:
            # 階数を合算する
            crt_flr['num_flr'] += prv_flr['num_flr']
            # 床高さを更新する
            crt_flr['flr_z'] = prv_flr['flr_z']
            # 下階情報を削除する
            lst_flrinfo[i-1] = None

    lsthmn = list()
    for (i, flrinf) in enumerate(lst_flrinfo):
        if flrinf is None:
            continue

        hmn_lz = flrinf['flr_z']
        hmn_uz = flrinf['cil_z']
        # 平面形状種クラス生成
        wrkhmn = smfrheimen()
        wrkhmn.lwr_hght = hmn_lz
        wrkhmn.upr_hght = hmn_uz
        wrkhmn.rf_hght = wrkhmn.upr_hght if hasRoof else 0.0
        wrkhmn.n_flr = flrinf['num_flr']
        wrkhmn.zaishitu = 1
        # 平面形状のMultiPolygonについて処理を繰り返す
        for ply in list(get_parts(flrinf['poly'])):
            # 外周ポリゴン座標生成（先頭ノードと終端ノードは重ならない）
            wrkhmn.smfrhmnvtxs += [smfrheimenvtx(xy[0],xy[1],hmn_lz,hmn_uz,(1 if i==0 else 0)) for (i,xy) in enumerate(ply.exterior.coords[:-1])]
            if len(ply.interiors)>0:
                for intr in ply.interiors:
                    # 内周ポリゴン座標生成（先頭ノードと終端ノードは重ならない）
                    wrkhmn.smfrhmnvtxs += [smfrheimenvtx(xy[0],xy[1],hmn_lz,hmn_uz,(1 if i==0 else 0)) for (i,xy) in enumerate(intr.coords[:-1])]

        # 頂点数をセットする
        wrkhmn.n_vtx = len(wrkhmn.smfrhmnvtxs)

        lsthmn.append(wrkhmn)

    return lsthmn

def dist_pnt_heimen_hrz(cntr:numpy.array, sfhmn:smfrheimen) -> float:
    """３次元窓面重心座標と平面形状種の包含を判定し、最も高い平面形状を取得する

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
    """
    # 平面形状種と評価点の包含を判定する
    from shapely import Point, Polygon, distance, dwithin
    wndpnt = Point(cntr[:2])
    hmnply = Polygon([numpy.array([hmnvtx.x,hmnvtx.y]) for hmnvtx in sfhmn.smfrhmnvtxs])
    if dwithin(hmnply, wndpnt, distance=0.0):
        pass

    return distance(wndpnt, hmnply)

def search_heimen_hrz(cntr:numpy.array, smfrhmnlst:list) -> int:
    """３次元窓面（水平）重心を包含し、最も高い平面形状種のインデックス番号を返す

    Parameters
    ----------
    cntr : numpy.array
        ３次元水平窓面重心座標
    smfrhmnlst : list
        市街地火災シミュレーション用データ：平面形状種リスト

    Returns
    -------
    int
        窓面重心に最も近い平面形状種リストインデックス番号[0～n-1], n=リスト長さ
    """
    # 平面形状種ポリゴンを生成する
    hmnplylst = [smfrhmn_to_mltpoly(sfhmn) for sfhmn in smfrhmnlst]
    # 窓面重心と平面形状種の距離を求める
    dstlst = [distance(cntr, hmnply) for hmnply in hmnplylst]
    # 平面形状上端高さリストを取得する
    upzlst = [sfhmn.upr_hght for sfhmn in smfrhmnlst]
    # (idx, dist, uprz)のタプルリストを生成する
    idzlst = zip(range(len(smfrhmnlst)), dstlst, upzlst)
    # dst(=tpl[1]) を昇順でソートし、次にuprz(=tpl[2])を降順でソートして、idx(tpl[0])を取得する
    minidx = sorted(idzlst, key=lambda tpl: (tpl[1], -1.0 * tpl[2]))[0][0]
    return minidx

if __name__ == '__main__':
    pass