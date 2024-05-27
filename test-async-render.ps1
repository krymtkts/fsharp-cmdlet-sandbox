dotnet publish
Import-Module .\src\async-render\bin\Debug\net7.0\publish\async-render.psd1 -Force
. .\scripts\Invoke-InfiniteLoop.ps1
Invoke-InfiniteLoop -ScriptBlock { $global:a += 1; $a; Start-Sleep -Seconds 1 } | Out-GridAsync
