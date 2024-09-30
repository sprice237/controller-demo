#!/bin/bash
SERVICE_NAME="ControllerDemo.Services.LogTransportService"
BASEDIR=$(dirname "$BASH_SOURCE")
cd $BASEDIR

mkdir -p /var/opt/demo/$SERVICE_NAME
chown -R demo:demo /var/opt/demo/$SERVICE_NAME
chmod 0770 /var/opt/demo/$SERVICE_NAME

rabbitmqctl set_policy "log-entries" "log-entries" '{"message-ttl":86400000, "queue-mode":"lazy"}' --apply-to queues

cp $SERVICE_NAME.service /etc/systemd/system
systemctl daemon-reload
systemctl enable $SERVICE_NAME
systemctl start $SERVICE_NAME
