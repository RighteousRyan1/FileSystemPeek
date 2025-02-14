using LiteNetLib;
using LiteNetLib.Utils;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FileSynchronizer;
public class FileSynchronizer {

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

        if (client.peer is not null)
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

        while (true) {
            //client.Poll();
            //server?.Poll();
            Console.ReadLine();
            Packet.RequestFolder(client, Environment.SpecialFolder.LocalApplicationData);
            //Packet.Test(client, new Random().Next(100));
        }


        /*var folder = new Folder(@"C:\Users\ryanr\Desktop");

        List<FileContents> loadedFiles = [];

        Console.WriteLine(folder);

        if (folder.Files.Length > 0) {
            Console.WriteLine("Files:");
            for (int i = 0; i < folder.Files.Length; i++) {
                Console.WriteLine("\t" + Path.GetFileName(folder.Files[i].FileName));
                loadedFiles.Add(folder.Files[i]);
            }
        }

        Console.WriteLine("Folders:");
        for (int i = 0; i < folder.SubFolders.Length; i++) {
            Console.WriteLine($"\t{folder.SubFolders[i]}");
            foreach (var file in folder.SubFolders[i].Files) {
                loadedFiles.Add(file);
            }
        }*/

        server.netManager.Stop(true);
        /*Console.ReadLine();
        foreach (var item in loadedFiles) {
            item.FetchData();
        }
        Console.ReadLine();*/
    }
}
