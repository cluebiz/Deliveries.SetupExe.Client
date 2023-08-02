ECHO OFF

SET srcName=%~n1
SET link=%~dp0%srcName%

mklink /D %link% %~1

REM set /p id="anykey "