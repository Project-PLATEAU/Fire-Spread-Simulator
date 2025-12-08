class smfrheaderinfo:
    """市街地火災シミュレーション用データ：ヘッダー情報
    """
    def __init__(self) -> None:
        self.dataVersion = ''
        """データバージョン '**'
        """
        self.projection = ''
        """座標系指定 'XY[01-19]'
        """
        self.reserved = ''
        """任意文字列 '…(任意)…'
        """

    def from_smfrstr(self, smfrstr:str, strt:int=0) -> int:
        """<Header>～</Header>で囲まれた文字列からヘッダ情報を生成する

        Parameters
        ----------
        s_hdr : str
            <Header>～</Header>を含む文字列

        Returns
        -------
        int
            処理済み
        """
        bg = smfrstr.lower().find(r'<header>')
        ed = smfrstr.lower().find(r'</header>', bg+9)
        if bg < 0 or ed < 0:
            # <header> </header> がみつからない
            return len(smfrstr)

        wrkhdr = smfrstr[bg+9:ed]
        
        tgt = '<dataversion>'
        bg1 = wrkhdr.lower().find(tgt)
        if bg1 >= 0:
            bg1 += len(tgt)
            ed1 = wrkhdr.find('\n', bg1)
            self.dataVersion = wrkhdr[bg1:ed1]
        
        tgt = '<projection>'
        bg2 = wrkhdr.lower().find(tgt)
        if bg2 >= 0:
            bg2 += len(tgt)
            ed2 = wrkhdr.find('\n', bg2)
            self.projection = wrkhdr[bg2:ed2]

            ed3 = wrkhdr.find('\n', ed2+1)
            self.reserved = wrkhdr[ed2+1:ed3]
        
        return ed + len('</header>\n')

    def to_smfrstr(self) -> str:
        """火災シミュレーション用ヘッダ情報文字列生成

        Returns
        -------
        str
            火災シミュレーション用ヘッダ情報文字列 '<Header>\\n～</Header>\\n'
        """
        ret = '<Header>\n'
        ret += f'<dataVersion>{self.dataVersion}\n'
        ret += f'<projection>{self.projection}\n'
        ret += f'{self.reserved}\n'
        ret += '</Header>\n'
        return ret

class smfrinput:
    """市街地火災シミュレーション用データ
    """
    def __init__(self) -> None:
        self.hdr_inf = None
        """ヘッダー情報クラス
        """
        self.n_bldgs = 0
        """建物数
        """
        self.smfrbldgs = list()
        """建物リスト [smfrbldg_0, smfrbldg_1, .. , smfrbldg_n-1]
        """

    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        # ヘッダ情報
        rslt = self.hdr_inf.to_smfrstr()
        # 建物数
        rslt += f'{self.n_bldgs}\n'
        # 建物データ文字列
        rslt += ''.join([b.to_smfrstr() for b in self.smfrbldgs])
        return rslt

