[CmdletBinding()]
param (
)

# dotnet add ./src/avalonia-funcui package Avalonia.Desktop
# dotnet add ./src/avalonia-funcui package Avalonia.Themes.Fluent
# dotnet add ./src/avalonia-funcui package Avalonia.FuncUI
# dotnet add ./src/avalonia-funcui package Avalonia.FuncUI.Elmish
Remove-Item ./src/avalonia-funcui/publish/* -Force -Recurse -ErrorAction Ignore
dotnet clean .\src\avalonia-funcui
dotnet clean .\src\avalonia-funcui -c Release
dotnet publish .\src\avalonia-funcui
# TODO: PowerShell cmdlet cannot resolve libSkiaSharp.dll automatically,
# TODO: dirty workaround 1 is add the path for libSkiaSharp.dll to PATH environment variable
# $env:PATH = "$(Resolve-Path './src/avalonia-funcui/publish/*/runtimes/win-x64/native');$env:PATH"
Import-Module ./src/avalonia-funcui/publish/*/*.psd1 -Force
# TODO: secondary invocation occurs following error:
# > Test-AvaloniaFuncUI: Setup was already called on one of AppBuilder instances
Test-AvaloniaFuncUI

