# 標準ライブラリ
import argparse
import json
# 追加ライブラリ
from lxml import etree
# 本業務で作成したクラス
from smfr_io import smfroutput, smfrheaderinfo, smfrbldresult

def load_simfire_result(rsltfn:str) -> smfroutput:
    """火災シミュレーション出力ファイルを読み込む

    Parameters
    ----------
    rsltfn : str
        火災シミュレーション出力ファイル名

    Returns
    -------
    smfroutput
        火災シミュレーション出力クラス
    """
    # 火災シミュレーション出力ファイルを開いてデータを読み込む
    with open(rsltfn, 'rt', encoding='cp932') as fin:
        smfrstr = ''.join(fin.readlines())
    # 改行コードを統一する
    smfrstr = smfrstr.replace('\r\n', '\n')
    # ヘッダ情報を取得する
    sfhdr = smfrheaderinfo()
    pos = sfhdr.from_smfrstr(smfrstr)
    # シミュレーション出力結果を取得する
    rsltlst = [smfrbldresult.from_smfrstr(rsltstr) for rsltstr in smfrstr[pos:].split('\n')]
    # 火災シミュレーション出力クラスを生成する
    sfout = smfroutput()
    sfout.hdr_inf = sfhdr
    sfout.smfrbldrslts = rsltlst
    return sfout

def add_simfire_result(rsltfn:str, igmlfn:str, ogmlfn:str) -> None:
    """火災シミュレーション出力データを、GMLファイルに追記する

    Parameters
    ----------
    rsltfn : str
        火災シミュレーション出力データファイル名
    igmlfn : str
        （追記前）入力GMLファイル名
    ogmlfn : str
        （追記後）出力GMLファイル名
    """
    # 火災シミュレーション出力データを読み込む
    sfout = load_simfire_result(rsltfn)
    # 出力データクラスから、建物IDごとの(出火時刻, 燃え尽き時刻)辞書を生成する
    # {'建物ID_0':(出火時刻,燃え尽き時刻), '建物ID_1':(出火時刻,燃え尽き時刻), .. ,'建物ID_n-1':(出火時刻,燃え尽き時刻) }
    dctrslt = dict([(rslt.bldid,(rslt.ignitetms, rslt.brnouttms)) for rslt in sfout.smfrbldrslts])
    # # テスト用ダミーデータ
    # dctrslt = {'14130-bldg-241873' : (10,20), '14130-bldg-243362' : (11,21)}

    # 入力GMLデータを読み込む
    with open(igmlfn, mode='rt', encoding='utf-8') as fin:
        s = ''.join([ln.rstrip('\r\n') for ln in fin.readlines()])
        s = s.replace('\t','')
    utf8prsr = etree.XMLParser(encoding='utf-8')
    root = etree.fromstring(s.encode('utf-8'), parser=utf8prsr)
    
    # sim の名前空間文字列を取得しておく
    nssm = root.nsmap['sim']

    # bldg:Building について繰り返す
    for bldg in root.xpath(r'/core:CityModel/core:cityObjectMember/bldg:Building', namespaces=root.nsmap):
        # bldidを取得する
        bldid_nds = bldg.xpath(r'gen:stringAttribute/gen:value', namespaces=root.nsmap)
        bldid = bldid_nds[0].text
        # 出火時刻,燃え尽き時刻を取得する
        (igrslt, btrslt) = dctrslt.get(bldid, (-1, -1))
        # 火災シミュレーション用の拡張部分を取得する
        ctyfrsm = bldg.xpath(r'sim:cityFireSimulation/sim:CityFireSimulation', namespaces=root.nsmap)
        # 出火時刻igniteTimeSecノードを追加する
        ignt = etree.Element(f'{{{nssm}}}igniteTimeSec', nsmap=root.nsmap)
        ignt.text = f'{igrslt}'
        ctyfrsm[0].append(ignt)
        # 燃え尽き時刻burnoutTimeSecノードを追加する
        brnt = etree.Element(f'{{{nssm}}}burnoutTimeSec', nsmap=root.nsmap)
        brnt.text = f'{btrslt}'
        ctyfrsm[0].append(brnt)
    
    # GMLファイル書き込み
    etree.indent(root, '\t')
    et = etree.ElementTree(root)
    et.write(ogmlfn, xml_declaration=True, encoding='utf-8', pretty_print=True)

def main() -> None:
    """引数は plateau_add.cfg で設定している
    """
    # import argparse
    # argprsr = argparse.ArgumentParser(description='plateau.GML -> simfire data converter.')
    # argprsr.add_argument('rsltfn', type=str, help='火災シミュレーション出力データファイル')
    # argprsr.add_argument('igmlfn', type=str, help='入力GMLファイル')
    # argprsr.add_argument('ogmlfn', type=str, help='出力GMLファイル')
    # args = argprsr.parse_args()
    # dct = vars(args)
    # with open('plateau_add.cfg','wt',encoding='cp932') as fcfg:
    #     json.dump(dct, fcfg, indent=4)
    with open('cfg/plateau_add.cfg','rt',encoding='cp932') as fcfg:
        dct = json.load(fcfg)
    add_simfire_result(**dct)

if __name__ == '__main__':
    # main()
    pass