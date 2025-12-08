import plateaupy

def load_plateau_bldg(gmlpth:str) -> dict:
    """plateau.GMLファイルを読み込み、建物データ辞書を取得する

    Parameters
    ----------
    gmlpth : str
        GMLファイルの検索パス

    Returns
    -------
    dict
        plparser.bldg={obj.location_0 : plbldg_0クラス, obj.location_1 : plbldg_1クラス}
    """
    # scan paths
    pl = plateaupy.plparser(gmlpth)
    # options
    ploptns = plateaupy.ploptions()
    ploptns.bUseLOD0 = False
    ploptns.bUseLOD1 = False
    ploptns.bUseLOD2texture = False
    ploptns.texturedir = 'cached'
    ploptns.bHeightZero = False
    quarter = None
    # if args.quarterx is not None and args.quartery is not None:
    # 	quarter = (args.quartery, args.quarterx)
    ploptns.div6toQuarter = quarter
    # load
    pl.loadFiles( bLoadCache=False, cachedir='cached', kind=0, location=-1, options=ploptns )

    return pl.bldg

if __name__ == '__main__':
    pass