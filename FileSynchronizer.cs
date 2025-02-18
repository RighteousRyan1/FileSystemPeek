using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FileSynchronizer;
public class FileSynchronizer {

    public static Folder FolderOnOtherSystem;
    public static byte[] LastFetchedFileBytes;

    public static Client client;
    public static Server server;
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

        if (client.netPeer is not null)
            Console.WriteLine(isHost ? "Server started." : "Connection successful.");

        var quitPoll = false;
        Task.Run(async () => {
            while (!quitPoll) {
                try {
                    client.Poll();
                    server?.Poll();
                    await Task.Delay(5);
                } catch (Exception e) {
                    Console.WriteLine($"Error polling events: " + e.Message);
                }
            }
        });


        // TODO: get file fetching working (test first tho)
        var runLoop = true;
        while (runLoop) {
            Console.WriteLine("List of commands and their subcommands");
            Console.WriteLine("help (-dl)");
            Console.WriteLine("mf (-u <folder>, -d)");
            Console.WriteLine("reqsfolder <sfolder>");
            Console.WriteLine("quit");
            try {
                var cmdArgs = Console.ReadLine()!.Split(' ');

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
                            tryAgain:
                                var folderToGoTo = string.Join(' ', cmdArgs[2..]);

                                bool success = false;

                                for (int i = 0; i < FolderOnOtherSystem.SubFolders.Length; i++) {
                                    var curSubFolder = FolderOnOtherSystem.SubFolders[i];
                                    var folderName = Path.GetFileName(curSubFolder.FolderPath);
                                    if (folderName == folderToGoTo) {
                                        FolderOnOtherSystem = curSubFolder;
                                        Packet.RequestFolder(client.netPeer, curSubFolder.FolderPath);
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
                                Packet.RequestFolder(client.netPeer, Directory.GetParent(FolderOnOtherSystem.FolderPath)!.FullName);
                                break;
                        }
                        break;
                    // "request special folder"
                    case "reqsfolder":
                        // for readability
                        var fileToReq = subcmd;
                        var pathNames = Utils.AllSpecialFolders;

                        bool nameSuccess = false;
                        for (int i = 0; i < pathNames.Length; i++) {
                            var name = pathNames[i];
                            if (fileToReq == name) {
                                nameSuccess = true;
                                Packet.RequestSFolder(client.netPeer, Enum.GetValues<Environment.SpecialFolder>()[i]);
                                break;
                            }
                        }
                        if (!nameSuccess)
                            Console.WriteLine("There is no special folder with that name. Please use 'help -dl' for more info.");
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
            //Packet.RequestSFolder(client.netPeer, Environment.SpecialFolder.CDBurning);
        }
        server.netManager.Stop(true);
    }
}
