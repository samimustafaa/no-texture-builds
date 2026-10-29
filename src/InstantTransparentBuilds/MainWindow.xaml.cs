using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace InstantTransparentBuilds;
public partial class MainWindow : Window
{
    private readonly string[] colors = { "#B7BECE", "#37D7CC", "#4393FF", "#D15CE8" };
    private readonly string[] labels = { "silver", "cyan", "blue", "purple" };
    private readonly string backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sg0hann", "InstantTransparentBuilds", "Backups");
    private int currentLevel;
    private string? gameFolder;
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(12) };

    public MainWindow() { InitializeComponent(); RefreshPreview(); }
    private void Level_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Tag?.ToString(), out var n)) { currentLevel = n; RefreshPreview(); }
    }
    private void OpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BuildFace != null) RefreshPreview();
    }
    private void RefreshPreview()
    {
        var color = (Color)ColorConverter.ConvertFromString(colors[currentLevel]);
        BuildFace.Fill = new SolidColorBrush(color);
        BuildSide.Fill = new SolidColorBrush(color);
        var opacity = OpacitySlider.Value / 100;
        BuildFace.Opacity = opacity;
        BuildSide.Opacity = opacity;
        PreviewLabel.Text = $"Lv.{currentLevel + 1} · Transparent {labels[currentLevel]} · {OpacitySlider.Value:0}% opacity";
    }
    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = "Select the CD Revive folder" };
        if (dialog.ShowDialog() == Forms.DialogResult.OK) { gameFolder = dialog.SelectedPath; GamePathLabel.Text = gameFolder; StatusLabel.Text = "Game folder selected (read-only)."; }
    }
    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        if (gameFolder == null || !Directory.Exists(gameFolder)) { MessageBox.Show("Select a game folder first."); return; }
        Process.Start(new ProcessStartInfo { FileName = gameFolder, UseShellExecute = true });
    }
    private void Backup_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Title = "Select a file to back up (no game file will be changed)" };
        if (picker.ShowDialog() != true) return;
        try
        {
            Directory.CreateDirectory(backupDir);
            string safeName = Path.GetFileName(picker.FileName);
            string destination = Path.Combine(backupDir, DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + safeName);
            File.Copy(picker.FileName, destination, overwrite: false);
            StatusLabel.Text = "Backup saved: " + Path.GetFileName(destination);
        }
        catch (Exception ex) { MessageBox.Show("Backup failed: " + ex.Message); }
    }
    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(backupDir);
        var picker = new OpenFileDialog { InitialDirectory = backupDir, Title = "Select your saved backup file" };
        if (picker.ShowDialog() != true) return;
        if (!Path.GetFullPath(picker.FileName).StartsWith(Path.GetFullPath(backupDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        { MessageBox.Show("Select a backup from the app's backup directory."); return; }
        var saver = new SaveFileDialog { Title = "Restore as (choose a safe location)", FileName = Path.GetFileName(picker.FileName) };
        if (saver.ShowDialog() != true) return;
        if (MessageBox.Show("Restore selected backup to:\n" + saver.FileName + "\n\nOverwrite existing file if present?", "Confirm restore", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { File.Copy(picker.FileName, saver.FileName, overwrite: true); StatusLabel.Text = "Backup restored successfully."; }
        catch (Exception ex) { MessageBox.Show("Restore failed: " + ex.Message); }
    }
    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        StatusLabel.Text = "Checking GitHub Releases...";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/samimustafaa/no-texture-builds/releases/latest");
            request.Headers.UserAgent.ParseAdd("InstantTransparentBuilds/0.1.0");
            using var response = await client.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) { StatusLabel.Text = "No published release yet."; return; }
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            string page = doc.RootElement.GetProperty("html_url").GetString() ?? "https://github.com/samimustafaa/no-texture-builds/releases";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latest)) { StatusLabel.Text = "Latest release: " + tag; return; }
            var current = new Version(0, 1, 0);
            if (latest > current)
            {
                StatusLabel.Text = "Update available: " + tag;
                if (MessageBox.Show("A newer version " + tag + " is available. Open the official GitHub release to download it?", "Update available", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(page) { UseShellExecute = true });
            }
            else StatusLabel.Text = "You're up to date (v0.1.0).";
        }
        catch (Exception ex) { StatusLabel.Text = "Update check failed: " + ex.Message; }
    }
}
