using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;

namespace EAPM
{
    public partial class MainWindow : MetroWindow
    {
        private List<PlayerProfile>? profiles;
        private PlayerProfile? selectedProfile;
        private List<SaveGame>? currentSaveGames;
        private SaveGame? selectedSaveGame;
        private SaveGameData? currentSaveData;

        public MainWindow()
        {
            InitializeComponent();
            LoadProfiles();
        }

        private async void LoadProfiles()
        {
            try
            {
                profiles = new List<PlayerProfile>();
                profiles.AddRange(PlayerProfile.GetEtsProfiles(game: "ets"));
                profiles.AddRange(PlayerProfile.GetEtsProfiles(game: "ats"));

                dgProfiles.ItemsSource = profiles;
                dgProfiles.Items.Refresh();
                
                statusBarText.Text = $"{profiles.Count} profiles loaded.";

                if (profiles.Count == 0)
                {
                    await this.ShowMessageAsync("No profiles found", 
                        "No profiles found. You should deactivate Steam Cloud synchronization in the game's profile settings.");
                }
            }
            catch (Exception ex)
            {
                statusBarText.Text = $"Error loading profiles: {ex.Message}";
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProfiles();
            ResetEditor();
        }

        private async void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("About EAPM", 
                "This software contains a decryption engine powered by SII_Decrypt by TheLazyTomcat, which is licensed under the Mozilla Public License 2.0. The underlying open-source source code for this specific module is available on GitHub.");
        }

        private void DgProfiles_Loaded(object sender, RoutedEventArgs e)
        {
            // Reset editor state
            ResetEditor();
        }

        private void ResetEditor()
        {
            selectedProfile = null;
            selectedSaveGame = null;
            currentSaveData = null;
            panelNoSelection.Visibility = Visibility.Visible;
            panelEditor.Visibility = Visibility.Collapsed;
            panelStats.Visibility = Visibility.Collapsed;
            panelButtons.Visibility = Visibility.Collapsed;
            
            if (rightPanel != null) rightPanel.Visibility = Visibility.Collapsed;
            if (separatorBorder != null) separatorBorder.Visibility = Visibility.Collapsed;
        }

