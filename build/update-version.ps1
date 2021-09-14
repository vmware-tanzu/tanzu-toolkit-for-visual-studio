# Thanks to Gérald Barré for the template of this script (source: https://www.meziantou.net/ci-cd-pipeline-for-a-visual-studio-extension-vsix-using-azure-devops.htm)

$version = $args[0] -replace "-build", ""
Write-Host "Set version: $version"

$FullPath = Resolve-Path $PSScriptRoot\..\src\VisualStudioExtension\src\source.extension.vsixmanifest
Write-Host $FullPath
[xml]$content = Get-Content $FullPath
$content.PackageManifest.Metadata.Identity.Version = $version
$content.Save($FullPath)