class smfrbldg:
    """市街地火災シミュレーション用データ：建物
    """
    def __init__(self) -> None:
        self.bldid = ''
        """建物ID, '/core:CityModel/core:cityObjectMember/bldg:Building/gen:stringAttribute'
        <gen:stringAttribute name="建物ID">
            <gen:value>14130-bldg-241873</gen:value>
        </gen:stringAttribute>
        """
        self.flrhght = 0.0
        """床高さ(m), '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:floorHeight'
        <sim:floorHeight>0.3</sim:floorHeight>
        """
        self.n_heimen = 0
        """平面形状種数
        """
        self.n_fuzoku = 0
        """付属面数
        """
        self.n_kaikou = 0
        """開口部数
        """
        self.kouzou = 0
        """構造,防耐火性の構造分類, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:fireproofStructureCityFireSimulationType'
        <sim:fireproofStructureCityFireSimulationType codeSpace="../../codelists/Building_fireproofStructureCityFireSimulationType.xml">5</sim:fireproofStructureCityFireSimulationType>
        """
        self.shubetu = 0
        """木・非木種別, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:buildingStructureCityFireSimulationType'
        <sim:buildingStructureCityFireSimulationType codeSpace="../../codelists/Building_buildingStructureCityFireSimulationType.xml">1</sim:buildingStructureCityFireSimulationType>
        """
        self.youto = 0
        """用途, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:usageCityFireSimulation'
        <sim:usageCityFireSimulation codeSpace="../../codelists/Building_usageCityFireSimulation.xml">0</sim:usageCityFireSimulation>
        """
        self.cnstrct_yr = 0
        """築年数, '/core:CityModel/core:cityObjectMember/bldg:Building/bldg:yearOfConstruction'
        ※GMLファイルに bldg:yearOfConstruction がない
        デフォルトは<--要確認-->とする
        """
        self.bouka = 0
        """防火地域,防火地域・準防火地域等, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:districtsAndZonesCityFireSimulationType'
        <sim:districtsAndZonesCityFireSimulationType codeSpace="../../codelists/Building_districtsAndZonesCityFireSimulationType.xml">2</sim:districtsAndZonesCityFireSimulationType>
        """
        self.jishin = 0
        """地震被害分類, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:earthquakeType'
        <sim:earthquakeType codeSpace="../../codelists/Building_earthquakeType.xml">0</sim:earthquakeType>
        """
        self.smfrhmns = list()
        """平面形状種リスト [smfrheimen_0, smfrheimen_1, .. , smfrheimen_n-1]
        """
        self.smfrfzks = list()
        """付属面リスト [smfrfuzoku_0, smfrfuzoku_1, .. , smfrfuzoku_n-1]
        """
        self.smfrkakos = list()
        """開口部リスト [smfrkaikou_0, smfrkaikou_1, .. , smfrkaikou_n-1]
        """
    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        # 建物IDや平面形状などを
        # １行目：建物ID, ...
        sfstr = f'{self.bldid},{self.flrhght},{self.n_heimen},{self.n_fuzoku},{self.n_kaikou},'
        sfstr += f'{self.kouzou},{self.shubetu},{self.youto},{self.cnstrct_yr},{self.bouka},{self.jishin}\n'
        # 平面形状種
        sfstr += ''.join([sfhmn.to_smfrstr() for sfhmn in self.smfrhmns])
        # 付属面
        sfstr += ''.join([sffzk.to_smfrstr() for sffzk in self.smfrfzks])
        # 合計開口部
        sfstr += ''.join([sfkako.to_smfrstr() for sfkako in self.smfrkakos])
        return sfstr

class smfrheimen:
    """市街地火災シミュレーション用データ：平面形状種
    """
    def __init__(self) -> None:
        self.n_vtx = 0
        """平面形状種頂点数
        """
        self.lwr_hght = 0.0
        """平面形状下端高さ,
        １階床高さ(= 地盤高さ(grndlvl) + 標準基礎高さ(外部入力))
        """
        self.upr_hght = 0.0
        """平面形状上端高さ,
        bldg:lod0FootPrint//gml:posList.Z座標 + 総高さ
            or
        bldg:lod0RoofEdge//gml:posList.Z座標
        """
        self.rf_hght = 0.0
        """屋根部分高さ,
        bldg:RoofSurface//gml:posList.Z座標の最大値
        ※屋根がない場合はゼロ
        """
        self.n_flr = 0
        """階数, (総高さ - １階床高さ)/標準階高[外部入力]
        ※１階床高さ(= 地盤高さ(grndlvl) + 標準基礎高さ(外部入力))
        ※総高さ(= bldg:lod1Solid//gml:surfaceMember//gml:posList.座標の最大値 - 最小値)
        """
        self.zaishitu = 1
        """平面形状種：材質ID, 平面形状種の材質ＩＤは、１固定
        """
        self.smfrhmnvtxs = list()
        """平面形状種頂点リスト [smfrheimenvtx_0, smfrheimenvtx_1, .. , smfrheimenvtx_n-1]
        """
    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        sfstr = f'{self.n_vtx},{self.lwr_hght},{self.upr_hght},{self.rf_hght},{self.n_flr},{self.zaishitu}\n'
        sfstr += ''.join([vt.to_smfrstr() for vt in self.smfrhmnvtxs])
        return sfstr

