#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

Write-Host "Building and packing Grinspector..." -ForegroundColor Cyan
dotnet pack src/Grinspector/Grinspector.csproj -c Release -o nupkg

Write-Host "Package created successfully in nupkg/" -ForegroundColor Green
Get-ChildItem nupkg/*.nupkg | Format-Table Name, Length, LastWriteTime
