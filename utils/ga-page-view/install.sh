#!/bin/bash

dotnet publish -c Release

mkdir /usr/local/ga-page-view

cp ./ga-page-view/bin/Release/netcoreapp2.0/publish/* /usr/local/ga-page-view
cp ./ga-page-view.service /lib/systemd/system/

systemctl daemon-reload
systemctl enable ga-page-view
systemctl start ga-page-view