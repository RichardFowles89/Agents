$env:DOTNET_ROOT = "$env:USERPROFILE\dotnet10"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
Write-Host "DOTNET_ROOT=$env:DOTNET_ROOT"
dotnet --version
