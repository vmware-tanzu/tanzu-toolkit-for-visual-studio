# Thanks to Gérald Barré for the template of this script (source: https://www.meziantou.net/ci-cd-pipeline-for-a-visual-studio-extension-vsix-using-azure-devops.htm)

$version = $args[0] -replace "-build", ""

Write-Host "Setting version in manifest files to $version"

$VS2019Path = Resolve-Path $PSScriptRoot\vs2019\source.extension.vsixmanifest
Write-Host "Updating Visual Studio 2019 manifest at $VS2019Path"
[xml]$content = Get-Content $VS2019Path
$content.PackageManifest.Metadata.Identity.Version = $version
$content.Save($VS2019Path)

$VS2022Path = Resolve-Path $PSScriptRoot\vs2022\source.extension.vsixmanifest
Write-Host "Updating Visual Studio 2022 manifest at $VS2022Path"
[xml]$content = Get-Content $VS2022Path
$content.PackageManifest.Metadata.Identity.Version = $version
$content.Save($VS2022Path)
