/** 編集コントロール */
var drawControl;

/** 編集レイヤ */
var editableLayers;

/** 出火点の作成可能最大件数 */
var firePointMaxCount = 200;

function initDrawSetting() {

    editableLayers = new L.FeatureGroup().addTo(map);

    // 作図用コントローラ
    drawControl = new L.Control.Draw({
        draw: {
            polyline: false,
            polygon: false,
            rectangle: false,
            circle: false,
            // 点の描画条件。
            marker: drawFirePointMarkerOptions(),
            circlemarker: false,
        },
        edit: {
            featureGroup: editableLayers,
            remove: false
        }
    });

    // コントロールテキスト調整
    // 追加
    L.drawLocal.draw.toolbar.actions.text = '中止';
    L.drawLocal.draw.toolbar.actions.title = '追加を中止する';

    // 追加（マーカー）
    L.drawLocal.draw.toolbar.buttons.marker = '出火点を追加';
    L.drawLocal.draw.handlers.marker.tooltip.start = '出火点の位置をクリックしてください';

    // 編集
    L.drawLocal.edit.toolbar.buttons.edit = '編集';
    L.drawLocal.edit.toolbar.actions.save.text = '保存';
    L.drawLocal.edit.toolbar.actions.save.title = '編集を確定する';
    L.drawLocal.edit.toolbar.actions.cancel.text = '中止';
    L.drawLocal.edit.toolbar.actions.cancel.title = '編集を中止する';
    L.drawLocal.edit.handlers.edit.tooltip.text = '出火点をドラッグで編集してください';
    L.drawLocal.edit.handlers.edit.tooltip.subtext = '編集を取り消す場合は中止をクリックしてください';

    map.addControl(drawControl);

    map.on(L.Draw.Event.DRAWSTART, function (e) {
        setFirePointEditingStatus(true);

        if (firePointMaxCount <= editableLayers.getLayers().length) {
            showWarningMessageBox('出火点を設定できません。\r\n最大件数（' + firePointMaxCount + '件）に達しています。');
            map.removeControl(drawControl);
            map.addControl(drawControl);
        }
    });

    map.on(L.Draw.Event.DRAWSTOP, function (e) {
        setFirePointEditingStatus(false);
    });

    map.on(L.Draw.Event.CREATED, function (e) {
        var layer = e.layer;

        if (e.layerType === 'marker') { 
            createdFirePointMaker(layer);

            if (firePointMaxCount <= editableLayers.getLayers().length) {
                showInformationMessageBox('出火点の設定可能最大件数（' + firePointMaxCount + '件）に達しました。');
            }
        }
    });

    map.on(L.Draw.Event.EDITSTART, function (e) {
        setFirePointEditingStatus(true);
    });

    map.on(L.Draw.Event.EDITSTOP, function (e) {
        updateFirePoints();
        setFirePointEditingStatus(false);
    });

    resetEditButtonEnable();
}

function drawFirePointMarkerOptions() {

    // 出火点マーカーアイコンを設定
    const firepointMarkerIcon = L.Icon.extend({
        options: {
            shadowUrl: null,
            iconAnchor: new L.Point(10, 15),
            iconSize: new L.Point(20, 20),
            iconUrl: 'Images/firepoint.png',
        }
    });

    return {
        icon: new firepointMarkerIcon(),
    };
}

function createdFirePointMaker(layer) {

    // 出火点の位置のチェック
    let result = checkFirePointLatLng(layer.getLatLng());

    if (result.Building) {

        // 出火点の総数から番号を取得
        result.No = editableLayers.getLayers().length + 1;

        // 出火点の位置に表示するマーカーを作成
        var labelMarker = L.marker(layer.getLatLng());
        labelMarker.no = result.No;
        labelMarker.setIcon(createFirePointIcon(result.No));

        // 編集レイヤに追加
        editableLayers.addLayer(labelMarker);

        // 描画機能のボタン制御
        resetEditButtonEnable();
    }

    chrome.webview.hostObjects.csProcess.AddFirePoint(JSON.stringify(result));
}

function updateFirePoints() {

    var results = [];

    for (let i = 0; i < editableLayers.getLayers().length; i++) {

        var tempLayer = editableLayers.getLayers()[i];

        // 出火点の位置のチェック
        let result = checkFirePointLatLng(tempLayer.getLatLng());
        result.No = tempLayer.no;
        results.push(result);
    }

    chrome.webview.hostObjects.csProcess.UpdateFirePoints(JSON.stringify(results));
}

function createFirePointIcon(no) {

    return L.divIcon({
        html: '<div>'
            + '<img src="Images/firepoint.png" width="20" height="20"/>'
            + '<div style="color:red;font-size:20px;margin-top:-10px;" width="20" align="right">' + no + '</div>'
            + '</div>',
        className: 'fire-point-icon',
        iconAnchor: [10, 15],
        iconSize: [20, 20],
    });
}

