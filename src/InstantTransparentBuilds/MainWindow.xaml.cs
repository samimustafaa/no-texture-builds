using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Reflection;
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
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromMinutes(5) };
    private static readonly Version AppVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 2, 0);

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
        if (sender is Button button) button.IsEnabled = false;
        StatusLabel.Text = "Checking official GitHub Releases...";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/samimustafaa/no-texture-builds/releases/latest");
            request.Headers.UserAgent.ParseAdd("InstantTransparentBuilds/0.2");
            using var response = await client.SendAsync(request);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            { StatusLabel.Text = "No published release yet. Download builds from GitHub Actions."; return; }
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            string tag = root.GetProperty("tag_name").GetString() ?? "";
            if (!Version.TryParse(tag.TrimStart('v', 'V'), out var latest))
            { StatusLabel.Text = "Release version could not be parsed."; return; }
            if (latest <= AppVersion)
            { StatusLabel.Text = "Already up to date (v" + AppVersion.ToString(3) + ")."; return; }

            var assets = root.GetProperty("assets").EnumerateArray().ToArray();
            string? GetUrl(string name) => assets.FirstOrDefault(a => a.GetProperty("name").GetString() == name)
                .TryGetProperty("browser_download_url", out var url) ? url.GetString() : null;
            var zipUrl = GetUrl("InstantTransparentBuilds-win-x64.zip");
            var checksumUrl = GetUrl("InstantTransparentBuilds-win-x64.zip.sha256");
            if (zipUrl == null || checksumUrl == null)
            { StatusLabel.Text = "Update package/checksum missing from release."; return; }
            if (MessageBox.Show("Install official update " + tag + " now?\nThe application will restart after updating.",
                "Install update", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            { StatusLabel.Text = "Update skipped."; return; }

            var stageDir = Path.Combine(Path.GetTempPath(), "ITB-update-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(stageDir);
            var zipPath = Path.Combine(stageDir, "package.zip");
            StatusLabel.Text = "Downloading update...";
            using (var source = await client.GetStreamAsync(zipUrl))
            using (var dest = File.Create(zipPath)) await source.CopyToAsync(dest);
            var checksum = (await client.GetStringAsync(checksumUrl)).Split(' ', '\n', '\r', '\t')
                .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim() ?? "";
            var actual = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(zipPath)));
            if (!string.Equals(actual, checksum, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update checksum verification failed.");

            using var zip = ZipFile.OpenRead(zipPath);
            var appEntry = zip.Entries.SingleOrDefault(x => x.FullName == "InstantTransparentBuilds.exe");
            var helperEntry = zip.Entries.SingleOrDefault(x => x.FullName == "InstantTransparentBuilds.Updater.exe");
            if (appEntry == null || helperEntry == null) throw new InvalidDataException("Release archive lacks executable files.");
            var stagedApp = Path.Combine(stageDir, "InstantTransparentBuilds.exe");
            var stagedUpdater = Path.Combine(stageDir, "InstantTransparentBuilds.Updater.exe");
            appEntry.ExtractToFile(stagedApp);
            helperEntry.ExtractToFile(stagedUpdater);
            var installedApp = Environment.ProcessPath ?? throw new InvalidOperationException("Cannot locate current application.");
            StatusLabel.Text = "Installing update and restarting...";
            Process.Start(new ProcessStartInfo(stagedUpdater)
            {
                UseShellExecute = true,
                ArgumentList = { Process.GetCurrentProcess().Id.ToString(), installedApp, stagedApp }
            });
            Application.Current.Shutdown();
        }
        catch (Exception ex) { StatusLabel.Text = "Update failed: " + ex.Message; }
        finally { if (sender is Button button) button.IsEnabled = true; }
    }
}
