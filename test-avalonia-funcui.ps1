[CmdletBinding()]
param (
)

dotnet publish .\src\avalonia-funcui
Import-Module .\src\avalonia-funcui\bin\Release\*\publish\*.psd1 -Force
Test-AvaloniaFuncUI
