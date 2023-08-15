[CmdletBinding()]
param (
    [Parameter()]
    [switch]
    $Publish
)

dotnet publish -c Release

$ModuleName = Get-Location | Split-Path -Leaf
$ModuleBase = "./publish/${ModuleName}/"
$Module = "${ModuleBase}${ModuleName}.psd1"

Write-Host "module name: $ModuleName, module path: $Module"

mkdir $ModuleName -Force
Remove-Item "${ModuleBase}*" -Recurse -Force
Copy-Item "./src/$ModuleName/bin/Release/*/publish/*" $ModuleBase -Force

Remove-Module $ModuleName -Force -ErrorAction SilentlyContinue
Import-Module $Module -Force

Test-PreReleaseModule
Get-Module 'prerelease-test-module' | Format-Table

if ($Publish) {
    $Params = @{
        Path = $Module
        ApiKey = (Get-Credential ApiKey -Message 'Enter your API key as the password')
        Repository = 'PSGallery'
        Verbose = $true
        # WhatIf = $true
    }
    Publish-PSResource @Params
}
