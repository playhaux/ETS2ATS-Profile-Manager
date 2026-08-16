
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using EAPM.Properties;

namespace EAPM
{
    internal class PlayerProfile
    {
        private string? _username;
        private string? _directory;
        private string? _directoryshort;
        private bool _decrypted = false;
        private string? _etsats;
        private string? _lastaccess;

        public string Directory
        {
            get => _directory ?? "none";
            set => _directory = value;
        }

        public string DirectoryShort
        {
            get => _directoryshort ?? "none";
            set => _directoryshort = value;
        }
        public string Username
        {
            get => _username ?? "none";
            set => _username = value;
        }

        public bool Decrypted
        {
            get => _decrypted;
            set => _decrypted = value;
        }

        public string EtsAts
        {
            get => _etsats ?? "none";
            set => _etsats = value;
        }

        public string? LastAccess
        {
            get => _lastaccess ?? null;
            set => _lastaccess = value;
        }

        public static List<PlayerProfile> GetEtsProfiles(string game="ets")
        {
            List<PlayerProfile> pf = new();
            string profiledirectory = string.Empty;
            if (game == "ets")
            {
                profiledirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"Euro Truck Simulator 2\profiles");
            }
            else
            {
                profiledirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"American Truck Simulator\profiles");
            }
            
            if(System.IO.Directory.Exists(profiledirectory))
            {
                string[] profilesubdirectories = System.IO.Directory.GetDirectories(profiledirectory);
                if(profilesubdirectories.Length > 0)
                {
                    foreach (string subdirectory in profilesubdirectories)
                    {
                        DirectoryInfo di =new(subdirectory);
                        string shortdir = di.Name;
                        if (shortdir.IsHex() & File.Exists(Path.Combine(subdirectory, "profile.sii")))
                        {
                            string profileSiiPath = Path.Combine(subdirectory, "profile.sii");
                            bool decrypted = IsDecrypted(profileSiiPath);
                            if (!decrypted)
                            {
                                decrypted = DecryptFile(subdirectory, "profile.sii");
                            }

                            PlayerProfile p = new()
                            {
                                Directory = subdirectory,
                                DirectoryShort = shortdir,
                                Decrypted = decrypted,
                                EtsAts = game.ToUpper(),
                                Username = subdirectory.DirectoryToScsUsername(),
                                LastAccess = di.LastWriteTime.ToString(),
                            };
                            pf.Add(p);
                        }
                    }
                }
            }
            return pf;
        }

        public static void CopyProfile(PlayerProfile profile, string newusername)
        {
            string newDirectoryShort = newusername.ScsUsernameToDirectory();

            string profileDirectoryBase;
            if (profile.EtsAts.ToLower() == "ets")
            {
                profileDirectoryBase = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"Euro Truck Simulator 2\profiles");
            }
            else
            {
                profileDirectoryBase = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                @"American Truck Simulator\profiles");
            }
            string newDirectoryFull = Path.Combine(profileDirectoryBase, newDirectoryShort);
            if (System.IO.Directory.Exists(newDirectoryFull))
            {
                return;
            }
            else
            {
                // Copy profile directory to new directory
                CopyDirectory(profile.Directory, newDirectoryFull, true);
                if (!profile.Decrypted)
                {
                    bool result = DecryptFile(newDirectoryFull, "profile.sii");
                    if (!result) 
                    {
                        System.IO.Directory.Delete(newDirectoryFull, true);
                        return;
                    }
                }
                string filename = Path.Combine(newDirectoryFull, "profile.sii");
                using StreamReader sr = new(filename);
                string content = sr.ReadToEnd();
                sr.Close();
                if (content.Contains(profile.Username))
                {
                    content = content.Replace(profile.Username, newusername);
                }
                using StreamWriter sw = new StreamWriter(filename, false);
                sw.Write(content);
                sw.Close();
            }
        }

        // Delete a profile directory
        public static bool DeleteProfile(PlayerProfile profile)
        {
            try
            {
                System.IO.Directory.Delete(profile.Directory, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Save a profile directory to a zip file
        public static void BackupProfile(PlayerProfile profile, string zipfilename)
        {
            string startPath = profile.Directory;
            string zipPath = zipfilename;
            ZipFile.CreateFromDirectory(startPath, zipPath);
        }

        // Check the first line of a file to detect if it is encrypted or not
        private static bool IsDecrypted(string filename)
        {
            using StreamReader sr = new(filename);
            string text = sr.ReadLine() ?? string.Empty;
            sr.Close();
            return text.Contains("SiiNunit");
        }

        // Use SII_Decrypt.exe for decrypting sii files
        public static bool DecryptFile(string directory, string filename)
        {
            string tempExeName = Path.Combine(Path.GetTempPath(), 
                "SII_Decrypt.exe");

            if (!File.Exists(tempExeName))
            {
                using FileStream fileStream = new(tempExeName, 
                    FileMode.CreateNew, 
                    FileAccess.Write);
                byte[] bytes = Resources.GetSII_Decrypt();

                fileStream.Write(bytes, 0, bytes.Length);
                fileStream.Close();
                if (!File.Exists(tempExeName))
                {
                    NtfyUsage.SendUsage("SII_Decrypt.exe", $"Write file {tempExeName} failed.");
                    return false;
                }
            }

            using Process process = new();
            process.StartInfo.FileName = tempExeName;
            process.StartInfo.Arguments = filename;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = directory;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains("Result: File is a plain-text SII (1)") ^
                output.Contains("Result: Success (0)");
        }

        // https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            DirectoryInfo dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            System.IO.Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        // Rename a profile directory
        public static bool RenameProfile(PlayerProfile profile, string newusername)
        {
            string newDirectoryShort = newusername.ScsUsernameToDirectory();
            string profileDirectoryBase = Path.GetDirectoryName(profile.Directory)!;
            string newDirectoryFull = Path.Combine(profileDirectoryBase, newDirectoryShort);

            if (System.IO.Directory.Exists(newDirectoryFull))
            {
                return false;
            }

            try
            {
                System.IO.Directory.Move(profile.Directory, newDirectoryFull);

                if (!profile.Decrypted)
                {
                    bool result = DecryptFile(newDirectoryFull, "profile.sii");
                    if (!result)
                    {
                        // Revert directory move
                        System.IO.Directory.Move(newDirectoryFull, profile.Directory);
                        return false;
                    }
                }

                string filename = Path.Combine(newDirectoryFull, "profile.sii");
                string content = File.ReadAllText(filename);
                if (content.Contains(profile.Username))
                {
                    content = content.Replace(profile.Username, newusername);
                }
                File.WriteAllText(filename, content);

                profile.Directory = newDirectoryFull;
                profile.DirectoryShort = newDirectoryShort;
                profile.Username = newusername;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class SaveGame
    {
        public string Path { get; set; } = string.Empty;
        public string FolderName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LastWriteTime { get; set; } = string.Empty;

        public static List<SaveGame> GetSaveGames(string profileDirectory)
        {
            List<SaveGame> saves = new();
            string savePath = System.IO.Path.Combine(profileDirectory, "save");
            if (System.IO.Directory.Exists(savePath))
            {
                foreach (string dir in System.IO.Directory.GetDirectories(savePath))
                {
                    var di = new DirectoryInfo(dir);
                    string folderName = di.Name;

                    if (File.Exists(System.IO.Path.Combine(dir, "game.sii")) && File.Exists(System.IO.Path.Combine(dir, "info.sii")))
                    {
                        string displayName = ParseSaveName(dir, folderName);
                        saves.Add(new SaveGame
                        {
                            Path = dir,
                            FolderName = folderName,
                            DisplayName = displayName,
                            LastWriteTime = di.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }
                }
            }
            return saves.OrderByDescending(s => s.LastWriteTime).ToList();
        }

        private static string ParseSaveName(string saveDir, string folderName)
        {
            string infoFile = System.IO.Path.Combine(saveDir, "info.sii");
            if (!File.Exists(infoFile)) return folderName;

            try
            {
                bool isDecrypted = IsFileDecrypted(infoFile);
                if (!isDecrypted)
                {
                    PlayerProfile.DecryptFile(saveDir, "info.sii");
                }

                string content = File.ReadAllText(infoFile);
                var match = System.Text.RegularExpressions.Regex.Match(content, @"\bname:\s*""?([^""\r\n]+)""?");
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    string nameVal = match.Groups[1].Value.Trim();
                    if (!nameVal.StartsWith("@@"))
                    {
                        return nameVal;
                    }
                }
            }
            catch { }

            if (folderName.Equals("autosave", StringComparison.OrdinalIgnoreCase)) return "Auto Save";
            if (folderName.Equals("autosave_drive", StringComparison.OrdinalIgnoreCase)) return "Auto Save (Drive)";
            if (folderName.Equals("quicksave", StringComparison.OrdinalIgnoreCase)) return "Quick Save";

            return $"Save Slot {folderName}";
        }

        private static bool IsFileDecrypted(string filename)
        {
            using StreamReader sr = new(filename);
            string text = sr.ReadLine() ?? string.Empty;
            sr.Close();
            return text.Contains("SiiNunit");
        }
    }

    public class SaveGameData
    {
        public long Money { get; set; }
        public int XP { get; set; }
        public int Adr { get; set; }
        public int LongDist { get; set; }
        public int HeavyCargo { get; set; }
        public int FragileCargo { get; set; }
        public int UrgentCargo { get; set; }
        public int EcoDriving { get; set; }

        public static SaveGameData Load(string saveDir)
        {
            string gameFile = Path.Combine(saveDir, "game.sii");
            if (!IsFileDecrypted(gameFile))
            {
                PlayerProfile.DecryptFile(saveDir, "game.sii");
            }

            string content = File.ReadAllText(gameFile);
            SaveGameData data = new();

            var moneyMatch = System.Text.RegularExpressions.Regex.Match(content, @"\bmoney_account:\s*(-?\d+)");
            if (moneyMatch.Success)
            {
                data.Money = long.Parse(moneyMatch.Groups[1].Value);
            }

            var xpMatch = System.Text.RegularExpressions.Regex.Match(content, @"\bexperience_points:\s*(\d+)");
            if (xpMatch.Success)
            {
                data.XP = int.Parse(xpMatch.Groups[1].Value);
            }

            var economyMatch = System.Text.RegularExpressions.Regex.Match(content, @"\beconomy\s*:\s*_nameless\.[^\n{]+\{([^}]+)\}");
            string economyBlock = economyMatch.Success ? economyMatch.Groups[1].Value : content;

            data.Adr = GetSkillValue(economyBlock, "adr");
            data.LongDist = GetSkillValue(economyBlock, "long_dist");
            data.HeavyCargo = GetSkillValue(economyBlock, "heavy");
            data.FragileCargo = GetSkillValue(economyBlock, "fragile");
            data.UrgentCargo = GetSkillValue(economyBlock, "urgent");
            data.EcoDriving = GetSkillValue(economyBlock, "mechanical");

            return data;
        }

        private static int GetSkillValue(string block, string key)
        {
            var match = System.Text.RegularExpressions.Regex.Match(block, $@"\b{key}:\s*(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        public static void Save(string saveDir, SaveGameData data)
        {
            string gameFile = Path.Combine(saveDir, "game.sii");
            if (!IsFileDecrypted(gameFile))
            {
                PlayerProfile.DecryptFile(saveDir, "game.sii");
            }

            string content = File.ReadAllText(gameFile);

            content = System.Text.RegularExpressions.Regex.Replace(content, @"\bmoney_account:\s*-?\d+", $"money_account: {data.Money}");
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\bexperience_points:\s*\d+", $"experience_points: {data.XP}");

            var economyMatch = System.Text.RegularExpressions.Regex.Match(content, @"\beconomy\s*:\s*(_nameless\.[^\n{]+)\{([^}]+)\}");
            if (economyMatch.Success)
            {
                string economyHeader = economyMatch.Groups[1].Value;
                string economyBlock = economyMatch.Groups[2].Value;

                economyBlock = ReplaceSkillValue(economyBlock, "adr", data.Adr);
                economyBlock = ReplaceSkillValue(economyBlock, "long_dist", data.LongDist);
                economyBlock = ReplaceSkillValue(economyBlock, "heavy", data.HeavyCargo);
                economyBlock = ReplaceSkillValue(economyBlock, "fragile", data.FragileCargo);
                economyBlock = ReplaceSkillValue(economyBlock, "urgent", data.UrgentCargo);
                economyBlock = ReplaceSkillValue(economyBlock, "mechanical", data.EcoDriving);

                string oldEconomyBlock = economyMatch.Value;
                string newEconomyBlock = $"economy : {economyHeader}{{{economyBlock}}}";
                
                content = content.Replace(oldEconomyBlock, newEconomyBlock);
            }
            else
            {
                content = ReplaceSkillValue(content, "adr", data.Adr);
                content = ReplaceSkillValue(content, "long_dist", data.LongDist);
                content = ReplaceSkillValue(content, "heavy", data.HeavyCargo);
                content = ReplaceSkillValue(content, "fragile", data.FragileCargo);
                content = ReplaceSkillValue(content, "urgent", data.UrgentCargo);
                content = ReplaceSkillValue(content, "mechanical", data.EcoDriving);
            }

            File.WriteAllText(gameFile, content);
        }

        private static string ReplaceSkillValue(string block, string key, int value)
        {
            return System.Text.RegularExpressions.Regex.Replace(block, $@"\b{key}:\s*\d+", $"{key}: {value}");
        }

        private static bool IsFileDecrypted(string filename)
        {
            using StreamReader sr = new(filename);
            string text = sr.ReadLine() ?? string.Empty;
            sr.Close();
            return text.Contains("SiiNunit");
        }
    }
}
