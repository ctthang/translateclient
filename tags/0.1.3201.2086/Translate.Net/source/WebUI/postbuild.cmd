@echo off
copy /Y /B ..\source\WebUI\*.css WebUI
copy /Y /B ..\source\WebUI\*.asax WebUI
copy /Y /B ..\source\WebUI\*.aspx WebUI
copy /Y /B ..\source\WebUI\*.js WebUI
copy /Y /B ..\source\WebUI\*.png WebUI
copy /Y /B Translate.WebUI.* WebUI\bin

