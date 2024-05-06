[CmdletBinding()]
param (
    [Parameter()]
    [switch]
    $RunTest,
    [Parameter()]
    [switch]
    $Types
)

dotnet clean
dotnet publish

if ($RunTest) {
    @(
        'Raw'
        'DUs'
        'Properties'
        'RawAndProperties'
        'DUsAndProperties'
        'Properties2'
        'RawAndProperties2'
        'DUsAndProperties2'
        'RawAndProperties3'
        'DUsAndProperties3'
        'DUsAndThroughProperties'
    ) | ForEach-Object {
        Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
        1..1000000 | Out-ObjectWrappedDUs -Mode $_
        [GC]::Collect()
        1..1000000 | ForEach-Object { [pscustomobject]@{Name = $_; Value = $_ } } | Out-ObjectWrappedDUs -Mode $_
        [GC]::Collect()
        Remove-Module wrap-dus-or-not -Force
    }
}
if ($Types) {
    Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
    1..1000000 | Out-ObjectWrappedDUs -Mode Types
    [GC]::Collect()
    1..1000000 | ForEach-Object { [PSCustomObject]@{
            Value = $_
        } } | Out-ObjectWrappedDUs -Mode Types
    [GC]::Collect()
    Remove-Module wrap-dus-or-not -Force
}

