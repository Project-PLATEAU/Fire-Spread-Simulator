@echo off

cd /d %~dp0

set OUTPUTFOLDER=%~1

if [%OUTPUTFOLDER%] == [] (
  set OUTPUTFOLDER=.\SimulationResultFileConverter\
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
echo GIS変換ツールプロジェクトの発行開始
echo #######################################################################################################

dotnet publish "..\SimulationResultFileConverter\SimulationResultFileConverter.csproj" -c Release -r win-x64 -p:PublishSingleFile=true --self-contained true -o %OUTPUTFOLDER%

echo 完了しました。
pause
