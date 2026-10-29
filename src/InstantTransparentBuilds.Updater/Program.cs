using System.Diagnostics;
using System.IO;

if (args.Length != 3 || !int.TryParse(args[0], out var pid)) return;
try
{
    var destination = Path.GetFullPath(args[1]);
    var staged = Path.GetFullPath(args[2]);
    if (!File.Exists(staged) || Path.GetFileName(destination) != "InstantTransparentBuilds.exe"
        || Path.GetFileName(staged) != "InstantTransparentBuilds.exe") return;

    try
    {
        var parent = Process.GetProcessById(pid);
        await parent.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(60)).Token);
    }
    catch (ArgumentException) { /* Parent already closed. */ }

    var oldBackup = destination + ".previous";
    for (int retry = 0; retry < 30; retry++)
    {
        try
        {
            if (File.Exists(oldBackup)) File.Delete(oldBackup);
            if (File.Exists(destination)) File.Move(destination, oldBackup);
            try
            {
                File.Copy(staged, destination, overwrite: false);
            }
            catch
            {
                if (File.Exists(oldBackup)) File.Move(oldBackup, destination, overwrite: true);
                throw;
            }
            Process.Start(new ProcessStartInfo(destination) { UseShellExecute = true });
            return;
        }
        catch (IOException) when (retry < 29) { await Task.Delay(1000); }
    }
}
catch (Exception ex)
{
    var log = Path.Combine(Path.GetTempPath(), "InstantTransparentBuilds-update-error.txt");
    try { await File.WriteAllTextAsync(log, ex.ToString()); } catch { }
}
