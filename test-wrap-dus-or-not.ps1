dotnet clean
dotnet publish

@(
    'Raw'
    'DUs'
    'Properties'
    'RawAndProperties'
    'DUsAndProperties'
    'Properties2'
    'RawAndProperties2'
    'DUsAndProperties2'
) | ForEach-Object {
    Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1 -Force
    1..1000000 | Out-ObjectWrappedDUs -Mode $_
    [GC]::Collect()
    Remove-Module wrap-dus-or-not -Force
}