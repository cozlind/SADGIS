@Echo Off
For /d /r "" %%i in (*) do (Rd /q /s "%%i" 2>nul)
Del /q /a "*.*"
Del "%userprofile%\Desktop\SADGIS.Ink"
Pause