using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Awesomium.Core;
using Awesomium.Windows.Forms;
using DiscordRPC;
using Newtonsoft.Json;
using NLog;
using ADashboard.DBoardHelper;
namespace ADashboard
{
    public partial class DBoard : Form
    {
        private bool drag;
        private int hWnd;
        private Point start_point = new Point(0, 0);
        private static readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

        private readonly string currentDir = AppDomain.CurrentDomain.BaseDirectory;
        private string parentDir;
        private static readonly Logger log = LogManager.GetLogger("fileLogger");
        private static int discordPipe = -1;

        private WebControl webControl1;
        private Panel panel_top, CloseBTN, MinimizerBTN, SettingsBTN;
        private Label label_top_2;
        private NotifyIcon notifyIcon1;
        private WebControlContextMenu webControlContextMenu1;
        private ToolTip toolTip;
        private IContainer components;
        private Dictionary<int, int> processToClientIdMap = new Dictionary<int, int>();
        private int nextClientId = 0;
        private Dictionary<int, string> processCharacterMap = new Dictionary<int, string>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Dictionary<int, CharacterData> previousCharacterDataMap = new Dictionary<int, CharacterData>();

        private readonly string nonRRUrl = "http://nonrr.mythmu.net/introdash/";
        private string currentApiUrl;

        private List<CharacterData> characterDataList = new List<CharacterData>();

        public DBoard()
        {
            InitializeComponent();
            InitializeDashboard
            LoadServerType();
            InitializeDiscord();
            InitializeDashboard();
            MonitorClient();
            parentDir = Directory.GetParent(currentDir).FullName;
        }

        private void LoadServerType()
        {
            currentApiUrl = nonRRUrl;
            this.webControl1.Source = new Uri(currentApiUrl);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!WebCore.IsInitialized)
            {
                WebCore.Initialize(new WebConfig { LogLevel = Awesomium.Core.LogLevel.Verbose });
            }
        }

