[CmdletBinding()]
param (
    [Parameter()]
    [switch]
    $RunTest,
    [Parameter()]
    [switch]
    $PSCustomObjectTest,
    [Parameter()]
    [switch]
    $HashtableTest,
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
        'Properties2'
        'RawAndProperties'
        'DUsAndProperties'
        'RawAndProperties2'
        'DUsAndProperties2'
        'RawAndProperties3'
        'DUsAndProperties3'
        'DUsAndThroughProperties'
        'DUsAndThroughProperties2'
    ) | ForEach-Object {
        Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
        1..1000000 | Out-ObjectWrappedDUs -Mode $_
        [GC]::Collect()
        if ($PSCustomObjectTest) {
            1..1000000 | ForEach-Object { [pscustomobject]@{Name = $_; Value = $_ } } | Out-ObjectWrappedDUs -Mode $_
            [GC]::Collect()
        }
        if ($HashtableTest) {
            1..1000000 | ForEach-Object { @{Name = $_; Value = $_ } } | Out-ObjectWrappedDUs -Mode $_
            [GC]::Collect()
        }
        Remove-Module wrap-dus-or-not -Force
        [GC]::Collect()
    }
}
if ($Types) {
    Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
    1..1000000 | Out-ObjectWrappedDUs -Mode Types
    [GC]::Collect()
    if ($PSCustomObjectTest) {
        1..1000000 | ForEach-Object { [PSCustomObject]@{ Value = $_ } } | Out-ObjectWrappedDUs -Mode Types
        [GC]::Collect()
    }
    if ($HashtableTest) {
        1..1000000 | ForEach-Object { @{ Value = $_ } } | Out-ObjectWrappedDUs -Mode Types
        [GC]::Collect()
    }
    Remove-Module wrap-dus-or-not -Force
    [GC]::Collect()
}

