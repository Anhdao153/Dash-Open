using Awesomium.Core;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ADashboard
{
    internal static class Program
    {
        private static readonly string currentDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static string parentDir;
        private static Mutex mutex;

        // URLs for Non-RR and RR servers
        private static readonly string nonRRUrl = "http://nonrr.mythmu.net/introdash/";
        private static readonly string rrUrl = "http://rr.mythmu.net/introdash/";
        private static string selectedUrl;

        // Method to check for internet connectivity by attempting to connect to the selected URL
        private static bool IsConnectedToInternet()
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    using (webClient.OpenRead(selectedUrl))
                    {
                        logger.Info($"Successfully connected to {selectedUrl}.");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to connect to {selectedUrl}.");
                return false;
            }
        }

        // Application entry point
        private static void Main()
        {
            logger.Info("Application starting.");

            // Choose between Non-RR or RR URL based on a configuration file
            ConfigureServerUrl();

            // Generate a unique mutex name based on the app's location hash to prevent multiple instances
            using (var md5 = MD5.Create())
            {
                byte[] locHash = md5.ComputeHash(Encoding.UTF8.GetBytes(Assembly.GetEntryAssembly().Location));
                string mutexName = Convert.ToBase64String(locHash);
                bool isNewInstance;

                // Initialize a new mutex based on the unique location hash
                mutex = new Mutex(true, mutexName, out isNewInstance);

                if (!isNewInstance)
                {
                    logger.Warn("Another instance of the application is already running.");

                    // Attempt to start the launcher if not already running
                    if (Process.GetProcessesByName("Launcher").Length == 0)
                    {
                        parentDir = new DirectoryInfo(currentDir).Parent?.FullName;
                        string launcherPath = Path.Combine(parentDir ?? string.Empty, "Launcher.exe");

                        if (File.Exists(launcherPath))
                        {
                            logger.Info("Starting Launcher.exe.");
                            Process.Start(launcherPath);
                        }
                        else
                        {
                            logger.Error("Launcher.exe not found.");
                            MessageBox.Show("Launcher.exe was not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    return;
                }

                // Check internet connection
                if (!IsConnectedToInternet())
                {
                    MessageBox.Show("No Internet connection. Please check your connection and try again.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    logger.Warn("Internet connection not available.");
                    return;
                }

                // Initialize Awesomium WebCore without the 'suppressErrors' parameter
                try
                {
                    logger.Info("Initializing Awesomium WebCore.");
                    WebCore.Initialize(new WebConfig { LogLevel = Awesomium.Core.LogLevel.None });
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to initialize WebCore.");
                    MessageBox.Show("Failed to initialize WebCore: " + ex.Message, "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Run the main application
                try
                {
                    logger.Info("Launching main dashboard application.");
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new DBoard());
                }
                catch (Exception ex)
                {
                    logger.Fatal(ex, "An unexpected error occurred while running the application.");
                    MessageBox.Show("An unexpected error occurred: " + ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    // Clean up Awesomium resources
                    if (WebCore.IsInitialized)
                    {
                        WebCore.Shutdown();
                        logger.Info("WebCore shutdown completed.");
                    }
                    logger.Info("Application closed.");
                }
            }
        }

        // Configures the selected server URL based on a configuration file
        private static void ConfigureServerUrl()
        {
            string configPath = Path.Combine(currentDir, "ServerConfig.txt");
            if (File.Exists(configPath))
            {
                string savedServerType = File.ReadAllText(configPath).Trim();
                selectedUrl = savedServerType.Equals("RR", StringComparison.OrdinalIgnoreCase) ? rrUrl : nonRRUrl;
                logger.Info($"Selected server URL set to: {selectedUrl}");
            }
            else
            {
                // Default to Non-RR and save this choice if no configuration file exists
                selectedUrl = nonRRUrl;
                File.WriteAllText(configPath, "Non-RR");
                logger.Info("No ServerConfig.txt found. Defaulting to Non-RR server.");
            }
        }
    }
}
