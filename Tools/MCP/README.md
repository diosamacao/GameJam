# Gamejam Unity MCP

- Unity package: `com.coplaydev.unity-mcp` v10.2.0 embedded in `Packages/com.coplaydev.unity-mcp` from the verified local cache; no GitHub download is required. All 716 copied files matched their source SHA-256 hashes.
- Python server: `mcpforunityserver==10.2.0`, launched through uvx.
- Codex server name: `unityGamejam`.
- MCP endpoint: `http://127.0.0.1:8088/mcp` (local machine only).
- Default target: `Gamejam`; use the exact `Gamejam@hash` from `mcpforunity://instances` when multiple Unity instances are registered.

The project editor script `Assets/GamejamMCP/Editor/GamejamMcpStartup.cs` starts a hidden local server if port 8088 is unused, and connects Unity after compilation. It retries up to three times per domain load. For a manual retry use `Tools > Gamejam MCP > Connect`. Logs are under `Logs/MCP`.

The original Codex `unityMCP` entry has not been modified. Its port 8080 was occupied by Steam during setup. This project uses the separate `unityGamejam` entry.

## Restarting

1. Open `D:\Projects\Gamejam` in Unity and wait for package import/compilation.
2. The MCP service connects automatically. Alternatively run `Tools/MCP/Start-MCP.ps1`, then the Unity Connect menu.
3. If a previously open Codex chat does not expose the new server, restart Codex/reload MCP connections and select `unityGamejam`.

## Scope and shutdown

MCP tools can inspect and change scenes, GameObjects, scripts, assets and play mode. No public listening interface, cloud account or API key is configured. Server telemetry is disabled in the launch environment. Unity MCP's transport settings are Unity Editor preferences shared on this Windows account; its selected local URL is 8088. Codex routing should select Gamejam explicitly.

The server is a local background process, not a Windows service or scheduled task. It can remain alive after Unity closes. Removing the startup C# file stops future project auto-starts; removing `unityGamejam` from Codex disconnects the client. To stop a running service, first identify and verify the process listening on 127.0.0.1:8088; do not terminate an arbitrary process by port alone.

References: https://github.com/CoplayDev/unity-mcp/tree/v10.2.0 and https://developers.openai.com/codex/mcp .
