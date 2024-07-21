[CmdletBinding()]
param (
)

dotnet publish
Import-Module .\src\fast-search\bin\Release\net8.0\publish\fast-search.psd1 -Force
# 1..1000000 | ForEach-Object { [pscustomobject]@{Name = $_; Value = $_ } } | Find-ObjectWithPattern