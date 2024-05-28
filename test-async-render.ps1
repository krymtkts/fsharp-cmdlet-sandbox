[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [ValidateSet('Easy', 'Hard')]
    [string]
    $Mode = 'Easy'
)

dotnet publish
Import-Module .\src\async-render\bin\Debug\net7.0\publish\async-render.psd1 -Force
. .\scripts\Invoke-InfiniteLoop.ps1

switch ($Mode) {
    'Easy' {
        Invoke-InfiniteLoop -ScriptBlock { $global:a += 1; $a; Start-Sleep -Seconds 1 } | Out-GridAsync
    }
    'Hard' {
        Invoke-InfiniteLoop -ScriptBlock { $global:a += 1; $a; Start-Sleep -Milliseconds 10 } | Out-GridAsync -Silent
    }
    default {
        throw "Invalid mode: $Mode"
    }
}