class smfrheimenvtx:
    """市街地火災シミュレーション用データ：平面形状頂点
    """
    def __init__(self, x:float=0.0, y:float=0.0, lz:float=0.0, uz:float=0.0, lpflg:int=0, zst:int=1) -> None:
        self.x = x
        """平面形状種Ｘ座標（頂点の並び順は、上または外から見て反時計周り）
        """
        self.y = y
        """平面形状種Ｙ座標（頂点の並び順は、上または外から見て反時計周り）
        """
        self.lz = lz
        """平面形状種下座標Ｚ
        """
        self.uz = uz
        """平面形状種上座標Ｚ
        """
        self.flg_loopstart = lpflg
        """ループ開始フラグ, ループ開始頂点は１、それ他はゼロ
        """
        self.zaishitu = zst
        """平面形状種頂点：材質ＩＤ, 平面形状種の材質ＩＤは１固定
        """
    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        sfstr = f'{self.x},{self.y},{self.lz},{self.uz},{self.flg_loopstart},{self.zaishitu}\n'
        return sfstr

class smfrfuzoku:
    """市街地火災シミュレーション用データ：付属面
    """
    def __init__(self) -> None:
        self.n_vtx = 0
        """付属面頂点数
        """
        self.thick = 0.0
        """厚さ, ゼロ固定
        """
        self.zaishitu = 0
        """平面形状種：材質ＩＤ, 付属面の材質ＩＤはゼロ固定
        """
        self.smfrfzkvtxs = list()
        """付属面頂点リスト [smfrfuzokuvtx_0, smfrfuzokuvtx_1, .. , smfrfuzokuvtx_n-1]
        """
    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        sfstr = f'{self.n_vtx},{self.thick},{self.zaishitu}\n'
        sfstr += ''.join([vt.to_smfrstr() for vt in self.smfrfzkvtxs])
        return sfstr

class smfrfuzokuvtx:
    """市街地火災シミュレーション用データ：付属面頂点
    """
    def __init__(self, x:float=0.0, y:float=0.0, z:float=0.0) -> None:
        self.x = x
        """付属面Ｘ座標
        """
        self.y = y
        """付属面Ｙ座標
        """
        self.z = z
        """付属面Ｚ座標
        """
    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        sfstr = f'{self.x},{self.y},{self.z}\n'
        return sfstr

class smfrkaikou:
    """市街地火災シミュレーション用データ：開口部
    """
    def __init__(self) -> None:
        self.blng_hmn = 0
        """平面形状種番号, 開口部の所属する平面形状種番号（ゼロからの連番）
        """
        self.blng_hkm = 0
        """壁面番号, 開口部の所属する壁面番号（ゼロからの連番）, 屋根面の場合は-1
        """
        self.zaishitu = 0
        """開口部：材質ＩＤ, '/core:CityModel/core:cityObjectMember/bldg:Building/bldg:boundedBy/bldg:WallSurface/bldg:opening/sim:Swindow/sim:materialReferenceType'
        <sim:materialReferenceType codeSpace="../../codelists/Building_materialReferenceType.xml">23</sim:materialReferenceType>
        """
        self.vrt_hrz = 0.0
        """垂直・水平, '/core:CityModel/core:cityObjectMember/bldg:Building/bldg:boundedBy/bldg:WallSurface/bldg:opening/sim:Swindow/sim:directionOfWindow'
        <sim:directionOfWindow>1</sim:directionOfWindow>
        """
        self.x1 = 0.0
        """開口部左下Ｘ座標
        """
        self.y1 = 0.0
        """開口部左下Ｙ座標
        """
        self.z1 = 0.0
        """開口部左下Ｚ座標
        """
        self.x2 = 0.0
        """開口部「右下」Ｘ座標
        """
        self.y2 = 0.0
        """開口部「右下」Ｙ座標
        """
        self.z2 = 0.0
        """開口部「右上」Ｚ座標, 水平開口部の場合は奥行
        """
    def to_smfrstr(self) -> str:
        """火災シミュレーション用データ文字列生成

        Returns
        -------
        str
            火災シミュレーション用データ文字列
        """
        sfstr = f'{self.blng_hmn},{self.blng_hkm},{self.zaishitu},{self.vrt_hrz},'
        sfstr += f'{self.x1},{self.y1},{self.z1},{self.x2},{self.y2},{self.z2}\n'
        return sfstr

