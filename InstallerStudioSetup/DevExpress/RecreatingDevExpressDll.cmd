cls
set PATHWIX="c:\Program Files\WiX Toolset v3.9\bin"
set PATHPRG="d:\Tolchev\Work\2014-07-17 Инсталятор STv2\ASPOSetup\ASPOSetup v4.0\STInstaller"
rem Запускать только один раз, для создания DevExpressDll.wxs.
rem При добавлении и изменении корректировать вручную, так как
rem необходимо сохранять GUID.
rem %PATHWIX%\heat.exe dir %PATHPRG%\InstallerStudioSetup\DevExpress\Dll -o %PATHPRG%\InstallerStudioSetup\DevExpressDll.wxs -gg -sreg -scom -sfrag -srd -dr INSTALLFOLDER -cg DevExpressComponentGroup -var var.DevExpress

pause