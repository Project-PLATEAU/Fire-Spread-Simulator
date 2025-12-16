/** ベースフォルダパス */
var baseDirPath;

/** シミュレーション実行中かどうか */
var isSimulationRunning;

/** シミュレーション完了済かどうか */
var isSimulationCompleated;

function setBaseDataFolderPath(path){

    baseDirPath = path;

    clearFirePointAll();
    clearSelectedMeshNumberAll();
    updateBuildingLayer();

    createMeshLayer();
}

function setSimulationRunningStatus(isRunning) {

    isSimulationRunning = isRunning;

    resetEditButtonEnable();
}

function setSimulationCompleatedStatus(isCompleated) {

    isSimulationCompleated = isCompleated;

    resetEditButtonEnable();
}

function showInformationMessageBox(message) {

    chrome.webview.hostObjects.csProcess.ShowInformationMessageBox(JSON.stringify(message));
}

function showWarningMessageBox(message) {

    chrome.webview.hostObjects.csProcess.ShowWarningMessageBox(JSON.stringify(message));
}

function initMapLegend(map) {
    var legend = L.control({ position: 'bottomleft' });

    legend.onAdd = function (map) {
        var div = L.DomUtil.create('div', 'legend');
        div.innerHTML += '<div>建物データ凡例</div>'
            + '<table>'
            + '<tbody>'
            + '<tr><td><i style="background: #1D1DFF"/></td><td>1 耐火造</td></tr>'
            + '<tr><td><i style="background: #AFAFFF"/></td><td>2 準耐火造</td></tr>'
            + '<tr><td><i style="background: #FFFFAF"/></td><td>3 防火造</td></tr>'
            + '<tr><td><i style="background: #FFFFFF"/></td><td>4 準防火造</td></tr>'
            + '<tr><td><i style="background: #FFFFFF"/></td><td>5 裸木造</td></tr>'
            + '<tr><td><i style="background: #FFFFFF"/></td><td>6 その他</td></tr>'
            + '</tbody>'
            + '</table>';

        return div;
    };

    // 凡例パネル
    var legendVisible = false;

    L.easyButton('<img src="images/legend_icon.svg" style="margin-top: 3px; height: 20px; width:20px;">', function (btn, map) {
        if (legendVisible) {
            map.removeControl(legend);
            legendVisible = false;
        } else {
            legend.addTo(map);
            legendVisible = true;
        }
    }, '凡例を表示').addTo(map);
}
