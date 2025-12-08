import xml.etree.ElementTree as ET
from lxml import etree

def main() -> None:
    with open(r'53392545_bldg_6697.gml', mode='rt', encoding='utf-8') as fin:
        s = ''.join([ln.rstrip('\n\r') for ln in fin.readlines()])
        s = s.replace('\t','')

    # et = ET.fromstring(s)
    # print(et)
    utf8prsr = etree.XMLParser(encoding='utf-8')
    et = etree.fromstring(s.encode('utf-8'), parser=utf8prsr)
    hoge = et.xpath(r'/core:CityModel/core:cityObjectMember/bldg:Building/sim:cityFireSimulation/sim:CityFireSimulation', namespaces=et.nsmap)
    print(hoge)

main()