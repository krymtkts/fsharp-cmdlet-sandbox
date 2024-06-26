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

$raw = 1..1000000
if ($PSCustomObjectTest) {
    $psc = $raw | ForEach-Object { [pscustomobject]@{Name = $_; Value = $_ } }
}
if ($HashtableTest) {
    $hst = $raw | ForEach-Object { @{Name = $_; Value = $_ } }
}
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
        Write-Host "Mode: $_ -----------------------------------------------------------------------"
        $Mode = $_
        Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
        Write-Host "Mode: $_ Raw -------------------------------------------------------------------"
        Measure-Command { $raw | Out-ObjectWrappedDUs -Mode $Mode }
        [GC]::Collect()
        if ($PSCustomObjectTest) {
            Write-Host "Mode: $_ PSC -------------------------------------------------------------------"
            Measure-Command { $psc | Out-ObjectWrappedDUs -Mode $Mode }
            [GC]::Collect()
        }
        if ($HashtableTest) {
            Write-Host "Mode: $_ HST -------------------------------------------------------------------"
            Measure-Command { $hst | Out-ObjectWrappedDUs -Mode $Mode }
            [GC]::Collect()
        }
        Remove-Module wrap-dus-or-not -Force
        [GC]::Collect()
    }
}
if ($Types) {
    Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
    Write-Host "Mode: $_ -----------------------------------------------------------------------"
    Measure-Command { $raw | Out-ObjectWrappedDUs -Mode Types }
    [GC]::Collect()
    if ($PSCustomObjectTest) {
        Write-Host "Mode: $_ PSC -------------------------------------------------------------------"
        Measure-Command { $psc | Out-ObjectWrappedDUs -Mode Types }
        [GC]::Collect()
    }
    if ($HashtableTest) {
        Write-Host "Mode: $_ HST -------------------------------------------------------------------"
        Measure-Command { $hst | Out-ObjectWrappedDUs -Mode Types }
        [GC]::Collect()
    }
    Remove-Module wrap-dus-or-not -Force
    [GC]::Collect()
}

