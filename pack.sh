#!/bin/bash

set -e

echo "Building and packing Grinspector..."
dotnet pack src/Grinspector/Grinspector.csproj -c Release -o nupkg

echo "Package created successfully in nupkg/"
ls -lh nupkg/*.nupkg
