using ADashboard.Properties;
using Awesomium.Core;
using Awesomium.Windows.Forms;
using DiscordRPC;
using Newtonsoft.Json;
using NLog;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ADashboard
{
    public class DBoard : Form
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
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(int hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private Dictionary<int, int> processToClientIdMap = new Dictionary<int, int>();
        private int nextClientId = 0;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_SHIFT = 0x10;
        private const byte VK_2 = 0x32;
        private const byte VK_3 = 0x33;
        private const byte VK_8 = 0x38;
        private const byte VK_9 = 0x39;
        private Dictionary<int, string> processCharacterMap = new Dictionary<int, string>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Dictionary<int, CharacterData> previousCharacterDataMap = new Dictionary<int, CharacterData>();

        private readonly string nonRRUrl = "http://nonrr.mythmu.net/introdash/";
        private string currentApiUrl;

        private List<CharacterData> characterDataList = new List<CharacterData>();

        public DBoard()
        {
            InitializeComponent();
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

        private DiscordRpcClient discordClient;

        private void InitializeDiscord()
        {
            discordClient = new DiscordRpcClient("1149097329782173727", pipe: discordPipe);
            discordClient.Initialize();
        }

        private static readonly Dictionary<int, string> vipRoles = new Dictionary<int, string>
        {
            { 0, "Basic" }, { 1, "Standard" }, { 2, "Enhanced" }, { 3, "Bronze" }, { 4, "Silver" },
            { 5, "Gold" }, { 6, "Platinum" }, { 7, "Diamond" }, { 8, "Sapphire" }, { 9, "Emerald" },
            { 10, "Ruby" }, { 11, "Topaz" }, { 12, "Opal" }, { 13, "Pearl" }, { 14, "Elite" }, { 15, "Legend" }
        };

        private string GetClassImageKey(string characterClass) => characterClass;

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

        private bool ReadCharacterClassFromMemory(Process process, CharacterData characterData)
        {
            try
            {
                MemoryReader memoryReader = new MemoryReader();
                IntPtr classBaseAddress = new IntPtr(0x01CFDAA8);
                int offset = 0x4;
                characterData.@class = memoryReader.ReadInt(process, ResolvePointerAddress(process, classBaseAddress, new int[] { 0x0, offset })).ToString();
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error reading character class from memory: {ex.Message}");
                return false;
            }
        }

        private bool ReadPointStatsFromMemory(Process process, CharacterData characterData)
        {
            try
            {
                MemoryReader memoryReader = new MemoryReader();
                IntPtr statsBaseAddress = new IntPtr(0x01CFDDFC);
                int[] strOffset = new int[] { 0x0, 0x18A };
                int[] agiOffset = new int[] { 0x0, 0x18C };
                int[] vitOffset = new int[] { 0x0, 0x18E };
                int[] eneOffset = new int[] { 0x0, 0x190 };
                int[] cmdOffset = new int[] { 0x0, 0x192 };
                characterData.str = ReadStatFromMemory(process, statsBaseAddress, strOffset);
                characterData.agi = ReadStatFromMemory(process, statsBaseAddress, agiOffset);
                characterData.vit = ReadStatFromMemory(process, statsBaseAddress, vitOffset);
                characterData.ene = ReadStatFromMemory(process, statsBaseAddress, eneOffset);
                characterData.cmd = ReadStatFromMemory(process, statsBaseAddress, cmdOffset);
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error reading character stats: {ex.Message}");
                return false;
            }
        }

        private string ReadStatFromMemory(Process process, IntPtr baseAddress, int[] offset, bool is2Bytes = false)
        {
            try
            {
                MemoryReader memoryReader = new MemoryReader();
                return is2Bytes
                    ? memoryReader.ReadInt16(process, ResolvePointerAddress(process, baseAddress, offset)).ToString()
                    : memoryReader.ReadInt(process, ResolvePointerAddress(process, baseAddress, offset)).ToString();
            }
            catch (Exception ex)
            {
                log.Error($"Error reading stat from memory at {string.Join(",", offset)}: {ex.Message}");
                return "0";
            }
        }

        private bool ReadVIPFromMemory(Process process, CharacterData characterData)
        {
            try
            {
                MemoryReader memoryReader = new MemoryReader();
                IntPtr vipAddress = new IntPtr(0x100AB52A);
                byte vipByte = memoryReader.ReadByte(process, vipAddress);
                string newVipValue = vipByte.ToString();
                if (characterData.vipAddress != newVipValue)
                {
                    characterData.vipAddress = newVipValue;
                }
                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error reading VIP address from memory: {ex.Message}");
                return false;
            }
        }

        private bool ReadCharacterStatsFromMemory(Process process, CharacterData characterData)
        {
            try
            {
                MemoryReader memoryReader = new MemoryReader();

                // Base address for stats
                IntPtr statsBaseAddress = new IntPtr(0x02315534); // Address for character stats
                IntPtr positionBaseAddress = new IntPtr(0x02315534); // Address for position stats
                IntPtr terrainIndexAddress = new IntPtr(0x02315534); // Terrain Index address
                IntPtr serverIndexAddress = new IntPtr(0x02315534);

                // Offsets for existing stats (Adjust based on byte size)
                int[][] offsets = new int[][] {
                    new int[] { 0x0, 0x68 },  // HP (4 bytes)
                    new int[] { 0x0, 0x68 },  // Max HP (4 bytes)
                    new int[] { 0x0, 0x68 },  // MP (4 bytes)
                    new int[] { 0x0, 0x68 },  // Max MP (4 bytes)
                    new int[] { 0x0, 0x68 },  // EXP (4 bytes)
                    new int[] { 0x0, 0x68 },  // Next EXP (4 bytes)
                    new int[] { 0x0, 0x68 },  // Shield (4 bytes)
                    new int[] { 0x0, 0x68 },  // Max Shield (4 bytes)
                    new int[] { 0x0, 0x68 },  // Skill Mana (4 bytes)
                    new int[] { 0x0, 0x68 },  // Max Skill Mana (4 bytes)
                    new int[] { 0x0, 0x68 }, // Position X (4 bytes)
                    new int[] { 0x0, 0x68 }, // Position Y (4 bytes)
                    new int[] { 0x0, 0x68 }, // Strength (2 bytes)
                    new int[] { 0x0, 0x68 }, // Agility (2 bytes)
                    new int[] { 0x0, 0x68 }, // Vitality (2 bytes)
                    new int[] { 0x0, 0x68 }, // Energy (1 byte or 2 bytes)
                    new int[] { 0x0, 0x192 }, // Command (2 bytes)
                };

                // Read basic stats (HP, Max HP, MP, etc.)
                characterData.hp = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[0])).ToString();  // HP
                characterData.maxHp = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[1])).ToString(); // Max HP
                characterData.mp = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[2])).ToString();  // MP
                characterData.maxMp = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[3])).ToString(); // Max MP
                characterData.exp = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[4])).ToString();  // EXP
                characterData.nextExp = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[5])).ToString();  // Next EXP
                characterData.shield = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[6])).ToString();  // Shield
                characterData.maxShield = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[7])).ToString(); // Max Shield
                characterData.skillMana = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[8])).ToString(); // Skill Mana
                characterData.maxSkillMana = memoryReader.ReadInt(process, ResolvePointerAddress(process, statsBaseAddress, offsets[9])).ToString();  // Max Skill Mana
                characterData.posX = memoryReader.ReadInt(process, ResolvePointerAddress(process, positionBaseAddress, offsets[10])).ToString(); // Position X
                characterData.posY = memoryReader.ReadInt(process, ResolvePointerAddress(process, positionBaseAddress, offsets[11])).ToString(); // Position Y
                characterData.terrainIndex = memoryReader.ReadInt16(process, terrainIndexAddress).ToString();
                characterData.serverIndex = memoryReader.ReadString(process, serverIndexAddress, 10); // Replace 10 with the max expected length if different

                // Read new stats: Strength (Str), Agility (Agi), Vitality (Vit), Energy (Ene), and Command (Cmd)
                characterData.str = ReadStatFromMemory(process, statsBaseAddress, new int[] { 0x0, 0x68 }, true); // Strength (2 bytes)
                characterData.agi = ReadStatFromMemory(process, statsBaseAddress, new int[] { 0x0, 0x68 }, true); // Agility (2 bytes)
                characterData.vit = ReadStatFromMemory(process, statsBaseAddress, new int[] { 0x0, 0x68 }, true); // Vitality (2 bytes)
                characterData.ene = ReadStatFromMemory(process, statsBaseAddress, new int[] { 0x0, 0x68 }, false); // Energy (1 byte or 2 bytes)
                characterData.cmd = ReadStatFromMemory(process, statsBaseAddress, new int[] { 0x0, 0x68 }, true);  // Command (2 bytes)

                if (!ReadVIPFromMemory(process, characterData))
                {
                    return false;
                }

                if (!ReadCharacterClassFromMemory(process, characterData))
                {
                    log.Error("Error reading character class.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error($"Error reading character stats: {ex.Message}");
                return false;
            }
        }

        private IntPtr ResolvePointerAddress(Process process, IntPtr moduleBaseAddress, int[] offsets)
        {
            MemoryReader memoryReader = new MemoryReader();
            IntPtr address = moduleBaseAddress + offsets[0];
            for (int i = 1; i < offsets.Length; i++)
            {
                address = memoryReader.ReadIntPtr(process, address) + offsets[i];
            }
            return address;
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

        private string ExtractCharacterName(string mainWindowTitle)
        {
            string[] charData = mainWindowTitle.Split(new[] { "Name: " }, StringSplitOptions.None);
            return charData.Length > 1 ? charData[1].Split(' ')[0].Replace("[", "").Replace("]", "") : string.Empty;
        }

        private string ExtractCharacterLevel(string mainWindowTitle)
        {
            string[] levelData = mainWindowTitle.Split(new[] { "Level: [" }, StringSplitOptions.None);
            return levelData.Length > 1 ? levelData[1].Split(']')[0] : "0";
        }

        private string ExtractCharacterMasterLevel(string mainWindowTitle)
        {
            string[] masterLevelData = mainWindowTitle.Split(new[] { "Master Level: [" }, StringSplitOptions.None);
            return masterLevelData.Length > 1 ? masterLevelData[1].Split(']')[0] : "0";
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

        private string ExtractValue(string mainWindowTitle, string startTag, string endTag)
        {
            string[] data = mainWindowTitle.Split(new[] { startTag }, StringSplitOptions.None);
            return data.Length > 1 ? data[1].Split(new[] { endTag }, StringSplitOptions.None)[0] : "0";
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

                if (previousData.name == characterData.name &&
                    previousData.level == characterData.level &&
                    previousData.masterLevel == characterData.masterLevel &&
                    previousData.@class == characterData.@class &&
                    previousData.hp == characterData.hp &&
                    previousData.maxHp == characterData.maxHp &&
                    previousData.mp == characterData.mp &&
                    previousData.maxMp == characterData.maxMp &&
                    previousData.shield == characterData.shield &&
                    previousData.maxShield == characterData.maxShield &&
                    previousData.skillMana == characterData.skillMana &&
                    previousData.maxSkillMana == characterData.maxSkillMana &&
                    previousData.exp == characterData.exp &&
                    previousData.nextExp == characterData.nextExp &&
                    previousData.posX == characterData.posX &&
                    previousData.posY == characterData.posY &&
                    previousData.terrainIndex == characterData.terrainIndex &&
                    previousData.serverIndex == characterData.serverIndex &&
                    previousData.vipAddress == characterData.vipAddress &&
                    previousData.str == characterData.str &&
                    previousData.agi == characterData.agi &&
                    previousData.vit == characterData.vit &&
                    previousData.ene == characterData.ene &&
                    previousData.cmd == characterData.cmd)
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
                existingData.name = characterData.name;
                existingData.level = characterData.level;
                existingData.masterLevel = characterData.masterLevel;
                existingData.@class = characterData.@class;
                existingData.hp = characterData.hp;
                existingData.maxHp = characterData.maxHp;
                existingData.mp = characterData.mp;
                existingData.maxMp = characterData.maxMp;
                existingData.shield = characterData.shield;
                existingData.maxShield = characterData.maxShield;
                existingData.skillMana = characterData.skillMana;
                existingData.maxSkillMana = characterData.maxSkillMana;
                existingData.exp = characterData.exp;
                existingData.nextExp = characterData.nextExp;
                existingData.posX = characterData.posX;
                existingData.posY = characterData.posY;
                existingData.terrainIndex = characterData.terrainIndex;
                existingData.serverIndex = characterData.serverIndex;
                existingData.vipAddress = characterData.vipAddress;
                existingData.str = characterData.str;
                existingData.agi = characterData.agi;
                existingData.vit = characterData.vit;
                existingData.ene = characterData.ene;
                existingData.cmd = characterData.cmd;
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


        private bool IsBase64String(string base64)
        {
            base64 = base64.Trim();
            return (base64.Length % 4 == 0) && Regex.IsMatch(base64, @"^[a-zA-Z0-9\\+/]*={0,2}$", RegexOptions.None);
        }

        private string DecodeBase64(string base64String)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private Dictionary<int, IntPtr> mainWindows = new Dictionary<int, IntPtr>();
        private Dictionary<int, bool> windowVisibilityMap = new Dictionary<int, bool>();

        private void ToggleMainWindow(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process != null)
                {
                    IntPtr windowHandle = process.MainWindowHandle;

                    if (windowHandle != IntPtr.Zero)
                    {
                        bool isWindowMinimized = windowVisibilityMap.ContainsKey(processId) && windowVisibilityMap[processId];

                        if (isWindowMinimized)
                        {
                            ShowWindow(windowHandle, SW_RESTORE);
                            windowVisibilityMap[processId] = false;
                        }
                        else
                        {
                            ShowWindow(windowHandle, SW_MINIMIZE);
                            windowVisibilityMap[processId] = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in ToggleMainWindow for processId {processId}: {ex.Message}");
            }
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private void RestoreWindow(string windowHandle)
        {
            hWnd = int.Parse(windowHandle);
            ShowWindow(hWnd, SW_RESTORE);
        }

        private void KillProcess(string characterName, string processId)
        {
            foreach (Process process in Process.GetProcessesByName("main"))
            {
                if (process.MainWindowTitle.Contains($"[{characterName}]") && process.Id == int.Parse(processId))
                {
                    process.Kill();
                }
            }
        }

        private void ToggleNewEffects()
        {
            string effectsFile = Path.Combine(parentDir, "opengl32.dll");

            if (File.Exists(effectsFile))
            {
                RenameFile(effectsFile, Path.Combine(parentDir, "opengl32-off.dll"));
                MessageBox.Show("The change will be loaded after restarting the game.", "Success");
            }
            else if (File.Exists(Path.Combine(parentDir, "opengl32-off.dll")))
            {
                RenameFile(Path.Combine(parentDir, "opengl32-off.dll"), effectsFile);
                MessageBox.Show("The change will be loaded after restarting the game.", "Success");
            }
            else
            {
                MessageBox.Show("Unable to execute the function, please contact the administration through Discord.", "Error");
            }
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
                        if (arguments.Length > 2)
                        {
                            string id = arguments[1].ToString();
                            string value = arguments[2].ToString();

                            ModifyDashboardSettings(id, value);

                            log.Info($"Modified dashboard settings with ID: {id} and value: {value}");
                        }
                        else
                        {
                            log.Error("Insufficient arguments for command 3 (ModifyDashboardSettings).");
                        }
                        break; ;

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
                        if (arguments.Length > 1)
                        {
                            string clientIdStr = arguments[1].ToString();
                            log.Info($"Received clientId argument: {clientIdStr}");

                            int clientId;
                            if (int.TryParse(clientIdStr, out clientId) && clientId >= 0)
                            {
                                log.Info($"Valid clientId: {clientId} for ToggleMainWindow.");
                                if (processToClientIdMap.ContainsValue(clientId))
                                {
                                    var processId = processToClientIdMap.FirstOrDefault(x => x.Value == clientId).Key;

                                    ToggleMainWindow(processId);
                                    log.Info($"Toggled window for clientId: {clientId}, processId: {processId}");
                                }
                                else
                                {
                                    log.Error($"Invalid clientId: {clientId}. No valid process found.");
                                }
                            }
                            else
                            {
                                log.Error($"Invalid clientId: {clientIdStr}. It must be a valid positive integer.");
                            }
                        }
                        else
                        {
                            log.Error("Insufficient arguments for command 8 (ToggleMainWindow). Expected clientId.");
                        }
                        break;

                    case 9:
                        if (arguments.Length > 2)
                        {
                            string characterName = arguments[1].ToString();
                            string processId = arguments[2].ToString();

                            try
                            {
                                foreach (Process process in Process.GetProcessesByName("main"))
                                {
                                    if (process.MainWindowTitle.Contains("MythMU") &&
                                        process.MainWindowTitle.Contains("[" + characterName + "]") &&
                                        process.Id.ToString() == processId)
                                    {
                                        process.Kill();
                                        log.Info("Killed process with ID: " + processId + " for character: " + characterName);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Error($"Error killing process for character '{characterName}' with ID '{processId}': {ex.Message}");
                            }
                        }
                        else
                        {
                            log.Error("Insufficient arguments for command 9 (KillProcess). Expected character name and process ID.");
                        }
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

        private void DisplayBalloonTip(JSValue[] arguments)
        {
            if (arguments.Length > 1)
            {
                this.notifyIcon1.ShowBalloonTip(10, "", (string)arguments[1], ToolTipIcon.None);
                log.Info("Displayed balloon tip with message: " + (string)arguments[1]);
            }
            else
            {
                log.Error("Insufficient arguments for DisplayBalloonTip (command 0).");
            }
        }

        private void ModifyDashboardSettings(JSValue[] arguments)
        {
            if (arguments.Length > 2)
            {
                string id = arguments[1].ToString();
                string value = arguments[2].ToString();
                ModifyDashboardSettings(id, value);
                log.Info($"Modified dashboard settings with ID: {id} and value: {value}");
            }
            else
            {
                log.Error("Insufficient arguments for ModifyDashboardSettings (command 3).");
            }
        }

        private void OpenUrl(JSValue[] arguments)
        {
            if (arguments.Length > 1)
            {
                Process.Start((string)arguments[1]);
                log.Info("Opened URL: " + (string)arguments[1]);
            }
            else
            {
                log.Error("Insufficient arguments for OpenUrl (command 4).");
            }
        }

        private void DisplayBalloonTipWithTitle(JSValue[] arguments)
        {
            if (arguments.Length > 2)
            {
                this.notifyIcon1.ShowBalloonTip(10, (string)arguments[1], (string)arguments[2], ToolTipIcon.None);
                log.Info("Displayed balloon tip with title: " + (string)arguments[1] + " and message: " + (string)arguments[2]);
            }
            else
            {
                log.Error("Insufficient arguments for DisplayBalloonTipWithTitle (command 5).");
            }
        }

        private void ToggleWindow(JSValue[] arguments)
        {
            int processId;
            if (arguments.Length > 1 && int.TryParse(arguments[1].ToString(), out processId))
            {
                ToggleMainWindow(processId);
                log.Info("Toggled main window for process ID: " + processId);
            }
            else
            {
                log.Error("Invalid or missing processId in ToggleWindow (command 8).");
            }
        }

        private void KillProcess(JSValue[] arguments)
        {
            if (arguments.Length > 2)
            {
                string processName = (string)arguments[1];
                string reason = arguments[2].ToString();
                KillProcess(processName, reason);
                log.Info("Killed process: " + processName + " with reason: " + reason);
            }
            else
            {
                log.Error("Insufficient arguments for KillProcess (command 9).");
            }
        }

        private void ShowEventNotification(JSValue[] arguments)
        {
            if (arguments.Length > 2)
            {
                string eventTitle = arguments[1].ToString();
                string eventDescription = arguments[2].ToString();
                ShowNotification(eventDescription, eventTitle);
                log.Info($"Event notification received: {eventTitle} - {eventDescription}");
            }
            else
            {
                log.Error("Insufficient arguments for event notification (command 15).");
            }
        }

        private void ActivateEffects()
        {
            IntPtr gameWindowHandle = FindGameWindow();
            if (gameWindowHandle != IntPtr.Zero)
            {
                SetForegroundWindow(gameWindowHandle);
                log.Info("Game window brought to the foreground.");
            }
            else
            {
                log.Error("Failed to find game window for ActivateEffects (command 20).");
            }

            var effectCommands = new List<(string key, string description)>
            {
                ("+8", "Hide Skill Effect"),
                ("+9", "Reduce Item Shine to +0"),
                ("+2", "Reduce Skill Effect"),
                ("+3", "Reduce Item Effect")
            };

            int currentIndex = 0;
            var timer = new System.Windows.Forms.Timer { Interval = 300 };

            timer.Tick += (s, e) =>
            {
                if (currentIndex < effectCommands.Count)
                {
                    var effectCommand = effectCommands[currentIndex];
                    SendKeys.Send(effectCommand.key);
                    log.Info($"Effect toggle: {effectCommand.description} executed with key {effectCommand.key}");
                    currentIndex++;
                }
                else
                {
                    timer.Stop();
                    ShowNotification("All selected effects have been activated.", "Effect Toggles");
                    log.Info("All effect toggles have been activated.");
                }
            };

            timer.Start();
        }

        private IntPtr FindGameWindow()
        {
            Process[] processes = Process.GetProcessesByName("main");
            if (processes.Length > 0)
            {
                return processes[0].MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        private void ShowNotification(string message, string title = "")
        {
            notifyIcon1.ShowBalloonTip(10, title, message, ToolTipIcon.None);
        }

        private void ModifyDashboardSettings(string settingIndex, string newValue)
        {
            string path = Path.Combine(currentDir, "Dashboard.ini");
            string[] lines = File.ReadAllLines(path);
            int sec = 2 * int.Parse(settingIndex);
            lines[0] = lines[0].Remove(sec, 2).Insert(sec, $"{newValue},");
            File.WriteAllLines(path, lines);
        }

        private void StartExternalProcess(string path)
        {
            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                log.Error($"Error starting process: {ex.Message}");
            }
        }

        private void LaunchGuides()
        {
            Process.Start("https://mythmu.net/About");
        }

        private void ShowMessageBox(string title, string message)
        {
            MessageBox.Show(message, title);
        }

        public void RenameFile(string originalName, string newName) => File.Move(originalName, newName);

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

        private void panel_top_MouseDown(object sender, MouseEventArgs e)
        {
            this.drag = true;
            this.start_point = new Point(e.X, e.Y);
        }

        private void panel_top_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.drag) return;
            Point screen = this.PointToScreen(e.Location);
            this.Location = new Point(screen.X - this.start_point.X, screen.Y - this.start_point.Y);
        }

        private void panel_top_MouseUp(object sender, MouseEventArgs e) => this.drag = false;

        private void panel1_Click(object sender, EventArgs e)
        {
            int num = 0;
            foreach (Process process in Process.GetProcessesByName("main"))
                ++num;
            if (num > 0)
            {
                this.notifyIcon1.ShowBalloonTip(10, " ", "You still have game clients open, if hidden press the F12 key to redisplay them.", ToolTipIcon.None);
                Thread.Sleep(2000);
                this.Close();
            }
            else
                this.Close();
        }

        private void panel1_Click_1(object sender, EventArgs e)
        {
            this.Hide();
            this.notifyIcon1.ShowBalloonTip(500);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e) => this.Show();

        private void SettingsBTN_Click(object sender, EventArgs e)
        {
            if (WebCore.IsInitialized)
            {
                try
                {
                    webControl1.ExecuteJavascript("goSettingsList()");
                }
                catch (Exception ex)
                {
                    log.Error($"Failed to execute JavaScript: {ex.Message}");
                    MessageBox.Show("An error occurred while executing JavaScript. Please restart the application.");
                }
            }
            else
            {
                MessageBox.Show("WebCore is not initialized or running. Please restart the application.");
            }
        }

        private void CloseBTN_MouseEnter(object sender, EventArgs e) => this.CloseBTN.BackgroundImage = Resources.button_close_hover;

        private void CloseBTN_MouseLeave(object sender, EventArgs e) => this.CloseBTN.BackgroundImage = Resources.button_close;

        private void MinimizerBTN_MouseEnter(object sender, EventArgs e) => this.MinimizerBTN.BackgroundImage = Resources.button_minimizer_hover;

        private void MinimizerBTN_MouseLeave(object sender, EventArgs e) => this.MinimizerBTN.BackgroundImage = Resources.button_minimizer;

        private void SettingsBTN_MouseEnter(object sender, EventArgs e) => this.SettingsBTN.BackgroundImage = Resources.button_settings_hover;

        private void SettingsBTN_MouseLeave(object sender, EventArgs e) => this.SettingsBTN.BackgroundImage = Resources.button_settings;

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DBoard));
            this.CloseBTN = new System.Windows.Forms.Panel();
            this.MinimizerBTN = new System.Windows.Forms.Panel();
            this.webControl1 = new Awesomium.Windows.Forms.WebControl(this.components);
            this.panel_top = new System.Windows.Forms.Panel();
            this.SettingsBTN = new System.Windows.Forms.Panel();
            this.label_top_2 = new System.Windows.Forms.Label();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.webControlContextMenu1 = new Awesomium.Windows.Forms.WebControlContextMenu(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.panel_top.SuspendLayout();
            this.SuspendLayout();
            // 
            // CloseBTN
            // 
            this.CloseBTN.BackColor = System.Drawing.Color.Transparent;
            this.CloseBTN.BackgroundImage = global::ADashboard.Properties.Resources.button_close;
            this.CloseBTN.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CloseBTN.Location = new System.Drawing.Point(223, 7);
            this.CloseBTN.Name = "CloseBTN";
            this.CloseBTN.Size = new System.Drawing.Size(25, 25);
            this.CloseBTN.TabIndex = 2;
            this.toolTip.SetToolTip(this.CloseBTN, "Close");
            this.CloseBTN.Click += new System.EventHandler(this.panel1_Click);
            this.CloseBTN.MouseEnter += new System.EventHandler(this.CloseBTN_MouseEnter);
            this.CloseBTN.MouseLeave += new System.EventHandler(this.CloseBTN_MouseLeave);
            // 
            // MinimizerBTN
            // 
            this.MinimizerBTN.BackColor = System.Drawing.Color.Transparent;
            this.MinimizerBTN.BackgroundImage = global::ADashboard.Properties.Resources.button_minimizer;
            this.MinimizerBTN.Cursor = System.Windows.Forms.Cursors.Hand;
            this.MinimizerBTN.Location = new System.Drawing.Point(200, 7);
            this.MinimizerBTN.Name = "MinimizerBTN";
            this.MinimizerBTN.Size = new System.Drawing.Size(25, 25);
            this.MinimizerBTN.TabIndex = 3;
            this.toolTip.SetToolTip(this.MinimizerBTN, "Minimizer");
            this.MinimizerBTN.Click += new System.EventHandler(this.panel1_Click_1);
            this.MinimizerBTN.MouseEnter += new System.EventHandler(this.MinimizerBTN_MouseEnter);
            this.MinimizerBTN.MouseLeave += new System.EventHandler(this.MinimizerBTN_MouseLeave);
            // 
            // webControl1
            // 
            this.webControl1.BackColor = System.Drawing.SystemColors.Window;
            this.webControl1.Location = new System.Drawing.Point(0, 0);
            this.webControl1.Margin = new System.Windows.Forms.Padding(0);
            this.webControl1.NavigationInfo = Awesomium.Core.NavigationInfo.None;
            this.webControl1.Size = new System.Drawing.Size(265, 380);
            this.webControl1.TabIndex = 3;
            // 
            // panel_top
            // 
            this.panel_top.BackColor = System.Drawing.Color.Transparent;
            this.panel_top.BackgroundImage = global::ADashboard.Properties.Resources.bg_new;
            this.panel_top.Controls.Add(this.SettingsBTN);
            this.panel_top.Controls.Add(this.label_top_2);
            this.panel_top.Controls.Add(this.CloseBTN);
            this.panel_top.Controls.Add(this.MinimizerBTN);
            this.panel_top.Location = new System.Drawing.Point(0, 0);
            this.panel_top.Margin = new System.Windows.Forms.Padding(0);
            this.panel_top.Name = "panel_top";
            this.panel_top.Size = new System.Drawing.Size(264, 37);
            this.panel_top.TabIndex = 2;
            this.panel_top.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_top_MouseDown);
            this.panel_top.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel_top_MouseMove);
            this.panel_top.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_top_MouseUp);
            // 
            // SettingsBTN
            // 
            this.SettingsBTN.BackgroundImage = global::ADashboard.Properties.Resources.button_settings;
            this.SettingsBTN.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SettingsBTN.Location = new System.Drawing.Point(177, 7);
            this.SettingsBTN.Name = "SettingsBTN";
            this.SettingsBTN.Size = new System.Drawing.Size(25, 25);
            this.SettingsBTN.TabIndex = 0;
            this.SettingsBTN.Click += new System.EventHandler(this.SettingsBTN_Click);
            this.SettingsBTN.MouseEnter += new System.EventHandler(this.SettingsBTN_MouseEnter);
            this.SettingsBTN.MouseLeave += new System.EventHandler(this.SettingsBTN_MouseLeave);
            // 
            // label_top_2
            // 
            this.label_top_2.AutoSize = true;
            this.label_top_2.ForeColor = System.Drawing.Color.White;
            this.label_top_2.Location = new System.Drawing.Point(33, 16);
            this.label_top_2.Name = "label_top_2";
            this.label_top_2.Size = new System.Drawing.Size(0, 13);
            this.label_top_2.TabIndex = 1;
            this.label_top_2.MouseDown += new System.Windows.Forms.MouseEventHandler(this.panel_top_MouseDown);
            this.label_top_2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.panel_top_MouseMove);
            this.label_top_2.MouseUp += new System.Windows.Forms.MouseEventHandler(this.panel_top_MouseUp);
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.BalloonTipText = "Your dashboard session has been minimized.";
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // webControlContextMenu1
            // 
            this.webControlContextMenu1.Name = "webControlContextMenu1";
            this.webControlContextMenu1.Size = new System.Drawing.Size(203, 126);
            this.webControlContextMenu1.View = null;
            // 
            // DBoard
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(264, 375);
            this.Controls.Add(this.panel_top);
            this.Controls.Add(this.webControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(500, 500);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RR";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.panel_top.ResumeLayout(false);
            this.panel_top.PerformLayout();
            this.ResumeLayout(false);

        }

        public class CharacterData
        {
            public string clientId { get; set; }
            public string name { get; set; }
            public string level { get; set; }
            public string masterLevel { get; set; }
            public string @class { get; set; }
            public string hp { get; set; }
            public string maxHp { get; set; }
            public string mp { get; set; }
            public string maxMp { get; set; }
            public string shield { get; set; }
            public string maxShield { get; set; }
            public string skillMana { get; set; }
            public string maxSkillMana { get; set; }
            public string exp { get; set; }
            public string nextExp { get; set; }
            public string posX { get; set; }
            public string posY { get; set; }
            public string terrainIndex { get; set; }
            public string serverIndex { get; set; }
            public string str { get; set; }
            public string agi { get; set; }
            public string vit { get; set; }
            public string ene { get; set; }
            public string cmd { get; set; }
            public string vipAddress { get; set; }
        }
    }
}