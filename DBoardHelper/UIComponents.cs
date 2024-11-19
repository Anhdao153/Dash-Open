using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Awesomium.Windows.Forms;
using NLog;

namespace ADashboard
{
    public static class UIComponents
    {
        public static void InitializeComponent()
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
        private static readonly Logger log = LogManager.GetLogger("fileLogger");

        public static void DisplayBalloonTip(JSValue[] arguments, NotifyIcon notifyIcon)
        {
            if (arguments.Length > 1)
            {
                notifyIcon.ShowBalloonTip(10, "", (string)arguments[1], ToolTipIcon.None);
                log.Info("Displayed balloon tip with message: " + (string)arguments[1]);
            }
            else
            {
                log.Error("Insufficient arguments for DisplayBalloonTip.");
            }
        }

        public static void ModifyDashboardSettings(JSValue[] arguments, string currentDir)
        {
            if (arguments.Length > 2)
            {
                string id = arguments[1].ToString();
                string value = arguments[2].ToString();
                string path = Path.Combine(currentDir, "Dashboard.ini");
                string[] lines = File.ReadAllLines(path);
                int sec = 2 * int.Parse(id);
                lines[0] = lines[0].Remove(sec, 2).Insert(sec, $"{value},");
                File.WriteAllLines(path, lines);
                log.Info($"Modified dashboard settings with ID: {id} and value: {value}");
            }
            else
            {
                log.Error("Insufficient arguments for ModifyDashboardSettings.");
            }
        }

        public static void OpenUrl(JSValue[] arguments)
        {
            if (arguments.Length > 1)
            {
                Process.Start((string)arguments[1]);
                log.Info("Opened URL: " + (string)arguments[1]);
            }
            else
            {
                log.Error("Insufficient arguments for OpenUrl.");
            }
        }

        public static void DisplayBalloonTipWithTitle(JSValue[] arguments, NotifyIcon notifyIcon)
        {
            if (arguments.Length > 2)
            {
                notifyIcon.ShowBalloonTip(10, (string)arguments[1], (string)arguments[2], ToolTipIcon.None);
                log.Info("Displayed balloon tip with title: " + (string)arguments[1] + " and message: " + (string)arguments[2]);
            }
            else
            {
                log.Error("Insufficient arguments for DisplayBalloonTipWithTitle.");
            }
        }

        public static void KillProcess(JSValue[] arguments)
        {
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
                log.Error("Insufficient arguments for KillProcess.");
            }
        }

        public static void LaunchGuides()
        {
            Process.Start("https://mythmu.net/About");
        }

        public static void ToggleNewEffects(string parentDir)
        {
            string effectsFile = Path.Combine(parentDir, "opengl32.dll");

            if (File.Exists(effectsFile))
            {
                Utilities.RenameFile(effectsFile, Path.Combine(parentDir, "opengl32-off.dll"));
                MessageBox.Show("The change will be loaded after restarting the game.", "Success");
            }
            else if (File.Exists(Path.Combine(parentDir, "opengl32-off.dll")))
            {
                Utilities.RenameFile(Path.Combine(parentDir, "opengl32-off.dll"), effectsFile);
                MessageBox.Show("The change will be loaded after restarting the game.", "Success");
            }
            else
            {
                MessageBox.Show("Unable to execute the function, please contact the administration through Discord.", "Error");
            }
        }

        public static void ShowMessageBox(string title, string message)
        {
            MessageBox.Show(message, title);
        }

        public static void ShowEventNotification(JSValue[] arguments, NotifyIcon notifyIcon)
        {
            if (arguments.Length > 2)
            {
                string eventTitle = arguments[1].ToString();
                string eventDescription = arguments[2].ToString();
                notifyIcon.ShowBalloonTip(10, eventTitle, eventDescription, ToolTipIcon.None);
                log.Info($"Event notification received: {eventTitle} - {eventDescription}");
            }
            else
            {
                log.Error("Insufficient arguments for event notification.");
            }
        }

        public static void ActivateEffects()
        {
            IntPtr gameWindowHandle = WindowManager.FindGameWindow();
            if (gameWindowHandle != IntPtr.Zero)
            {
                WindowManager.SetForegroundWindow(gameWindowHandle);
                log.Info("Game window brought to the foreground.");
            }
            else
            {
                log.Error("Failed to find game window for ActivateEffects.");
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
                    notifyIcon1.ShowBalloonTip(10, "Effect Toggles", "All selected effects have been activated.", ToolTipIcon.None);
                    log.Info("All effect toggles have been activated.");
                }
            };

            timer.Start();
        }
    }
}