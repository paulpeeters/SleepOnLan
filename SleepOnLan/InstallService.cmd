@echo off
set serviceName=SleepOnLan
set displayName=SleepOnLan
set serviceDescription=This service will wait for a WOL packet (standard UDP port 9) that contains the PC's reversed MAC address andput the PC in sleep mode
set binPath=%CD%\SleepOnLan.exe
if "%1"=="--install" goto install
if "%1"=="--uninstall" goto uninstall
if "%1"=="--restart" goto restart
if "%1"=="--start" goto start
if "%1"=="--stop" goto stop
if "%1"=="--status" goto status
goto help

:help
echo Usage: InstallService { --install ^| --uninstall ^| --restart ^| --start ^| --stop ^| --status }
goto end

:install
sc query type= service | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if not ERRORLEVEL 1 (sc stop %serviceName%) >NUL
sc query state= all type= service | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if not ERRORLEVEL 1 (sc delete %serviceName%) >NUL
sc create %serviceName% start= auto binPath= %binPath% DisplayName= "%DisplayName%" >NUL
sc description %serviceName% "%serviceDescription%"  >NUL
sc start %serviceName% >NUL
echo Service %serviceName% created and started
sc query %serviceName%
goto end

:uninstall
sc query type= service state= all | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if ERRORLEVEL 1 goto not_installed
sc query type= service | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if not ERRORLEVEL 1 (sc stop %serviceName%)  >NUL
sc query type= service state= all | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if not ERRORLEVEL 1 (sc delete %serviceName%) >NUL
echo Service %serviceName% stopped and removed
goto end

:restart
sc query type= service state= all | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if ERRORLEVEL 1 goto not_installed
sc query type= service | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if not ERRORLEVEL 1 (sc stop %serviceName%) >NUL
sc start %serviceName% >NUL
echo Service %serviceName% restarted
sc query %serviceName%
goto end

:start
sc query type= service state= all | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if ERRORLEVEL 1 goto not_installed
sc query type= service | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if ERRORLEVEL 1 (sc start %serviceName%) >NUL
echo Service %serviceName% started
sc query %serviceName%
goto end

:stop
sc query type= service state= all | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if ERRORLEVEL 1 goto not_installed
sc query type= service | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if not ERRORLEVEL 1 (sc stop %serviceName%) >NUL
echo Service %serviceName% stopped
sc query %serviceName%
goto end

:status
sc query type= service state= all | findstr /C:"SERVICE_NAME: %serviceName%" >NUL
if ERRORLEVEL 1 goto not_installed
sc query %serviceName%
goto end

:not_installed
echo Service %serviceName% not installed
goto end

:end