        private void CmbSaveGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSaveGames.SelectedItem is SaveGame save)
            {
                selectedSaveGame = save;
                panelStats.Visibility = Visibility.Visible;
                panelButtons.Visibility = Visibility.Visible;
                LoadSaveData(save);
            }
            else
            {
                selectedSaveGame = null;
                panelStats.Visibility = Visibility.Collapsed;
                panelButtons.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadSaveData(SaveGame save)
        {
            try
            {
                statusBarText.Text = $"Loading stats for save '{save.DisplayName}'...";
                currentSaveData = SaveGameData.Load(save.Path);

                txtMoney.Text = currentSaveData.Money.ToString();
                txtXP.Text = currentSaveData.XP.ToString();

                // ADR Checks
                chkAdr1.IsChecked = (currentSaveData.Adr & 1) != 0;
                chkAdr2.IsChecked = (currentSaveData.Adr & 2) != 0;
                chkAdr3.IsChecked = (currentSaveData.Adr & 4) != 0;
                chkAdr4.IsChecked = (currentSaveData.Adr & 8) != 0;
                chkAdr5.IsChecked = (currentSaveData.Adr & 16) != 0;
                chkAdr6.IsChecked = (currentSaveData.Adr & 32) != 0;

                sldLongDist.Value = currentSaveData.LongDist;
                sldHeavyCargo.Value = currentSaveData.HeavyCargo;
                sldFragileCargo.Value = currentSaveData.FragileCargo;
                sldUrgentCargo.Value = currentSaveData.UrgentCargo;
                sldEcoDriving.Value = currentSaveData.EcoDriving;

                statusBarText.Text = $"Loaded stats for '{save.DisplayName}'.";
            }
            catch (Exception ex)
            {
                statusBarText.Text = $"Error loading save data: {ex.Message}";
                MessageBox.Show($"Failed to load save data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveStats_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSaveGame == null || currentSaveData == null) return;

            if (IsTrucksimRunning)
            {
                await this.ShowMessageAsync("Game is running", "Please exit the game before modifying save file statistics.");
                return;
            }

            try
            {
                // Parse inputs
                if (!long.TryParse(txtMoney.Text, out long money))
                {
                    await this.ShowMessageAsync("Invalid Input", "Please enter a valid numeric value for Money.");
                    return;
                }
                if (!int.TryParse(txtXP.Text, out int xp))
                {
                    await this.ShowMessageAsync("Invalid Input", "Please enter a valid numeric value for XP.");
                    return;
                }

                currentSaveData.Money = money;
                currentSaveData.XP = xp;

                // Reconstruct ADR bitmask
                int adrVal = 0;
                if (chkAdr1.IsChecked == true) adrVal |= 1;
                if (chkAdr2.IsChecked == true) adrVal |= 2;
                if (chkAdr3.IsChecked == true) adrVal |= 4;
                if (chkAdr4.IsChecked == true) adrVal |= 8;
                if (chkAdr5.IsChecked == true) adrVal |= 16;
                if (chkAdr6.IsChecked == true) adrVal |= 32;

                currentSaveData.Adr = adrVal;
                currentSaveData.LongDist = (int)sldLongDist.Value;
                currentSaveData.HeavyCargo = (int)sldHeavyCargo.Value;
                currentSaveData.FragileCargo = (int)sldFragileCargo.Value;
                currentSaveData.UrgentCargo = (int)sldUrgentCargo.Value;
                currentSaveData.EcoDriving = (int)sldEcoDriving.Value;

                // Write to file
                statusBarText.Text = $"Saving changes to '{selectedSaveGame.DisplayName}'...";
                SaveGameData.Save(selectedSaveGame.Path, currentSaveData);
                
                statusBarText.Text = $"Successfully updated stats for '{selectedSaveGame.DisplayName}'!";
                await this.ShowMessageAsync("Success", $"Stats updated successfully for save game '{selectedSaveGame.DisplayName}'.");
            }
            catch (Exception ex)
            {
                statusBarText.Text = $"Failed to save data: {ex.Message}";
                MessageBox.Show($"Failed to save changes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Profile Actions (Buttons)

        private async void ActionRename_Click(object sender, RoutedEventArgs e)
        {
            if (IsTrucksimRunning)
            {
                await this.ShowMessageAsync("Game is running", "Please exit the game before renaming a profile.");
                return;
            }

            if (sender is Button btn && btn.Tag is PlayerProfile profile)
            {
                string newusername = await this.ShowInputAsync("Rename Profile", 
                    $"Enter new name for profile '{profile.Username}':") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(newusername) || newusername == profile.Username)
                {
                    return;
                }

                // Check uniqueness
                if (profiles != null && profiles.Any(p => p.Username.Equals(newusername, StringComparison.OrdinalIgnoreCase) && p.EtsAts == profile.EtsAts))
                {
                    await this.ShowMessageAsync("Name Exists", $"A profile named '{newusername}' already exists for {profile.EtsAts}.");
                    return;
                }

                statusBarText.Text = $"Renaming profile '{profile.Username}' to '{newusername}'...";
                bool success = PlayerProfile.RenameProfile(profile, newusername);
                if (success)
                {
                    statusBarText.Text = $"Renamed profile to '{newusername}'.";
                    LoadProfiles();
                    ResetEditor();
                }
                else
                {
                    statusBarText.Text = "Rename failed. The folder might be locked by another process.";
                    await this.ShowMessageAsync("Error", "Failed to rename profile. Ensure no other applications are using the profile files.");
                }
            }
        }

        private async void ActionCopy_Click(object sender, RoutedEventArgs e)
        {
            if (IsTrucksimRunning)
            {
                await this.ShowMessageAsync("Game is running", "Please exit the game before copying a profile.");
                return;
            }

            if (sender is Button btn && btn.Tag is PlayerProfile toCopy)
            {
                string newusername = await this.ShowInputAsync("Copy Profile", "Enter the user name for the new profile:") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(newusername) || newusername == toCopy.Username)
                {
                    return;
                }

                if (profiles != null && profiles.Any(p => p.Username == newusername && p.EtsAts == toCopy.EtsAts))
                {
                    await this.ShowMessageAsync("Not Unique", $"The new user name must be unique. '{newusername}' is already in use.");
                    return;
                }

                statusBarText.Text = $"Copying profile '{toCopy.Username}' to '{newusername}'...";
                PlayerProfile.CopyProfile(toCopy, newusername);
                LoadProfiles();
                ResetEditor();
                statusBarText.Text = $"Successfully copied profile to '{newusername}'.";
            }
        }

        private async void ActionBackup_Click(object sender, RoutedEventArgs e)
        {
            if (IsTrucksimRunning)
            {
                await this.ShowMessageAsync("Game is running", "Please exit the game before backing up a profile.");
                return;
            }

            if (sender is Button btn && btn.Tag is PlayerProfile profile)
            {
                string profiledirectory = profile.Directory;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = $"{profile.DirectoryShort}-{DateTime.Now:yyyy-dd-M_HH-mm-ss}.zip",
                    DefaultExt = ".zip",
                    Filter = "Zip files (.zip)|*.zip",
                    Title = "Select Backup File Location",
                    InitialDirectory = Path.GetDirectoryName(profile.Directory)
                };

                bool? result = saveFileDialog.ShowDialog();
                if (result == true && !string.IsNullOrEmpty(saveFileDialog.FileName))
                {
                    statusBarText.Text = $"Backing up profile to {saveFileDialog.FileName}...";
                    PlayerProfile.BackupProfile(profile, saveFileDialog.FileName);
                    statusBarText.Text = $"Backup saved to '{Path.GetFileName(saveFileDialog.FileName)}'.";
                }
            }
        }

        private void ActionExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlayerProfile profile)
            {
                if (Directory.Exists(profile.Directory))
                {
                    Process.Start("explorer.exe", profile.Directory);
                    statusBarText.Text = $"Opened explorer in '{profile.DirectoryShort}'.";
                }
            }
        }

        private void ActionEditStats_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PlayerProfile profile)
            {
                // Select the profile
                dgProfiles.SelectedItem = profile;

                // Open the editor panel for this profile
                OpenProfileEditor(profile);
            }
        }

        private void OpenProfileEditor(PlayerProfile profile)
        {
            selectedProfile = profile;
            lblSelectedProfile.Text = profile.Username;
            lblSelectedDetails.Text = $"{profile.EtsAts} — Folder ID: {profile.DirectoryShort}";

            panelNoSelection.Visibility = Visibility.Collapsed;
            panelEditor.Visibility = Visibility.Visible;
            panelStats.Visibility = Visibility.Collapsed;
            panelButtons.Visibility = Visibility.Collapsed;

            if (rightPanel != null) rightPanel.Visibility = Visibility.Visible;
            if (separatorBorder != null) separatorBorder.Visibility = Visibility.Visible;

            // Load save games list
            currentSaveGames = SaveGame.GetSaveGames(profile.Directory);
            cmbSaveGames.ItemsSource = currentSaveGames;

            if (currentSaveGames != null && currentSaveGames.Count > 0)
            {
                cmbSaveGames.SelectedIndex = 0;
            }
            else
            {
                cmbSaveGames.SelectedIndex = -1;
                statusBarText.Text = $"No save games found for '{profile.Username}'.";
            }
        }

        private void HideEditor_Click(object sender, RoutedEventArgs e)
        {
            dgProfiles.SelectedIndex = -1;
            ResetEditor();
        }

        private async void ActionDelete_Click(object sender, RoutedEventArgs e)
        {
            if (IsTrucksimRunning)
            {
                await this.ShowMessageAsync("Game is running", "Please exit the game before deleting a profile.");
                return;
            }

            if (sender is Button btn && btn.Tag is PlayerProfile toDelete)
            {
                MessageDialogResult result = await this.ShowMessageAsync("Confirm Deletion",
                    $"Are you sure you want to permanently delete the profile '{toDelete.Username}'?", 
                    MessageDialogStyle.AffirmativeAndNegative);

                if (result == MessageDialogResult.Affirmative)
                {
                    bool delresult = PlayerProfile.DeleteProfile(toDelete);
                    if (!delresult)
                    {
                        await this.ShowMessageAsync("Delete Failed", 
                            "Failed to delete the profile directory. Ensure no files are currently open or locked.");
                        return;
                    }
                    LoadProfiles();
                    ResetEditor();
                    statusBarText.Text = $"Profile '{toDelete.Username}' deleted.";
                }
            }
        }

        #endregion

        private static bool IsTrucksimRunning
        {
            get
            {
                Process[] localAll = Process.GetProcesses();
                foreach (Process p in localAll)
                {
                    string name = p.ProcessName.ToLower();
                    if (name.Contains("eurotrucks2") || name.Contains("amtrucks"))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}");
            }
        }
    }

    #region Value Converters

    public class SimColorConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string game = value?.ToString()?.ToUpper() ?? "";
            if (game == "ETS")
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1D3E5E"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F2B1D"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class SimBorderColorConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string game = value?.ToString()?.ToUpper() ?? "";
            if (game == "ETS")
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00B6FF"));
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7B00"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    #endregion
}