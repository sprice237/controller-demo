#!/bin/bash
SERVICE_NAME="ControllerDemo.Services.LogTransportService"
BASEDIR=$(dirname "$BASH_SOURCE")
cd $BASEDIR

systemctl stop $SERVICE_NAME
systemctl disable $SERVICE_NAME
rm -f /etc/systemd/system/$SERVICE_NAME.service
