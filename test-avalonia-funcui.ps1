[CmdletBinding()]
param (
)

# dotnet add ./src/avalonia-funcui package Avalonia.Desktop
# dotnet add ./src/avalonia-funcui package Avalonia.Themes.Fluent
# dotnet add ./src/avalonia-funcui package Avalonia.FuncUI
# dotnet add ./src/avalonia-funcui package Avalonia.FuncUI.Elmish
dotnet publish .\src\avalonia-funcui
Import-Module .\src\avalonia-funcui\bin\Release\*\publish\*.psd1 -Force
Test-AvaloniaFuncUI
