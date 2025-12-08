/**
 * 建物レイヤーのディクショナリ
 * key メッシュ番号
 * val メッシュの建物レイヤー
 */
var buildingLayerDict = {};

// 構造タイプに対応する配色
function getBuildingColor(type) {
    return type === '1'
        ? "#7F7FFF"
        : type === '2'
            ? "#AFAFFF"
            : type === '3'
                ? "#FFFFAF"
                : type === '4'
                    ? "#FFFFFF"
                    : type === '5'
                        ? "#FFFFFF"
                        : type === '6'
                            ? "#FFFFFF"
                            : "#FFFFFF";
}

// GeoJSONのスタイル関数
function geoJsonBuildingLayerStyle(feature) {

    return {
        color: '#000000',
        opacity: 1,
        weight: 1,
        fillColor: getBuildingColor(feature.properties.fireproofStructureType),
        fillOpacity: 0.5,
        fill: true,
    };
}

function updateBuildingLayer() {

    // 編集中はすべて非表示にして終了
    if (isSimulationRangeEditing) {

        for (var meshNum in buildingLayerDict) {

            buildingLayerDict[meshNum].removeFrom(map);
        }

        buildingLayerDict = {};
        return;
    }

    // 編集中でない場合は選択されたメッシュのみ表示
    var deleteMeshNumbers = [];
    for (var meshNum in buildingLayerDict) {

        if (!(meshNum in selectedMeshNumberDict)) {
            deleteMeshNumbers.push(meshNum);
        }
    }

    for (var meshNum of deleteMeshNumbers) {

        buildingLayerDict[meshNum].removeFrom(map);
        delete buildingLayerDict[meshNum];
    }

    for (var meshNum of Object.keys(selectedMeshNumberDict)) {

        if (meshNum in buildingLayerDict) {
            continue;
        }

        var buildingDataPath = 'file:///' + baseDirPath + '/bldg_geojson/Building_' + meshNum + '.geojson';

        const xhr = new XMLHttpRequest();
        var text;

        xhr.open('GET', buildingDataPath, false); // GETでローカルファイルを開く
        xhr.onload = () => text = xhr.responseText;
        xhr.onerror = () => console.log("error!");
        xhr.send();

        var data = JSON.parse(text);

        var buildingLayer = L.geoJson(data);
        buildingLayer.setStyle(geoJsonBuildingLayerStyle);
        buildingLayer.addTo(map);

        buildingLayerDict[meshNum] = buildingLayer;
    }
}