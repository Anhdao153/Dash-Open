using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using NLog;

namespace ADashboard
{
    public static class Utilities
    {
        private static readonly Logger log = LogManager.GetLogger("fileLogger");

        public static string ExtractCharacterName(string mainWindowTitle)
        {
            string[] charData = mainWindowTitle.Split(new[] { "Name: " }, StringSplitOptions.None);
            return charData.Length > 1 ? charData[1].Split(' ')[0].Replace("[", "").Replace("]", "") : string.Empty;
        }

        public static string ExtractCharacterLevel(string mainWindowTitle)
        {
            string[] levelData = mainWindowTitle.Split(new[] { "Level: [" }, StringSplitOptions.None);
            return levelData.Length > 1 ? levelData[1].Split(']')[0] : "0";
        }

        public static string ExtractCharacterMasterLevel(string mainWindowTitle)
        {
            string[] masterLevelData = mainWindowTitle.Split(new[] { "Master Level: [" }, StringSplitOptions.None);
            return masterLevelData.Length > 1 ? masterLevelData[1].Split(']')[0] : "0";
        }

        public static bool IsBase64String(string base64)
        {
            base64 = base64.Trim();
            return (base64.Length % 4 == 0) && Regex.IsMatch(base64, @"^[a-zA-Z0-9\\+/]*={0,2}$", RegexOptions.None);
        }

        public static string DecodeBase64(string base64String)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64String);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static void RenameFile(string originalName, string newName) => File.Move(originalName, newName);

        public static void StartExternalProcess(string path)
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
    }
}