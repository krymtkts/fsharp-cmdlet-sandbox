[CmdletBinding()]
param (
)

dotnet clean
dotnet publish

$raw = 1..1000000
Import-Module .\src\code-quote\bin\Release\*\publish\*.psd1 -Force
Measure-Command { $raw | Select-ObjectTest -Mode Lambda }
$raw | Select-ObjectTest -Mode Lambda | Measure-Object
Measure-Command { $raw | Select-ObjectTest -Mode CodeQuotation }
$raw | Select-ObjectTest -Mode CodeQuotation | Measure-Object
