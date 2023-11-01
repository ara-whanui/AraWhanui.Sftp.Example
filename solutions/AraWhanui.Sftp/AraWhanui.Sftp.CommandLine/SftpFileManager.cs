using Renci.SshNet;
using Spectre.Console;

namespace AraWhanui.Sftp.CommandLine;

public static class SftpFileManager
{
    public static IEnumerable<string> ListFiles(SftpClient sftp, string remoteDirectory)
    {
        var files = sftp.ListDirectory(remoteDirectory);
        foreach (var file in files.OrderBy(x => x.FullName))
        {
            if (!file.IsDirectory && !file.IsSymbolicLink)
            {
                yield return file.Name;
            }
        }
    }

    public static void DownloadFile(SftpClient sftp, string remoteDirectory, string localDirectory, string fileName)
    {
        var remoteFileName = $"{remoteDirectory}/{fileName}";
        var localFileName = Path.Combine(localDirectory, fileName);
        using var fileStream = new FileStream(localFileName, FileMode.Create);
        AnsiConsole.MarkupLine($"[green]Downloading {remoteFileName}...[/]");
        sftp.DownloadFile(remoteFileName, fileStream);
        AnsiConsole.MarkupLine($"[green]Downloaded to {localFileName}.[/]");
    }
}
