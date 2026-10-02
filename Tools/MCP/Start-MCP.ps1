$ErrorActionPreference='Stop'
$uvx=Join-Path ([Environment]::GetFolderPath('UserProfile')) '.local/bin/uvx.exe'
$argsList=@('--from','mcpforunityserver==10.2.0','mcp-for-unity','--transport','http','--http-host','127.0.0.1','--http-port','8088','--default-instance','Gamejam')
$tcp=[System.Net.Sockets.TcpClient]::new()
try {$open=$tcp.ConnectAsync('127.0.0.1',8088).Wait(500) -and $tcp.Connected} catch {$open=$false} finally {$tcp.Dispose()}
if($open){Write-Output 'Port 8088 is already listening. No additional server was started.';exit}
$env:UNITY_MCP_TELEMETRY_ENABLED='false'
Start-Process -FilePath $uvx -ArgumentList $argsList -WindowStyle Hidden -WorkingDirectory (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) | Out-Null
Write-Output 'Starting Gamejam MCP at http://127.0.0.1:8088/mcp'
