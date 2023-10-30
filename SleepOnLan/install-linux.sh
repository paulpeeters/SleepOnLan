#!/bin/bash

if [ "$EUID" -ne 0 ]
then 
    echo "Please run as root"
    exit 1
fi

service=SleepOnLan.service
from=.
to=/opt/SleepOnLan
serviceFolder=/lib/systemd/system

serviceExists=false
if [ $(systemctl list-unit-files "$service" | wc -l) -gt 3 ]
then
	serviceExists=true
fi

case "$1" in 
   install)
      if [ $serviceExists == true ]
      then
         systemctl stop $service
         systemctl disable $service
      fi
      systemctl daemon-reload
      if [ -e $serviceFolder/$service ]; then
         rm -f $serviceFolder/$service
      fi
      mkdir -p $to
      mkdir -p $to/Logs
      mkdir -p $to/config
      cp $from/SleepOnLan $to/
      cp $from/config/{appsettings.json,logging.json} $to/config
      cp $from/$service $serviceFolder/
      chmod +x $to/SleepOnLan
      systemctl enable $service
      systemctl daemon-reload
      systemctl start $service
      ;;
   uninstall)
      if [ $serviceExists == true ]
      then
         systemctl stop $service
         systemctl disable $service
      fi
      rm -f $serviceFolder/$service
      systemctl daemon-reload
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
      ;;
   start)
      if [ $serviceExists == true ]
      then
         systemctl start $service
         echo Service $service has been started
      else
         echo Service $service does not exist
      fi
      ;;
   stop)
      if [ $serviceExists == true ]
      then
         systemctl stop $service
         echo Service $service has been stopped
      else
         echo Service $service does not exist
      fi
      ;;
   restart)
      if [ $serviceExists == true ]
      then
         systemctl restart $service
         echo Service $service has been restarted
      else
         echo Service $service does not exist
      fi
      ;;
   status)
      if [ $serviceExists == true ]
      then
         systemctl status $service
      else
         echo Service $service does not exist
      fi
      ;;
   *)
      echo "Usage: $0 {install|uninstall|start|stop|status|restart}"
esac

exit 0