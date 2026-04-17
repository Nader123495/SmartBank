Write-Host '==================================================='
Write-Host ' Configuration Automatique de SQL Server par lIA'
Write-Host '==================================================='
Write-Host ''

# Trouver l'instance SQLEXPRESS
$version = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL').SQLEXPRESS
$tcpKey  = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$version\MSSQLServer\SuperSocketNetLib\Tcp"
$ipAllKey = "$tcpKey\IPAll"

# Activer le protocole
Set-ItemProperty -Path $tcpKey -Name 'Enabled' -Value 1
Write-Host '[V] TCP/IP Actif'

# Forcer le port 1433
Set-ItemProperty -Path $ipAllKey -Name 'TcpDynamicPorts' -Value ''
Set-ItemProperty -Path $ipAllKey -Name 'TcpPort' -Value '1433'
Write-Host '[V] Port 1433 configuré'

# Redémarrer MSSQL Server
Write-Host '[+] Redémarrage du serveur SQL (cela prend environ 10 secondes)...'
Restart-Service 'MSSQL$SQLEXPRESS' -Force
Write-Host '[V] Serveur Redémarré.'

Write-Host ''
Write-Host '==================================================='
Write-Host ' TOUT EST PRET ! VOUS POUVEZ FERMER CETTE FENETRE '
Write-Host '==================================================='
Start-Sleep -Seconds 10