        private void InitializeDashboard()
        {
            WindowState = FormWindowState.Minimized;
            StartPosition = FormStartPosition.Manual;
            Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - Width - 50, Screen.PrimaryScreen.WorkingArea.Height - Height - 50);
            webControl1.LoadingFrameComplete += WebControl1_LoadingFrameComplete;
            string dashboardPath = Path.Combine(currentDir, "Dashboard.ini");
            if (!File.Exists(dashboardPath))
            {
                File.WriteAllText(dashboardPath, new string('1', 250));
            }
        }

        private void WebControl1_LoadingFrameComplete(object sender, FrameEventArgs e)
        {
            if (WebCore.IsInitialized)
            {
                try
                {
                    var aweObject = (JSObject)webControl1.CreateGlobalJavascriptObject("aweCustomObject");
                    aweObject.Bind(new JSFunctionHandler(exeFunctionAwesomium));
                    string dashboardConfig = File.ReadAllText(Path.Combine(currentDir, "Dashboard.ini"));
                    webControl1.ExecuteJavascript($"LoadEventAlerts('{dashboardConfig}', 'web')");
                    if (File.Exists(Path.Combine(parentDir, "opengl32.dll")))
                    {
                        webControl1.ExecuteJavascript("newEffectsClient()");
                    }
                    WindowState = FormWindowState.Normal;
                    LoadCharacterDataFromProcess();
                }
                catch (Exception ex)
                {
                    log.Error($"Error executing JavaScript: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("WebCore is not initialized. Please restart the application.");
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (WebCore.IsInitialized)
            {
                WebCore.Shutdown();
            }
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            base.OnFormClosed(e);
        }

        private void LoadCharacterDataFromProcess()
        {
            Task.Run(() =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var activeCharacterNames = new HashSet<string>();
                    var activeProcessIds = new HashSet<int>();

                    foreach (Process process in Process.GetProcessesByName("main"))
                    {
                        try
                        {
                            int processId = process.Id;
                            string mainWindowTitle = process.MainWindowTitle;
                            var characterData = ExtractCharacterData(mainWindowTitle, process);

                            if (!processToClientIdMap.ContainsKey(processId))
                            {
                                processToClientIdMap[processId] = nextClientId++;
                            }

                            characterData.clientId = processToClientIdMap[processId].ToString();
                            ReadCharacterStatsFromMemory(process, characterData);

                            activeCharacterNames.Add(characterData.name);
                            activeProcessIds.Add(processId);

                            UpdateCharacterData(characterData, processId);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error processing character data for process {process.Id}: {ex.Message}");
                        }
                    }

                    RemoveInactiveCharacters(activeCharacterNames, activeProcessIds);
                    Thread.Sleep(500);
                }
            }, cancellationTokenSource.Token);
        }

        private void RemoveInactiveCharacters(HashSet<string> activeCharacterNames, HashSet<int> activeProcessIds)
        {
            characterDataList.RemoveAll(c => !activeCharacterNames.Contains(c.name));
            foreach (var processId in processToClientIdMap.Keys.ToList())
            {
                if (!activeProcessIds.Contains(processId))
                {
                    processToClientIdMap.Remove(processId);
                    processCharacterMap.Remove(processId);
                }
            }
        }

        private CharacterData ExtractCharacterData(string mainWindowTitle, Process process)
        {
            var characterData = new CharacterData
            {
                name = ExtractCharacterName(mainWindowTitle),
                level = ExtractCharacterLevel(mainWindowTitle),
                masterLevel = ExtractCharacterMasterLevel(mainWindowTitle)
            };

            if (!ReadCharacterStatsFromMemory(process, characterData))
            {
                log.Error("Failed to read character stats from memory.");
            }

            return characterData;
        }

        private void UpdateCharacterData(CharacterData characterData, int processId)
        {
            log.Info($"Starting UpdateCharacterData for processId: {processId}, character name: {characterData.name}");

            if (characterData.serverIndex != "Non-RR")
            {
                log.Info($"Character {characterData.name} is not on Non-RR server (serverIndex: {characterData.serverIndex}), ignoring character.");
                return;
            }

            if (previousCharacterDataMap.TryGetValue(processId, out var previousData))
            {
                if (previousData.Equals(characterData))
                {
                    log.Info("No changes detected in character data. Skipping update.");
                    return;
                }
            }
            else
            {
                log.Info("No previous data found. Adding new character data.");
            }

            previousCharacterDataMap[processId] = characterData;

            var existingData = characterDataList.FirstOrDefault(c => c.clientId == characterData.clientId);
            if (existingData != null)
            {
                existingData.Update(characterData);
            }
            else
            {
                characterDataList.Add(characterData);
            }

            var rrCharacters = characterDataList
                .Where(c => !string.IsNullOrEmpty(c.name) && c.serverIndex == "Non-RR")
                .ToList();

            log.Info($"RR Characters selected to send: {JsonConvert.SerializeObject(rrCharacters)}");

            if (rrCharacters.Count > 0)
            {
                string jsonData = JsonConvert.SerializeObject(rrCharacters);
                string base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));

                try
                {
                    log.Info($"Base64 encoded data for RR server: {base64Data}");
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.Invoke((Action)(() =>
                        {
                            webControl1.ExecuteJavascript($"LoadCharacters('{base64Data}');");
                        }));
                        log.Info("Successfully sent RR character data to JavaScript.");
                    }
                    else
                    {
                        log.Warn("Control handle not created. Skipping Invoke call for RR server.");
                    }
                }
                catch (Exception ex)
                {
                    log.Error($"Error executing JavaScript for RR server: {ex.Message}");
                }
            }
            else
            {
                log.Warn("No RR characters to send. Exiting function.");
            }
        }

        private JSValue exeFunctionAwesomium(JSValue[] arguments)
        {
            int command = Convert.ToInt32(arguments[0].ToString());

            log.Info("Command received in exeFunctionAwesomium: " + command);

            try
            {
                switch (command)
                {
                    case 0:
                        DisplayBalloonTip(arguments);
                        break;

                    case 1:
                        Process.Start("https://mythmu.net/");
                        log.Info("Opened MythMU website.");
                        break;

                    case 2:
                        Process.Start("https://discord.gg/wFTznVrfv4");
                        log.Info("Opened MythMU Discord invite link.");
                        break;

                    case 3:
                        ModifyDashboardSettings(arguments);
                        break;

                    case 4:
                        OpenUrl(arguments);
                        break;

                    case 5:
                        DisplayBalloonTipWithTitle(arguments);
                        break;

                    case 6:
                        LoadCharacterDataFromProcess();
                        log.Info("Loaded character data from process.");
                        break;

                    case 8:
                        ToggleWindow(arguments);
                        break;

                    case 9:
                        KillProcess(arguments);
                        break;

                    case 10:
                        LaunchGuides();
                        log.Info("Launched guides.");
                        break;

                    case 11:
                        ToggleNewEffects();
                        log.Info("Toggled new effects.");
                        break;

                    case 12:
                        ShowMessageBox((string)arguments[1], (string)arguments[2]);
                        log.Info("Displayed message box with title: " + (string)arguments[1] + " and message: " + (string)arguments[2]);
                        break;

                    case 15:
                        ShowEventNotification(arguments);
                        break;

                    case 20:
                        ActivateEffects();
                        break;

                    case 1993:
                        this.Close();
                        log.Info("Application closed with command 1993.");
                        break;

                    default:
                        log.Error("Unknown command received: " + command);
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error("Exception occurred in exeFunctionAwesomium: " + ex.Message);
            }

            return new JSValue(true);
        }

        private void MonitorClient()
        {
            var timer = new System.Timers.Timer(5000);
            timer.Elapsed += ClientTick;
            timer.Start();
        }

        private void ClientTick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Process.GetProcessesByName("main").Length == 0)
            {
                if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
                Close();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                    cancellationTokenSource = null;
                }
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
        // private void ToggleWindow(JSValue[] arguments)
        // {
        //     if (arguments.Length > 1)
        //     {
        //         string clientIdStr = arguments[1].ToString();
        //         log.Info($"Received clientId argument: {clientIdStr}");

        //         if (int.TryParse(clientIdStr, out int clientId) && clientId >= 0)
        //         {
        //             log.Info($"Valid clientId: {clientId} for ToggleMainWindow.");
        //             if (processToClientIdMap.ContainsValue(clientId))
        //             {
        //                 var processId = processToClientIdMap.FirstOrDefault(x => x.Value == clientId).Key;
        //                 ToggleMainWindow(processId);
        //                 log.Info($"Toggled window for clientId: {clientId}, processId: {processId}");
        //             }
        //             else
        //             {
        //                 log.Error($"Invalid clientId: {clientId}. No valid process found.");
        //             }
        //         }
        //         else
        //         {
        //             log.Error($"Invalid clientId: {clientIdStr}. It must be a valid positive integer.");
        //         }
        //     }
        //     else
        //     {
        //         log.Error("Insufficient arguments for ToggleWindow. Expected clientId.");
        //     }
        // }
    }
}