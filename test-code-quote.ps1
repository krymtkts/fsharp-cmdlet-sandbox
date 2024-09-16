[CmdletBinding()]
param (
)

dotnet clean
dotnet publish

$raw = 1..1000000
Import-Module .\src\code-quote\bin\Release\*\publish\*.psd1 -Force
1..5 | ForEach-Object {
    $Size = $_
    'And', 'Or' | ForEach-Object {
        $Params = @{
            Operator = $_
            Size = $Size
        }
        Measure-Command { $raw | Select-ObjectTest @Params -Mode Lambda }
        # NOTE: Compiling Code Quotations takes more than 100ms, but generated expressions are generally faster.
        Measure-Command { $raw | Select-ObjectTest @Params -Mode CodeQuotation }
        # NOTE: Invoke consistency checks secondly to avoid suppressing the effect of the first run.
        $raw | Select-ObjectTest @Params -Mode Lambda | Measure-Object
        $raw | Select-ObjectTest @Params -Mode CodeQuotation | Measure-Object
    }
}
