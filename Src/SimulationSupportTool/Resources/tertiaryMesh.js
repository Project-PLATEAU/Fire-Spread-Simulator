/** 3次メッシュレイヤ */
var tertiaryMeshLayer;

/**
 * 選択された3次メッシュのメッシュ番号のディクショナリ
 * key メッシュ番号
 * val メッシュのフィーチャー
 */
var selectedMeshNumberDict = {};

/**
 * 選択された3次メッシュのメッシュ番号のディクショナリのクローン（キャンセルで元に戻す用）
 * key メッシュ番号
 * val メッシュのフィーチャー
 */
var selectedMeshNumbersClone = {};

/** シミュレーション範囲編集中かどうか */
var isSimulationRangeEditing = false;

/** シミュレーション範囲指定時ツールチップ */
var simulationRangeTooltip;

/** 3次メッシュの選択可能最大件数 */
var meshNumberMaxCount = 10;

function addSimulationRangeTooltip() {
    if (!simulationRangeTooltip) {
        simulationRangeTooltip = new L.Draw.Tooltip(map);
        simulationRangeTooltip.updateContent({ text: 'シミュレーション範囲のメッシュをクリックしてください' });
        map.on('mousemove', onMouseMoveSimulationRangeTooltip);
    }
}

function onMouseMoveSimulationRangeTooltip(e) {
    var latlng = e.latlng;
    if (simulationRangeTooltip) {
        simulationRangeTooltip.updatePosition(latlng);
    }
}

function removeSimulationRangeTooltip() {
    if (simulationRangeTooltip) {
        map.off('mousemove', onMouseMoveSimulationRangeTooltip);
        simulationRangeTooltip.dispose();
        simulationRangeTooltip = null;
    }
}

// GeoJSONのスタイル関数（選択中）
function geoStyle1(feature) {

    var meshNum = feature.properties.meshNum;

    if (meshNum in selectedMeshNumberDict) {

        // 選択済み
        return {
            color: '#C80000',
            opacity: 1,
            weight: 2,
            fillColor: '#C80000',
            fillOpacity: 0.3,
            fill: true,
        };
    }
    else {

        // 未選択
        return {
            color: '#616BDA',
            opacity: 1,
            weight: 1,
            fillColor: '#616BDA',
            fillOpacity: 0.1,
            fill: true,
        };
    }
}

// GeoJSONのスタイル関数（選択確定済み）
function geoStyle2(feature) {

    var meshNum = feature.properties.meshNum;

    if (meshNum in selectedMeshNumberDict) {

        // 選択済み
        return {
            color: '#C80000',
            opacity: 1,
            weight: 2,
            fill: false,
        };
    }
    else {

        // 未選択
        return {
            color: null,
            fill: false,
        };
    }
}

// GeoJSONのonEachFeatureオプション関数
function geoOnEachFeature(feature, layer) {

    layer.on({ click: selectMesh });

    function selectMesh(e) {

        if (!isSimulationRangeEditing) {
            return;
        }

        var meshNum = feature.properties.meshNum;

        if (meshNum in selectedMeshNumberDict) {

            // 選択済み　→　未選択へ
            delete selectedMeshNumberDict[meshNum];
        }
        else {

            if (meshNumberMaxCount <= Object.keys(selectedMeshNumberDict).length) {

                // これ以上選択できない
                showWarningMessageBox('シミュレーション範囲を選択できません。\r\n最大件数（' + meshNumberMaxCount + '件）に達しています。');
                return;
            }

            // 未選択　→　選択済みへ
            selectedMeshNumberDict[meshNum] = feature;

            if (meshNumberMaxCount <= Object.keys(selectedMeshNumberDict).length) {

                // 選択可能最大件数に達した
                showInformationMessageBox('シミュレーション範囲の\r\n選択可能最大件数（' + meshNumberMaxCount + '件）に達しました。');
            }
        }

        tertiaryMeshLayer.setStyle(geoStyle1);
    }
}

function createMeshLayer() {

    if (tertiaryMeshLayer) {
        return;
    }

    var tertiaryMeshDataPath = '../workspace/TertiaryMesh.geojson';

    const xhr = new XMLHttpRequest();
    var text;

    xhr.open('GET', tertiaryMeshDataPath, false); // GETでローカルファイルを開く
    xhr.onload = () => text = xhr.responseText;
    xhr.onerror = () => console.log("error!");
    xhr.send();

    var data = JSON.parse(text);

    var latMax = 0;
    var latMin = 90;
    var lngMax = 0;
    var lngMin = 180;

    var features = data.features;
    for (var f in features) {
        var coords = features[f].geometry.coordinates;

        for (var i = 0; i < 4; i++) {
            var lng = coords[0][i][0];
            var lat = coords[0][i][1];

            if (latMax < lat) {
                latMax = lat
            }
            if (lat < latMin) {
                latMin = lat;
            }

            if (lngMax < lng) {
                lngMax = lng
            }
            if (lng < lngMin) {
                lngMin = lng;
            }
        }
    }

    // 矩形範囲を指定
    var bounds = L.latLngBounds([[latMin, lngMin], [latMax, lngMax]]);

    // 矩形範囲に合わせて地図を表示
    map.fitBounds(bounds); 

    tertiaryMeshLayer = L.geoJson(data,
        {
            // onEachFeatureオプション
            onEachFeature: geoOnEachFeature
        });

    tertiaryMeshLayer.addTo(map);
    tertiaryMeshLayer.setStyle(geoStyle2);
}

function startMeshSelection() {

    // キャンセルされた時の為に複製を保持
    selectedMeshNumbersClone = structuredClone(selectedMeshNumberDict);

    if (tertiaryMeshLayer) {
        tertiaryMeshLayer.setStyle(geoStyle1);
        tertiaryMeshLayer.bringToFront();
    }

    isSimulationRangeEditing = true;
    updateBuildingLayer();
    addSimulationRangeTooltip();
    resetEditButtonEnable();
}

function endMeshSelection(isConfirm) {

    if (!isConfirm) {

        // キャンセルされた場合は複製から復元
        selectedMeshNumberDict = structuredClone(selectedMeshNumbersClone);
    }

    if (tertiaryMeshLayer) {
        tertiaryMeshLayer.setStyle(geoStyle2);
        tertiaryMeshLayer.bringToBack();
    }

    updateFirePoints();

    isSimulationRangeEditing = false;
    updateBuildingLayer();
    removeSimulationRangeTooltip();
    resetEditButtonEnable();

    return Object.keys(selectedMeshNumberDict);
}

function clearSelectedMeshNumberAll() {

    selectedMeshNumberDict = {};

    if (tertiaryMeshLayer) {

        tertiaryMeshLayer.removeFrom(map);
        tertiaryMeshLayer = null;
    }

    resetEditButtonEnable();
}