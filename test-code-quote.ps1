[CmdletBinding()]
param (
)

dotnet clean
dotnet publish

$raw = 1..1000000
Import-Module .\src\code-quote\bin\Release\*\publish\*.psd1 -Force
'And', 'Or' | ForEach-Object {
    $Params = @{
        Operator = $_
    }
    $raw | Select-ObjectTest @Params -Mode Lambda | Measure-Object
    $raw | Select-ObjectTest @Params -Mode CodeQuotation | Measure-Object
    Measure-Command { $raw | Select-ObjectTest @Params -Mode Lambda }
    Measure-Command { $raw | Select-ObjectTest @Params -Mode CodeQuotation }
}
