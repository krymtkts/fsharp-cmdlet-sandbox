[CmdletBinding()]
param (
    [Parameter()]
    [ValidateSet('PowerShellGet', 'PSResourceGet')]
    [ValidateNotNullOrEmpty()]
    [string]
    $Mode,
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
    $ApiKey = (Get-Credential ApiKey -Message 'Enter your API key as the password')
    $WhatIf = $true
    switch ($Mode) {
        'PowerShellGet' {
            $Params = @{
                Path = $Module | Split-Path -Parent
                Repository = 'PSGallery'
                Verbose = $true
                WhatIf = $WhatIf
            }
            Publish-Module @Params -NuGetApiKey $ApiKey
        }
        'PSResourceGet' {
            $Params = @{
                Path = $Module
                Repository = 'PSGallery'
                Verbose = $true
                WhatIf = $WhatIf
            }
            Publish-PSResource @Params -ApiKey $ApiKey
        }
        Default {
            throw 'invalid pass.'
        }
    }
}
