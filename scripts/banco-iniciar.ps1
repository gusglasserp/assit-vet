# Inicia o PostgreSQL local de desenvolvimento (versão portátil em C:\dev\pgsql).
# Precisa rodar de novo depois de reiniciar o computador.
$pg = 'C:\dev\pgsql'
& "$pg\bin\pg_ctl.exe" status -D "$pg\data" *> $null
if ($LASTEXITCODE -eq 0) { Write-Host 'PostgreSQL já está rodando.'; exit 0 }
& "$pg\bin\pg_ctl.exe" start -D "$pg\data" -l "$pg\postgres.log" -w
