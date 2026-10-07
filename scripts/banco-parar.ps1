# Para o PostgreSQL local de desenvolvimento.
$pg = 'C:\dev\pgsql'
& "$pg\bin\pg_ctl.exe" stop -D "$pg\data" -m fast
