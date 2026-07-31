#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd $SCRIPT_DIR

dotnet publish src/SharpDb/SharpDb.csproj \
  --self-contained \
  --configuration Release \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=false \
  -p:DebugType=None \
  --output ../bin
