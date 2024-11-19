using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using NLog;

namespace ADashboard
{
    public static class WindowManager
    {
        private static readonly Logger log = LogManager.GetLogger("fileLogger");

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        public static void ToggleWindow(JSValue[] arguments, Dictionary<int, int> processToClientIdMap)
        {
            if (arguments.Length > 1)
            {
                string clientIdStr = arguments[1].ToString();
                log.Info($"Received clientId argument: {clientIdStr}");

                if (int.TryParse(clientIdStr, out int clientId) && clientId >= 0)
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
                log.Error("Insufficient arguments for ToggleWindow. Expected clientId.");
            }
        }

        public static void ToggleMainWindow(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                if (process != null)
                {
                    IntPtr windowHandle = process.MainWindowHandle;

                    if (windowHandle != IntPtr.Zero)
                    {
                        bool isWindowMinimized = false; // Logic to determine if the window is minimized

                        if (isWindowMinimized)
                        {
                            ShowWindow(windowHandle, SW_RESTORE);
                        }
                        else
                        {
                            ShowWindow(windowHandle, SW_MINIMIZE);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error in ToggleMainWindow for processId {processId}: {ex.Message}");
            }
        }

        public static IntPtr FindGameWindow()
        {
            Process[] processes = Process.GetProcessesByName("main");
            if (processes.Length > 0)
            {
                return processes[0].MainWindowHandle;
            }
            return IntPtr.Zero;
        }
    }
}