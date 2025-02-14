using LiteNetLib;
using LiteNetLib.Utils;
using System.Net;

namespace FileSynchronizer;

#pragma warning disable
public class Client {
    public NetManager netManager;
    public EventBasedNetListener netListener;
    public NetPeer peer;

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
        peer = netManager.Connect(address, port, password);
        netListener.PeerDisconnectedEvent += Goodbye;
    }

    private void Goodbye(NetPeer peer, DisconnectInfo disconnectInfo) {
        Console.WriteLine($"Server stopped responding: {disconnectInfo.Reason}");
    }

    private void Receive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        int packet = reader.GetInt();
        NetDataWriter writer = new();

        writer.Put(packet);

        switch (packet) {
            case Packet.REQ_FOLDER:
                var specialPath = (Environment.SpecialFolder)reader.GetByte();

                writer.Put((byte)specialPath);

                Packet.SendFolder(this, specialPath);
                break;
            case Packet.SEND_FOLDER:
                var folder = reader.DeserializeFolder();

                Console.WriteLine(folder);

                if (folder.Files.Length > 0) {
                    Console.WriteLine("Files:");
                    for (int i = 0; i < folder.Files.Length; i++) {
                        Console.WriteLine("\t" + Path.GetFileName(folder.Files[i].FileName));
                    }
                }

                Console.WriteLine("Folders:");
                for (int i = 0; i < folder.SubFolders.Length; i++) {
                    Console.WriteLine($"\t{folder.SubFolders[i]}");
                }
                break;
            case Packet.TEST:
                var num = reader.GetInt();

                Console.WriteLine(num);
                break;
        }
    }
}
