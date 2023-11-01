using AraWhanui.Sftp.CommandLine;
using Renci.SshNet;
using Spectre.Console;

// Change these values.
const string host = "sftp.example.com";
const int port = 22;
const string username = "user-one";
const string remoteDirectory = "/upload";
const string localDirectory = "C:\\tmp";
const string privateKeyPath = "C:\\keys\\openssh-format-private-key";

if (Directory.Exists(localDirectory) == false)
{
    Directory.CreateDirectory(localDirectory);
}

if (File.Exists(privateKeyPath) == false)
{
    throw new Exception($"Unable to locate private key at '{privateKeyPath}'. Aborting.");
}

PrivateKeyFile? keyFile = null;
const string badPassphraseSubstring = "The random check bytes of the OpenSSH key do not match";
const string emptyPassphraseSubstring = "passphrase is empty";
// Attempt to first load private key, also checking if a passphrase is required. 
try
{
    keyFile = new PrivateKeyFile(privateKeyPath);
    AnsiConsole.MarkupLine($"[green]No passphrase, private key loaded.[/]");
}
catch (Renci.SshNet.Common.SshException ex) when (ex.Message.Contains(emptyPassphraseSubstring))
{
    AnsiConsole.MarkupLine($"[yellow]Passphrase required.[/]");
}

// if the keyFile wasn't loaded without a passphrase, get user to enter it.
if (keyFile == null)
{
    const int maxPassphraseAttempts = 3;
    AnsiConsole.MarkupLine(
        $"Enter the passphrase for the private key...");
    for (var i = 0; i < maxPassphraseAttempts; i++)
    {
        var attemptNumber = i + 1;
        var passphrase = AnsiConsole.Prompt(
            new TextPrompt<string>($"[yellow]  Attempt {attemptNumber}/{maxPassphraseAttempts}[/]:")
                .PromptStyle("green")
                .Secret());

        try
        {
            keyFile = new PrivateKeyFile(privateKeyPath, passphrase);

            // no exception was thrown, passphrase worked, key is loaded, break out of the prompt loop...
            break;
        }
        catch (Renci.SshNet.Common.SshException ex) when (ex.Message.Contains(badPassphraseSubstring))
        {
            // Passphrase failed
            var pleaseTryAgain = attemptNumber == maxPassphraseAttempts ? "" : $" Please try again...";
            AnsiConsole.MarkupLine(
                $"[red]  Incorrect passphrase.[/][yellow]{pleaseTryAgain}[/]");
        }
    }

    if (keyFile == null)
    {
        AnsiConsole.MarkupLine("\n[red]Aborting due to too many incorrect attempts.[/]");
        return;
    }
}

IPrivateKeySource[] keyFiles = { keyFile };
var methods = new AuthenticationMethod[] { new PrivateKeyAuthenticationMethod(username, keyFiles) };
var conInfo = new ConnectionInfo(host, port, username, methods);

using var sftp = new SftpClient(conInfo);

while (true)
{
    var mainOption = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Choose an option")
            .AddChoices(new[] { "Download Files", "Exit" }));

    switch (mainOption)
    {
        case "Download Files":
            sftp.Connect();
            var files = SftpFileManager.ListFiles(sftp, remoteDirectory).ToList();
            sftp.Disconnect();

            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No files available for download.[/]");
                continue;
            }

            var selectedFile = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a file to download")
                    .PageSize(10)
                    .AddChoices(files));

            sftp.Connect();
            SftpFileManager.DownloadFile(sftp, remoteDirectory, localDirectory, selectedFile);
            sftp.Disconnect();
            break;

        case "Exit":
            return;
    }
}