class smfroutput:
    """市街地火災シミュレーション出力データ
    """
    def __init__(self) -> None:
        self.hdr_inf = None
        """ヘッダ情報クラス
        """
        self.smfrbldrslts = list()
        """建物出力結果リスト [smfrbldresult_0, smfrbldresult_1, .. , smfrbldresult_n-1]
        """

class smfrbldresult:
    """市街地火災シミュレーション出力データ：建物
    """
    def __init__(self) -> None:
        self.meshno = 0
        """メッシュ番号, ゼロ固定
        """
        self.bldno = 0
        """建物番号, ファイル内の記述順（ゼロ～）
        """
        self.bldid = ''
        """建物ＩＤ, 入力データの建物ＩＤ
        """
        self.ignitetms = 0
        """出火時刻, 
        出火時刻出力先xpath, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:igniteTimeSec'
        """
        self.brnouttms = 0
        """燃え尽き時刻, 
        燃え尽き時刻出力先xpath, '/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation/sim:burnoutTimeSec'
        """

    @classmethod
    def from_smfrstr(cls, smfrstr:str) -> object:
        """火災シミュレーション出力文字列（１行分）から市街地火災シミュレーション出力クラスを生成する

        Parameters
        ----------
        smfrstr : str
            火災シミュレーション出力文字列（１行分）

        Returns
        -------
        object
            火災シミュレーション出力クラス
        """
        sfrslt = smfrbldresult()
        wrklst = smfrstr.rstrip('\r\n').split(',')
        sfrslt.meshno = int(wrklst[0])
        sfrslt.bldno = int(wrklst[1])
        sfrslt.bldid = str.strip(wrklst[2])
        sfrslt.ignitetms = int(wrklst[3])
        sfrslt.brnouttms = int(wrklst[4])
        return sfrslt

def test_hdr_inf() -> None:
    hdr_inf = smfrheaderinfo()
    hdr_inf.dataVersion = '123456'
    hdr_inf.projection = 'XY09'
    hdr_inf.reserved = 're'
    hoge = hdr_inf.to_smfrstr()
    hdr_inf2 = smfrheaderinfo()
    hdr_inf2.from_smfrstr(hoge)
    print(hdr_inf2)
    pass

def test_smfrinput() -> None:
    hoge = smfrinput()
    hoge.hdr_inf = smfrheaderinfo()
    hoge.smfrbldgs = [smfrbldg(), smfrbldg()]
    hoge.n_bldgs = len(hoge.smfrbldgs)
    print(hoge)

def test_smfrbldg() -> None:
    bld = smfrbldg()
    bld.bldid = '123456'
    bld.flrhght = 0.3
    bld.kouzou = 1
    bld.shubetu = 2
    bld.youto = 3
    bld.cnstrct_yr = 4
    bld.bouka = 5
    bld.jishin = 6
    bld.smfrhmns = [smfrheimen(), smfrheimen()]
    bld.n_heimen = len(bld.smfrhmns)
    bld.smfrfzks = [smfrfuzoku(), smfrfuzoku()]
    bld.n_fuzoku = len(bld.smfrfzks)
    bld.smfrkakos = [smfrkaikou(), smfrkaikou()]
    bld.n_kaikou = len(bld.smfrkakos)
    print(bld)

def test_smfrout() -> None:
    smfro = smfroutput()
    smfro.hdr_inf = smfrheaderinfo()
    smfro.smfrbldrslts = [smfrbldresult(), smfrbldresult()]
    print(smfro)

if __name__ == '__main__':
    """
    # test_hdr_inf()
    # test_smfrinput()
    # test_smfrbldg()
    # test_smfrout()
    # with open(r'smfrdata.txt', 'rt', encoding='cp932') as fin:
    #     smfrstr = ''.join(fin.readlines())
    # sfhdrinf = smfrheaderinfo()
    # sfhdrinf.from_smfrstr(smfrstr)
    # print(sfhdrinf)
    rslt1 = smfrbldresult()
    rslt1.bldid = 'rslt1'
    rslt1.ignitetms = 10
    rslt1.brnouttms = 100
    rslt2 = smfrbldresult()
    rslt2.bldid = 'rslt2'
    rslt2.ignitetms = 20
    rslt2.brnouttms = 200
    hoge = dict([(r.bldid, (r.ignitetms, r.brnouttms)) for r in [rslt1, rslt2]])
    print(hoge)
    """
    pass