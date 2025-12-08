@echo off

cd /d %~dp0

set OUTPUTFOLDER=%~1

if [%OUTPUTFOLDER%] == [] (
  set OUTPUTFOLDER=.\SimulationSourceFileCreator\
)

set ANSWER=Y

if exist %OUTPUTFOLDER% (
  set /p ANSWER="すでにフォルダ(%OUTPUTFOLDER%)が存在します。削除して続行してよろしいですか (Y/N)？"
)

if not %ANSWER:Y=Y%==Y (
  echo 終了します。
  pause
  exit /b
)

if exist %OUTPUTFOLDER% (
  rmdir /s /q %OUTPUTFOLDER%
)

echo.
echo #######################################################################################################
echo ファイル作成ツールプロジェクトの発行開始
echo #######################################################################################################

dotnet publish "..\SimulationSourceFileCreator\SimulationSourceFileCreator.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o %OUTPUTFOLDER%

echo.
echo #######################################################################################################
echo 外部ツール：データ変換ツール（Python＋Anaconda） を ExternalTools から GeneFile にコピーして配置
echo #######################################################################################################

REM コピー元とコピー先を設定
set SOURCE=..\ExternalTools\GeneFile
set DESTINATION=%OUTPUTFOLDER%GeneFile

REM フォルダ構造を保持してコピー
xcopy %SOURCE%\* %DESTINATION%\ /E /I /Y

echo.
echo #######################################################################################################
echo 外部ツール：シミュレーションエンジン を ExternalTools から SimFire にコピーして配置
echo #######################################################################################################

REM コピー元とコピー先を設定
set SOURCE=..\ExternalTools\SimFire_ForSourceFileCreator
set DESTINATION=%OUTPUTFOLDER%SimFire

REM フォルダ構造を保持してコピー
xcopy %SOURCE%\* %DESTINATION%\ /E /I /Y

echo 完了しました。
pause
