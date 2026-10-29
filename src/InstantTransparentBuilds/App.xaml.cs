using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace InstantTransparentBuilds;
public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 3 && e.Args[0] == "--apply-update")
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            try { await Task.Run(() => ApplyUpdate(e.Args[1], e.Args[2])); }
            catch (Exception ex)
            {
                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "InstantTransparentBuilds-update-error.txt"), ex.ToString()); } catch { }
            }
            Shutdown();
            return;
        }
        new MainWindow().Show();
    }

    private static void ApplyUpdate(string pidText, string target)
    {
        if (!int.TryParse(pidText, out int pid)) return;
        string current = Environment.ProcessPath ?? throw new IOException("Executable path missing.");
        string destination = Path.GetFullPath(target);
        if (!string.Equals(Path.GetFileName(destination), "InstantTransparentBuilds.exe", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetFileName(current), "InstantTransparentBuilds.exe", StringComparison.OrdinalIgnoreCase)
            || string.Equals(current, destination, StringComparison.OrdinalIgnoreCase)) return;
        try { Process.GetProcessById(pid).WaitForExit(60000); } catch (ArgumentException) { }
        string backup = destination + ".previous";
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                if (File.Exists(backup)) File.Delete(backup);
                if (File.Exists(destination)) File.Move(destination, backup);
                try { File.Copy(current, destination); }
                catch
                {
                    if (File.Exists(backup)) File.Move(backup, destination, true);
                    throw;
                }
                Process.Start(new ProcessStartInfo(destination) { UseShellExecute = true });
                return;
            }
            catch (IOException) when (attempt < 19) { System.Threading.Thread.Sleep(1000); }
        }
    }
}
