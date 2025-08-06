#!/usr/bin/env pwsh

set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$targetPath = Join-Path -Path $PSScriptRoot -ChildPath "vsdbg"
if (-not (Test-Path $targetPath)) {
    New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
    Write-Host "Created directory: $targetPath"
}

$scriptPath = Join-Path -Path $targetPath -ChildPath "getvsdbg.ps1"
if (-not(Test-path $scriptPath)) {
    Write-Host ">>> Downloading getvsdbg from Microsoft"
    Invoke-WebRequest -Uri "https://aka.ms/getvsdbgps1" -OutFile $scriptPath
}

function Download-vsdbg {
    param (
        [string]$RuntimeID
    )

    $outputPath = Join-Path -Path $targetPath -ChildPath $RuntimeID

    # Skip download if the file already exists
    if (Test-Path $outputPath) {
        Write-Host ">>> vsdbg for $RuntimeID already exists at $outputPath, skipping download."
        return
    }
    
    Write-Host ">>> Downloading vsdbg for $RuntimeID"

    & $scriptPath -Version vs2022 -RuntimeID $RuntimeID -InstallPath $outputPath
}

Download-vsdbg -RuntimeID "win7-x64"
Download-vsdbg -RuntimeID "linux-x64"