function checkFirePointLatLng(latlng) {

    var targetMeshNum;
    for (var meshNum of Object.keys(selectedMeshNumberDict)) {

        var meshFeature = selectedMeshNumberDict[meshNum];
        const meshPolygonBounds = L.geoJson(meshFeature).getBounds();

        if (meshPolygonBounds.contains(latlng)) {

            targetMeshNum = meshNum;
            break;
        }
    }

    if (!targetMeshNum) {

        return {
            // 番号（ここでは設定しない）
            //No: no,

            // 出火点の位置
            PointCoordinate: { Lat: latlng.lat, Lon: latlng.lng },

            // 出火点がシミュレーション範囲内にあるかどうか
            IsPointInSimulationRange: false,

            // 出火点が含まれている建物
            Building: null
        }
    }

    var buildingDataPath = 'file:///' + baseDirPath + '/bldg_geojson/Building_' + targetMeshNum + '.geojson';

    const xhr = new XMLHttpRequest();
    var text;

    xhr.open('GET', buildingDataPath, false); // GETでローカルファイルを開く
    xhr.onload = () => text = xhr.responseText;
    xhr.onerror = () => console.log("error!");
    xhr.send();

    var geojsonData = JSON.parse(text);

    let building = null;

    for (var buildingFeature of geojsonData.features) {

        if (buildingFeature.geometry.type === 'MultiPolygon') {

            // 高速化の為、まずは境界矩形で判定
            const buildingPolygonBounds = L.geoJson(buildingFeature).getBounds();

            if (!buildingPolygonBounds.contains(latlng)) {
                continue;
            }

            // その後、ポリゴンで判定
            const pointFeature = turf.point([latlng.lng, latlng.lat]);
            const polygonFeature = turf.multiPolygon(buildingFeature.geometry.coordinates);

            if (turf.booleanPointInPolygon(pointFeature, polygonFeature)) {

                building = {
                    BldgId: buildingFeature.properties.bldgId,
                    Structure: buildingFeature.properties.fireproofStructureType,
                    Story: buildingFeature.properties.storeysAboveGround,
                };
                break;
            }
        }
        else {
            alert('feature.geometry.type = ' + buildingFeature.geometry.type)
        }
    }

    return {
        // 番号（ここでは設定しない）
        //No: no,

        // 出火点の位置
        PointCoordinate: { Lat: latlng.lat, Lon: latlng.lng },

        // 出火点がシミュレーション範囲内にあるかどうか
        IsPointInSimulationRange: true,

        // 出火点が含まれている建物
        Building: building
    }
}

function deleteFirePoint(deleteNumber) {

    // 番号から対象を確認
    var deleteLayer;
    for (let i = 0; i < editableLayers.getLayers().length; i++) {

        var tempLayer = editableLayers.getLayers()[i];

        if (tempLayer.no === deleteNumber) {
            deleteLayer = tempLayer;
            break;
        }
    }

    // 対象を削除
    if (deleteLayer) {
        editableLayers.removeLayer(deleteLayer);
    }

    // 番号の振り直し
    for (let i = 0; i < editableLayers.getLayers().length; i++) {

        var tempLayer = editableLayers.getLayers()[i];

        tempLayer.no = i + 1;
        tempLayer.setIcon(createFirePointIcon(i + 1));
    }

    resetEditButtonEnable();
}

function deleteFirePoints(deleteNumbers) {

    // 番号から対象を確認
    var deleteLayers = [];
    for (let i = 0; i < editableLayers.getLayers().length; i++) {

        var tempLayer = editableLayers.getLayers()[i];

        if (deleteNumbers.includes(tempLayer.no)) {
            deleteLayers.push(tempLayer);
        }
    }

    // 対象を削除
    for (var deleteLayer of deleteLayers) {
        editableLayers.removeLayer(deleteLayer);
    }

    // 番号の振り直し
    for (let i = 0; i < editableLayers.getLayers().length; i++) {

        var tempLayer = editableLayers.getLayers()[i];

        tempLayer.no = i + 1;
        tempLayer.setIcon(createFirePointIcon(i + 1));
    }

    resetEditButtonEnable();
}

function clearFirePointAll() {

    var deleteLayers = [];
    for (let i = 0; i < editableLayers.getLayers().length; i++) {

        deleteLayers.push(editableLayers.getLayers()[i]);
    }

    for (var deleteLayer of deleteLayers) {
        editableLayers.removeLayer(deleteLayer);
    }
}

function resetEditButtonEnable() {

    var markerbutton = document.getElementsByClassName("leaflet-draw-draw-marker")[0];
    var editbutton = document.getElementsByClassName("leaflet-draw-edit-edit")[0];

    // disabledクラス
    var drawDisabledClassName = 'disabled-draw-control';

    var isEnableMarker = true;
    var isEnableEdit = true;

    if (isSimulationRangeEditing || isSimulationRunning || isSimulationCompleated) {

        // ・シミュレーション範囲編集中
        // ・シミュレーション実行中
        // ・シミュレーション完了済
        // は編集機能は使用不可
        isEnableMarker = false;
        isEnableEdit = false;

    } else if (Object.keys(selectedMeshNumberDict).length === 0) {

        // シミュレーション範囲が0件の場合
        isEnableMarker = false;
        isEnableEdit = false;

    } else if (editableLayers.getLayers().length === 0) {

        // 編集対象が0件の場合
        isEnableEdit = false;
    }

    if (isEnableMarker) {
        if (markerbutton.classList.contains(drawDisabledClassName)) {
            markerbutton.classList.remove(drawDisabledClassName);
        }
    } else {
        if (!markerbutton.classList.contains(drawDisabledClassName)) {
            markerbutton.classList.add(drawDisabledClassName);
        }
    }

    if (isEnableEdit) {
        if (editbutton.classList.contains(drawDisabledClassName)) {
            editbutton.classList.remove(drawDisabledClassName);
        }
    } else {
        if (!editbutton.classList.contains(drawDisabledClassName)) {
            editbutton.classList.add(drawDisabledClassName);
        }
    }
}

function setFirePointEditingStatus(isEditing) {

    chrome.webview.hostObjects.csProcess.SetFirePointEditingStatus(JSON.stringify(isEditing));
}