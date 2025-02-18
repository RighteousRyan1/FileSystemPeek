using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FileSynchronizer;
public class FileSynchronizer {
    public static float PercentFetched;
    public static Folder FolderOnOtherSystem;

    public static FileContents LastFetchedFile;
    public static byte[] LastFetchedFileBytes;

    public static Client client;
    public static Server server;

    public static bool runLoop;
    static void Main() {
        var hasIpFolder = File.Exists("ip.txt");
        string ip;

        if (!hasIpFolder) {
            Console.Write("Please open with a base IP: ");
            ip = Console.ReadLine()!;
        }
        else
            ip = File.ReadAllText("ip.txt");
        Console.Write("Is this the host computer? (y/n) ");
        var isHost = Console.ReadLine()!.Equals("y", StringComparison.CurrentCultureIgnoreCase);

        client = new(isHost ? 0 : 1);

        Console.Title = "File Synchronizer Client PC";

        if (isHost) {
            server = new() {
                Password = string.Empty,
                Address = ip,
                Name = "FileSharer"
            };
            server.Start(1234, string.Empty);
            Console.Title = "File Synchronizer Host PC";
        }
        client.AttemptConnectionTo(ip, 1234, string.Empty);

        // Thread.Sleep(500);

        if (client.netPeer is not null) {
            Console.WriteLine(isHost ? "Server started." : "Connection successful.");
            Packet.SendString(client.netPeer, $"'{Environment.MachineName}' has connected.");
        }

        var quitPoll = false;
        /*Task.Run(async () => {
            while (!quitPoll) {
                try {
                    client.Poll();
                    server?.Poll();
                    await Task.Delay(5);
                } catch (Exception e) {
                    Console.WriteLine($"Error polling events: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        });*/


        // TODO: get file fetching working (test first tho)

        Console.WriteLine("List of commands and their subcommands");
        Console.WriteLine("help (-dl)");
        Console.WriteLine("mf (-u <folder>, -d)");
        Console.WriteLine("reqsfolder <sfolder>");
        Console.WriteLine("quit");

        runLoop = true;

        Task.Run(() => {
            while (runLoop) {
                SubmitCmd(Console.ReadLine()!);
            }
        });
        while (!quitPoll) {
            client.Poll();
            server?.Poll();
        }
        server.netManager.Stop(true);
    }

    public static void SubmitCmd(string command) {
        try {
            var cmdArgs = command.Split(' ');

            var cmd = cmdArgs[0];
            var subcmd = cmdArgs[1];

            switch (cmd) {
                case "help":
                    switch (subcmd) {
                        // "directory list"
                        case "-dl":
                            var names = Utils.AllSpecialFolders;

                            string strToDisplay = string.Empty;
                            for (int i = 0; i < names.Length; i++) {
                                strToDisplay += names[i] + ", ";
                                if (i != 0 && i % 4 == 0)
                                    strToDisplay += "\n";
                            }
                            Console.WriteLine(strToDisplay);
                            break;
                    }
                    break;
                // "move file"
                case "mf":
                    switch (subcmd) {
                        // "up"
                        case "-u":
                            var folderToGoTo = string.Join(' ', cmdArgs[2..]);

                            bool success = false;

                            for (int i = 0; i < FolderOnOtherSystem.SubFolders.Length; i++) {
                                var curSubFolder = FolderOnOtherSystem.SubFolders[i];
                                var folderName = Path.GetFileName(curSubFolder.FolderPath);
                                if (folderName == folderToGoTo) {
                                    FolderOnOtherSystem = curSubFolder;
                                    Packet.RequestFolder(client.netPeer!, curSubFolder.FolderPath);
                                    success = true;
                                    break;
                                }
                            }
                            if (!success) {
                                Console.WriteLine("No folder exists as a subdirectory with name '" + folderToGoTo + "'.");
                            }
                            break;
                        // "down
                        case "-d":
                            Packet.RequestFolder(client.netPeer!, Directory.GetParent(FolderOnOtherSystem.FolderPath)!.FullName);
                            break;
                    }
                    break;
                // "request folder -special""
                case "reqfolder":
                    // for readability

                    switch (subcmd) {
                        case "-s":
                            var folderToReq = string.Join(' ', cmdArgs[2..]);
                            var pathNames = Utils.AllSpecialFolders;

                            bool nameSuccess = false;
                            for (int i = 0; i < pathNames.Length; i++) {
                                var name = pathNames[i];
                                if (folderToReq == name) {
                                    nameSuccess = true;
                                    Packet.RequestSFolder(client.netPeer!, Enum.GetValues<Environment.SpecialFolder>()[i]);
                                    break;
                                }
                            }
                            if (!nameSuccess)
                                Console.WriteLine("There is no special folder with that name. Please use 'help -dl' for more info.");
                            break;
                        default:
                            Packet.RequestFolder(client.netPeer!, string.Join(' ', cmdArgs[1..]));
                            break;
                    }
                    break;
                case "reqfile":
                    var fileWeWant = string.Join(' ', cmdArgs[1..]);

                    bool fileSuccess = false;

                    if (FolderOnOtherSystem is null || FolderOnOtherSystem.Files is null) {
                        Console.WriteLine("Cannot request file as there is no folder loaded from the other system.");
                        break;
                    }

                    for (int i = 0; i < FolderOnOtherSystem.Files.Length; i++) {
                        var curFile = FolderOnOtherSystem.Files[i];
                        var fileName = curFile.FileName;
                        if (fileName == fileWeWant) {
                            Console.WriteLine($"Requesting file '{fileName}'");
                            Packet.RequestFile(client.netPeer!, curFile.FilePath);
                            LastFetchedFile = curFile;
                            fileSuccess = true;
                            break;
                        }
                    }

                    if (!fileSuccess)
                        Console.WriteLine($"There is no file with name '{fileWeWant}' within the current folder.");
                    break;
                case "quit":
                    runLoop = false;
                    break;
                default:
                    Console.WriteLine("Unknown command.");
                    break;
            }
        }
        catch {
            Console.WriteLine("Error in command parsing.");
        }
    }
    
    public static void FinalizeFileFetch() {
        Console.WriteLine($"Done! Saved to 'FileSynchronizer/{LastFetchedFile.FileName}'");
        File.WriteAllBytes(LastFetchedFile.FileName, LastFetchedFileBytes);
        LastFetchedFileBytes = [];
        PercentFetched = 1f;
    }
    public static void UpdateFileFetch() {
        Console.WriteLine($"'{LastFetchedFile.FileName}': {PercentFetched * 100:0.00}%");
    }
}
