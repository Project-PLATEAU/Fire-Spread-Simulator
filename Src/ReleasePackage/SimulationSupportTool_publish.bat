@echo off

cd /d %~dp0

set OUTPUTFOLDER=%~1

if [%OUTPUTFOLDER%] == [] (
  set OUTPUTFOLDER=.\SimulationSupportTool\
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
echo 条件設定支援ツールプロジェクトの発行開始
echo #######################################################################################################

dotnet publish "..\SimulationSupportTool\SimulationSupportTool.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o %OUTPUTFOLDER%

echo.
echo #######################################################################################################
echo 外部ツール：GIS変換ツールプロジェクト を ResultFileConv に発行
echo #######################################################################################################

dotnet publish "..\SimulationResultFileConverter\SimulationResultFileConverter.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o %OUTPUTFOLDER%ResultFileConv

echo.
echo #######################################################################################################
echo 外部ツール：延焼シミュレーションエンジン を ExternalTools から SimFire にコピーして配置
echo #######################################################################################################

REM コピー元とコピー先を設定
set SOURCE=..\ExternalTools\SimFire_ForSupportTool
set DESTINATION=%OUTPUTFOLDER%SimFire

REM フォルダ構造を保持してコピー
xcopy %SOURCE%\* %DESTINATION%\ /E /I /Y

echo 完了しました。
pause
