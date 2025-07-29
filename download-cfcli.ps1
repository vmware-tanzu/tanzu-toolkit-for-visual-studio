$cf_versions = @("8.14.1")

$targetPath = Join-Path -Path $PSScriptRoot -ChildPath "src/cf_cli"

if (-not (Test-Path $targetPath)) {
    New-Item -ItemType Directory -Path $targetPath -Force | Out-Null
    Write-Host "Created directory: $targetPath"
}

function Download-CfCli {
    param (
        [string]$version
    )

    $major = $version.Split('.')[0]
    $outputPath = Join-Path -Path $PSScriptRoot -ChildPath "src/cf_cli/cf$major.exe"

    # Skip download if the file already exists
    if (Test-Path $outputPath) {
        Write-Host ">>> cf CLI v$major already exists at $outputPath, skipping download."
        return
    }
    
    Write-Host ">>> Downloading cf CLI v$version"

    $tempDir = New-Item -ItemType Directory -Path ([System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.Guid]::NewGuid().ToString()))

    try {
        # Determine naming scheme
        if ($major -eq "7" -or $major -eq "8") {
            $fileName = "cf$major-cli_${version}_winx64.zip"
            $baseUrl = "https://github.com/cloudfoundry/cli/releases/download/v$version"
        }
        else {
            throw "Unsupported version format: $version"
        }

        $zipUrl = "$baseUrl/$fileName"
        $zipPath = Join-Path $tempDir $fileName

        # Validate existence before download
        Write-Host "Checking path before trying to download: $zipUrl"
        $response = Invoke-WebRequest -Uri $zipUrl -Method Head -UseBasicParsing -ErrorAction SilentlyContinue
        if (-not $response -or $response.StatusCode -ne 200) {
            throw "cf CLI v$version not found at expected URL: $zipUrl"
        }

        Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath

        $extractPath = Join-Path $tempDir "extract"
        Expand-Archive -Path $zipPath -DestinationPath $extractPath

        $cfExePath = Get-ChildItem -Path $extractPath -Recurse -Filter "cf.exe" | Select-Object -First 1

        if (-not $cfExePath) {
            throw "cf.exe not found in the extracted files for v$version."
        }

        Copy-Item -Path $cfExePath.FullName -Destination $outputPath -Force
        Write-Host ">>> cf CLI v$version saved to $outputPath"
    }
    finally {
        Remove-Item -Recurse -Force $tempDir
    }
}

foreach ($version in $cf_versions) {
    Download-CfCli -version $version
}
