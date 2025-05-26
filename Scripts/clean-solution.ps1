# Remove todas as pastas bin e obj recursivamente a partir do diretório atual
Get-ChildItem -Path . -Include bin,obj -Recurse -Directory | ForEach-Object {
    Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Host "Pastas 'bin' e 'obj' removidas com sucesso." -ForegroundColor Green
