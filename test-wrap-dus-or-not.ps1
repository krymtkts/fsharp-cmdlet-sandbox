dotnet clean
dotnet publish
Import-Module .\src\wrap-dus-or-not\bin\Debug\*\publish\*.psd1

@(
    'Raw'
    'DUs'
    'Properties'
    'RawAndProperties'
    'DUsAndProperties'
) | ForEach-Object {
    1..1000000 | Out-ObjectWrappedDUs -Mode $_
    [GC]::Collect()
}