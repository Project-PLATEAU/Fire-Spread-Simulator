# ソースファイルのビルド手順

# 1 本書について

本書では、火災延焼シミュレーションシステム（以下「本システム」という。）の開発環境構築手順について記載しています。本システムの構成や仕様の詳細については以下も参考にしてください。



> [!NOTE]
> [技術検証レポート（URL要更新）](https://www.mlit.go.jp/plateau/file/libraries/doc/plateau_tech_doc_0030_ver01.pdf)

> [!IMPORTANT]
> 本システムを実行するには「市街地火災シミュレーションプログラム」のエンジンが必要です。エンジンは、現時点では開発中のため一般公開されていません。詳細は、[GitHub火災延焼シミュレーションシステムページ](https://github.com/Project-PLATEAU/Fire-Spread-Simulator)の「3. 利用手順」をご確認ください。



# 2 開発環境

本システムはMicrosoft Visual Studio 2022 を統合開発環境（IDE）として使用して開発しました。以下に主な開発環境の構成を示します。


| 項目 | 内容 |
| ---- | ---- |
| 統合開発環境（IDE） | Microsoft Visual Studio Professional 2022<br>(64 ビット Version 17.14.4) |
| プログラミング言語 | C# 12 |
| ターゲットフレームワーク |.NET 8 |
| パッケージ管理 | NuGet による外部ライブラリの導入と管理 |
| デバッグ、ビルド| VisualStudio の標準機能を使用 |
| リリース用ビルド| VisualStudio の発行コマンドの実行、及び外部ツールの同梱をバッチで実行 |


※本システムで利用している「シミュレーション用データ変換ツール」は、Python3（python 3.9.18）／Anaconda（conda 23.5.2）環境で開発が行われています。


     


# 3 火災延焼シミュレーションシステムのビルド手順

## 3.1ダウンロード・展開

1.	[こちら](https://github.com/Project-PLATEAU/Fire-Spread-Simulator/Src/)
に格納されているソースコード一式を任意のフォルダに展開します。

    ※以降の手順のパスは、すべてこのフォルダを起点（ルート）とした相対パスで記載します。

2.	国総研より配布を受けたエンジン「simFireMP64.exe」を以下の2か所に配置します。

 - \ExternalTools\SimFire_ForSourceFileCreator
 - \ExternalTools\SimFire_ForSupportTool



## 3.2ファイル作成ツール・条件設定支援ツール・GISデータ変換ツールのビルド

> [!NOTE]
> - リリース用ビルド（実行ファイルの発行）は、「ReleasePackage」フォルダに格納されているバッチを実行することにより、行います
> - 実行ファイルの発行は VisualStudio の発行コマンドを使用しているため、「2 開発環境」に示すVisualStudio でソリューションのビルドができることが前提条件となります
> - フォルダ構成はアプリケーションで使用するための構成となっており、変更すると動作しなくなる可能性があります


### 1.GISデータ変換ツールの実行ファイルの発行
以下のバッチファイルを実行します。

```Batch
\ReleasePackage\SimulationResultFileConverter_publish.bat
```
**バッチの処理内容**
- GISデータ変換ツールの実行ファイル（SimulationResultFileConverter.exe）の発行

※GISデータ変換ツールは通常は単体では実行しないため使用しない想定です。条件設定支援ツールの実行ファイルの発行バッチで所定のフォルダに発行を行っています。



### 2.ファイル作成ツールの実行ファイルの発行バッチ
以下のバッチファイルを実行します。

```Batch
\ReleasePackage\SimulationSourceFileCreator_publish.bat
```
**バッチの処理内容**
- ファイル作成ツールの実行ファイル（SimulationSourceFileCreator.exe）の発行
- データ変換ツール（Python＋Anaconda）の「外部ツールのフォルダ」からの複製
- 延焼シミュレーションエンジン（ファイル作成ツール用）の「外部ツールのフォルダ」からの複製


### 3.条件設定支援ツールの実行ファイルの発行バッチ
以下のバッチファイルを実行します。

```Batch
\ReleasePackage\SimulationSupportTool_publish.bat
```
**バッチの処理内容**
- 条件設定支援ツールの実行ファイル（SimulationSupportTool.exe）の発行
- GISデータ変換ツールの実行ファイル（SimulationResultFileConverter.exe）の発行
- 延焼シミュレーションエンジン（条件設定支援ツール用）の「外部ツールのフォルダ」からの複製









## 3.3シミュレーション用データ変換ツールのビルド
※「ファイル作成ツール」は、国総研が開発した「シミュレーション用データ変換ツール」をビルド（exe化）して利用しています。


### 1.仮想環境の構築・外部ライブラリの導入

> [!NOTE]
>以下の「1.仮想環境(env_r5nilim) を構築」から「 4.	各ライブラリのバージョンを確認」までの記載は「シミュレーション用データ変換ツール」開発時の報告書（令和５年度市街地火災シミュレータ拡張３Ｄ都市モデルＬＯＤ２対応データ変換ツール作成業務　報告書）を元に作成されています。



以下のcondaコマンドで仮想環境を構築し、外部ライブラリを導入します。

1.	仮想環境(env_r5nilim) を構築
     ```Batch
    (base) conda create -n env_r5nilim python=3.9
     ```
2.	仮想環境(env_r5nilim) をアクティベート
    ```Batch
    (base) conda activate env_r5nilim
    ```

3.	仮想環境(env_r5nilim) に外部ライブラリを導入
    ```Batch
    (env_r5nilim) conda install gdal
    (env_r5nilim) conda install shapely
    (env_r5nilim) conda install shapely
    (env_r5nilim) conda install lxml
    (env_r5nilim) conda install trimesh -c conda-forge
    (env_r5nilim) conda install rtree
     ```

 4.	各ライブラリのバージョンを確認
    ```Batch
    (env_r5nilim) conda list
    ：
    gdal　3.6.2 py39h7670e6c_3 main
    ：
    shapely　2.0.1 py3 9hd7f5953_0
    ：
    lxml 4.9.2 py39h2bbff1b_0
    ：
    trimesh　4.1.3 pyhd8ed1ab_0 conda-forge
    ：
    rtree　1.0.1 py39h2eaa2aa_0 main
     ```


    5.追加のライブラリを導入<br>
    ※お使いの環境次第では、以下のライブラリの追加導入が必要な場合があります。

    ```Batch
    conda install -c conda-forge opencv
    conda install -c conda-forge mapbox_earcut
    ```


### 2.pyinstallerによるexeファイル作成


以下のcondaコマンドでexeファイルを作成します。


1.	pyinstallerのインストール
    ```Batch
    (env_r5nilim) > pip install pyinstaller
    ```

2.	pyinstallerのバージョン確認
     ```Batch
    (env_r5nilim) > pyinstaller --version
    6.15.0
     ```

3. Pythonファイル(plateau_conv.py)のあるフォルダへ移動
     ```Batch
    (env_r5nilim) > cd <プログラム配置フォルダパス>\createFile
     ```


4. pyinstallerコマンドでexeファイル作成
    ```Batch
    (env_r5nilim) > pyinstaller plateau_conv.py
    ```


     ※以下の通りdistフォルダにexe、カレントディレクトリにspecファイルが作成される

    ```Batch
    createFile（カレントディレクトリ）
     ├dist
     │　└plateau_conv
     │　　　├_internal
     │　　　└plateau_conv.exe
     └plateau_conv.spec
    
    ※作成されるフォルダ・ファイルのみ記載
    ```

5.  plateau_conv.spec の修正

    このままではエラー（「ModuleNotFoundError: No module named 'osgeo._gdal'」）となるので、 hiddenimportsに'osgeo._gdal'を追記


    ```Batch
    hiddenimports=['osgeo._gdal']
    ```

6. specファイルを使用してpyinstallerコマンドでexeを作成
    ```Batch
    (env_r5nilim) > pyinstaller plateau_conv.spec
    ```

7. exeと同じフォルダに「proj.db」を配置

    Pythonがインストールされていない環境では「proj.db」が必要となるので、仮想環境のライブラリからコピーして配置する

    コピー元："C:\Program Files\anaconda3\envs\env_r5nilim\Library\share\proj\proj.db"<br>※インストール先フォルダに合わせてパスを読み替える


8. cfgファイルの配置

    プログラムの実行に必要な「plateau_conv.cfg」を配置
    ```Batch
    createFile（カレントディレクトリ）
    ├dist
    │　└plateau_conv
    │　　　├_internal
    │　　　├cfg
    │　　　│　└plateau_conv.cfg
    │　　　├plateau_conv.exe
    │　　　└proj.db
    └plateau_conv.spec

    ※ここでは作成されるフォルダ・ファイル、および追加するフォルダ・ファイルのみ記載
    ```