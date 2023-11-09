#!/bin/bash

if [ "$EUID" -ne 0 ]
then 
    echo "Please run as root"
    exit 1
fi

from=.
serviceName=SleepOnLan
serviceFolder=/Library/LaunchDaemons
servicePlist=SleepOnLan.plist
serviceLabel=be.pware.sleeponlan
to=/opt/SleepOnLan

serviceExists=false
if [[ $(launchctl list | grep "$serviceLabel" | wc -l) -eq 1 ]]
then
	serviceExists=true
fi

serviceIsRunning=false
if [[ "$serviceExists" == true ]]
then
	if [[ $(launchctl list $serviceLabel | grep "PID" | wc -l) -eq 1 ]]
	then
		serviceIsRunning=true
	fi
fi

case "${1}" in 
   install)
      if [[ "$serviceExists" == true ]]
      then
		 launchctl stop $serviceLabel
		 launchctl unload $serviceFolder/$servicePlist
      fi
      if [ -e $serviceFolder/$servicePlist ]; then
         rm -f $serviceFolder/$servicePlist
      fi
      mkdir -p $to
      mkdir -p $to/Logs
      mkdir -p $to/config
      cp $from/SleepOnLan $to/
      cp $from/config/{appsettings.json,logging.json} $to/config
      cp $from/$servicePlist $serviceFolder/$servicePlist
      chmod +x $to/SleepOnLan
	  launchctl load $serviceFolder/$servicePlist
	  launchctl start $serviceLabel
	  echo Service $serviceName has been installed and started
      ;;
   uninstall)
      if [[ "$serviceExists" == true ]]
      then
		 launchctl stop $serviceLabel
		 launchctl unload $serviceFolder/$servicePlist
      fi
      rm -f $serviceFolder/$servicePlist
	  if [ -d "$to/Logs" ]; then
		  rm -f $to/Logs/*
		  rmdir $to/Logs
	  fi
	  if [ -d "$to/config" ]; then
         rm -f $to/config/*
         rmdir $to/config
      fi
	  if [ -d "$to" ]; then
         rm -f $to/*
         rmdir $to
      fi
	  echo Service $serviceName has been uninstalled and stopped
      ;;
   start)
      if [[ "$serviceExists" == true ]]
      then
		 if [[ "$serviceIsRunning" == false ]]
		 then
			launchctl start $serviceLabel
			echo Service $serviceName has been started
		 else
			echo Service $serviceName is already started
		 fi
      else
         echo Service $serviceName does not exist
      fi
      ;;
   stop)
      if [[ "$serviceExists" == true ]]
      then
		 if [[ "$serviceIsRunning" == true ]]
		 then
			launchctl stop $serviceLabel
			echo Service $serviceName has been stopped
		 else
			echo Service $serviceName is already stopped
		 fi
      else
         echo Service $serviceName does not exist
      fi
      ;;
   restart)
      if [[ "$serviceExists" == true ]]
      then
		 if [[ "$serviceIsRunning" == false ]]
		 then
			launchctl stop $serviceLabel
		 fi
         launchctl start $serviceLabel
         echo Service $serviceName has been restarted
      else
         echo Service $serviceName does not exist
      fi
      ;;
   status)
      if [[ "$serviceExists" == true ]]
      then
         launchctl list $serviceLabel
      else
         echo Service $serviceName does not exist
      fi
      ;;
   *)
      echo "Usage: $0 {install|uninstall|start|stop|status|restart}"
esac

exit 0