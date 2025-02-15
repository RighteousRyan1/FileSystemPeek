using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;

namespace FileSynchronizer;

#pragma warning disable
public class Client {
    public NetManager netManager;
    public EventBasedNetListener netListener;
    public NetPeer netPeer;
    public int Id { get; private set; }
    public string Name { get; }

    public Client(int id) {
        Name = Environment.MachineName;
        Id = id;

        netListener = new();
        netManager = new(netListener) {
            NatPunchEnabled = true
        };

        netListener.NetworkReceiveEvent += Receive;
    }

    public void Poll() => netManager.PollEvents();
    public void AttemptConnectionTo(string address, int port, string password) {
        netManager?.Start();
        netPeer = netManager.Connect(address, port, password);
        netListener.PeerDisconnectedEvent += Goodbye;
    }

    private void Goodbye(NetPeer peer, DisconnectInfo disconnectInfo) {
        Console.WriteLine($"Server stopped responding: {disconnectInfo.Reason}");
    }

    public static List<byte> byteList = [];

    private void Receive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        int packet = reader.GetInt();

        switch (packet) {
            case Packet.SEND_STR:
                var str = reader.GetString();

                Console.WriteLine(str);
                break;
            case Packet.REQ_S_FOLDER:
                var specialPath = (Environment.SpecialFolder)reader.GetByte();

                Packet.SendSFolder(netPeer, specialPath);
                break;
            case Packet.SEND_S_FOLDER:
                var sentSFolder = reader.DeserializeFolder();
                FileSynchronizer.FolderOnOtherSystem = sentSFolder;

                Console.WriteLine(sentSFolder);

                if (sentSFolder.Files.Length > 0) {
                    Console.WriteLine("Files:");
                    for (int i = 0; i < sentSFolder.Files.Length; i++) {
                        Console.WriteLine("\t" + Path.GetFileName(sentSFolder.Files[i].FileName));
                    }
                }

                Console.WriteLine("Folders:");
                for (int i = 0; i < sentSFolder.SubFolders.Length; i++) {
                    Console.WriteLine($"\t{sentSFolder.SubFolders[i]}");
                }
                break;
            case Packet.SEND_FILE:
                byte[] bytes = [];
                reader.GetBytes(bytes, Packet.MAX_PACKET_CAPACITY);

                bool terminate = reader.GetBool();

                if (!terminate)
                    byteList.AddRange(bytes);
                else
                    FileSynchronizer.LastFetchedFileBytes = byteList.ToArray();
                break;
            case Packet.REQ_FOLDER:
                var path = reader.GetString();

                Packet.SendFolder(netPeer, path);
                break;
            case Packet.SEND_FOLDER:
                var sentFolder = reader.DeserializeFolder();
                FileSynchronizer.FolderOnOtherSystem = sentFolder;

                Console.WriteLine(sentFolder);

                if (sentFolder.Files.Length > 0) {
                    Console.WriteLine("Files:");
                    for (int i = 0; i < sentFolder.Files.Length; i++) {
                        Console.WriteLine("\t" + Path.GetFileName(sentFolder.Files[i].FileName));
                    }
                }

                Console.WriteLine("Folders:");
                for (int i = 0; i < sentFolder.SubFolders.Length; i++) {
                    Console.WriteLine($"\t{sentFolder.SubFolders[i]}");
                }
                break;
        }
    }
}
