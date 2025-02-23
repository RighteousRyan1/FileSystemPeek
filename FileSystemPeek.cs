using System.Diagnostics;

namespace FileSynchronizer;

public class FileSystemPeek {
    public static float PercentFetched;
    public static int CLeft;
    public static int CTop;

    public static Folder FolderOnOtherSystem;

    public static FileContents LastFetchedFile;
    public static byte[] LastFetchedFileBytes;

    public static Client client;
    public static Server server;

    public static bool runLoop;
    public static bool startDl = true;
    public static bool isFetchingFolder;

    // a whole shitstorm going on with this one soon lol.
    public static int curFolderDl;
    public static int curFileDl;

    public static Stopwatch folderDlStopwatch = new();
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
            Utils.Log(isHost ? "Server started." : "Connection successful.");
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
            var subcmd = cmdArgs.Length > 1 ? cmdArgs[1] : string.Empty;

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
                case "dlfile":
                    var fileWeWant = string.Join(' ', cmdArgs[1..]);

                    bool fileSuccess = false;

                    if (FolderOnOtherSystem is null || FolderOnOtherSystem.Files is null) {
                        Console.WriteLine("Cannot download file as there is no folder loaded from the other system.");
                        break;
                    }

                    for (int i = 0; i < FolderOnOtherSystem.Files.Length; i++) {
                        var curFile = FolderOnOtherSystem.Files[i];
                        var fileName = curFile.FileName;
                        if (fileName == fileWeWant) {
                            Utils.Log($"Requesting file '{fileName}'");
                            Packet.RequestFile(client.netPeer!, curFile.FilePath);
                            LastFetchedFile = curFile;
                            fileSuccess = true;
                            break;
                        }
                    }

                    if (!fileSuccess)
                        Console.WriteLine($"There is no file with name '{fileWeWant}' within the current folder.");
                    break;
                // "download folder -all"
                // -a indicates that it should also download subfolders
                case "dlfolder":
                    if (FolderOnOtherSystem is null || FolderOnOtherSystem.Files is null) {
                        Console.WriteLine("Cannot download folder as there is no folder loaded from the other system.");
                        break;
                    }
                    if (FolderOnOtherSystem.Files.Length == 0) {
                        Console.WriteLine("Cannot download this folder as it contains no files.");
                        break;
                    }

                    curFileDl = 0;

                    Directory.CreateDirectory(FolderOnOtherSystem.FolderName);
                    isFetchingFolder = true;

                    LastFetchedFile = FolderOnOtherSystem.Files[curFileDl];

                    folderDlStopwatch.Restart();
                    // fetch the first one, FinalizeFileFetch will fetch sequential ones in ascending order.
                    Packet.RequestFile(client.netPeer!, FolderOnOtherSystem.Files[curFileDl].FilePath);
                    break;
                case "clear":
                    Console.Clear();
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
    
    public static void FinalizeFileFetch(bool isFolderFetch, bool fetchSubfolders = false) {
        if (isFolderFetch) {
            if (curFileDl == FolderOnOtherSystem.Files.Length - 1) {
                if (!fetchSubfolders) {
                    Utils.Log($"Folder done! Time elapsed: {folderDlStopwatch.Elapsed.StopwatchFormat()}ms");
                    Utils.Log($"All contents saved inside '{nameof(FileSystemPeek)}/{FolderOnOtherSystem.FolderName}'");
                    isFetchingFolder = false;
                    folderDlStopwatch.Stop();
                }
                else {
                    // do the zaza.
                    Utils.Log($"Folder {curFolderDl} done.");
                }
            }
            // if we're just incrementing
            else {
                curFileDl++;
                File.WriteAllBytes(Path.Combine(FolderOnOtherSystem.FolderName, LastFetchedFile.FileName), LastFetchedFileBytes);
                // ensure this is next otherwise file misnaming!!
                LastFetchedFile = FolderOnOtherSystem.Files[curFileDl];
                //Console.WriteLine($"'{LastFetchedFile.FileName}'... Done");
                Packet.RequestFile(client.netPeer!, LastFetchedFile.FilePath);
            }
        } 
        // just fetching a singular file.
        else {
            Utils.Log($"Done! Saved to '{nameof(FileSystemPeek)}/{LastFetchedFile.FileName}'");
            File.WriteAllBytes(LastFetchedFile.FileName, LastFetchedFileBytes);
        }
        LastFetchedFileBytes = [];
        Client.byteList = [];
        startDl = true;
        Console.CursorVisible = true;
    }

    public static void UpdateFileFetch() {
        if (startDl) {
            Console.Write($"'{LastFetchedFile.FileName}'... ");
            CLeft = Console.CursorLeft;
            CTop = Console.CursorTop;
            Console.CursorVisible = false;
            startDl = false;
        }

        var finished = PercentFetched == 1f;
        var totalBytes = Utils.SizeSuffix(LastFetchedFile.Size, 2);
        var bytesDownloaded = finished ? totalBytes : Utils.SizeSuffix(LastFetchedFile.Size, 2, PercentFetched);

        var str = finished ? "Done!" : $"{PercentFetched * 100:0.00}% ({bytesDownloaded} / {totalBytes})";

        Console.SetCursorPosition(CLeft, CTop);

        Console.WriteLine(str);

        if (finished)
            FinalizeFileFetch(isFetchingFolder);
    }
}
