using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MCPForUnity.Editor.Services;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GamejamMcpStartup
{
    const string Url = "http://127.0.0.1:8088";
    static bool busy;
    static double nextTry = EditorApplication.timeSinceStartup + 5;
    static int attempts;
    static GamejamMcpStartup() { EditorApplication.update += Tick; }
    static async void Tick()
    {
        if (busy || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextTry) return;
        if (MCPServiceLocator.Bridge.IsRunning) { EditorApplication.update -= Tick; return; }
        if (++attempts > 3) { EditorApplication.update -= Tick; return; }
        busy = true;
        try { await Connect(); }
        catch (Exception e) { Directory.CreateDirectory("Logs/MCP");File.WriteAllText("Logs/MCP/startup-error.txt",e.ToString());UnityEngine.Debug.LogWarning("Gamejam MCP: "+e.Message); }
        finally { busy=false;nextTry=EditorApplication.timeSinceStartup+20; }
    }
    [MenuItem("Tools/Gamejam MCP/Connect")]
    static async void ConnectMenu() { try { await Connect(); } catch(Exception e) { UnityEngine.Debug.LogException(e); } }
    static bool PortOpen()
    {
        try { using(var tcp=new TcpClient()) { return tcp.ConnectAsync("127.0.0.1",8088).Wait(300) && tcp.Connected; } }
        catch { return false; }
    }
    static async Task Connect()
    {
        Directory.CreateDirectory("Logs/MCP");
        var config=EditorConfigurationCache.Instance;
        config.SetUseHttpTransport(true);config.SetHttpTransportScope("local");config.SetHttpBaseUrl(Url);
        var uvx=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),".local","bin","uvx.exe");
        if (!File.Exists(uvx)) throw new FileNotFoundException("uvx not found",uvx);
        config.SetUvxPathOverride(uvx);
        if (!PortOpen())
        {
            var start=new ProcessStartInfo(uvx,"--from mcpforunityserver==10.2.0 mcp-for-unity --transport http --http-host 127.0.0.1 --http-port 8088 --default-instance Gamejam")
            { UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden, WorkingDirectory=Directory.GetCurrentDirectory() };
            start.EnvironmentVariables["UNITY_MCP_TELEMETRY_ENABLED"]="false";
            Process.Start(start);
        }
        for(int i=0;i<40 && !PortOpen();i++) await Task.Delay(500);
        bool connected=await MCPServiceLocator.Bridge.StartAsync();
        File.WriteAllText("Logs/MCP/connection.txt","URL: "+Url+"/mcp\nProject: "+Application.dataPath+"\nConnected: "+connected+"\nUTC: "+DateTime.UtcNow.ToString("O"));
        if(!connected) throw new InvalidOperationException("Unity MCP bridge did not connect; see Window > MCP for Unity.");
        UnityEngine.Debug.Log("Gamejam MCP connected: "+Url+"/mcp");
    }
}
