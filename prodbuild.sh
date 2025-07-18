#!/bin/bash
git fetch
git pull
npm run build
sudo cp -r dist/* /var/www/html
pkill -f ohs-sharedmouse
pkill -f dotnet
dotnet build ohs-sharedmouse-ws/ohs-sharedmouse-ws.csproj -c Release
ohs-sharedmouse-ws/bin/Release/net8.0/ohs-sharedmouse-ws &

