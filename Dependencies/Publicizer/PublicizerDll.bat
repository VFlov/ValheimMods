@echo off

for %%a in (Managed\*.dll) do (
  AssemblyPublicizer.exe "%%a"
)
setlocal enabledelayedexpansion

set "source_word=_publicized"
set "replace_word="

for %%a in (publicized_assemblies\*) do (
  set "file_name=%%~na"
  set "file_name=!file_name:%source_word%=%replace_word%!"
  ren "%%a" "!file_name!%%~xa"
)

endlocal
pause