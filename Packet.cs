using LiteNetLib;
using LiteNetLib.Utils;

namespace FileSynchronizer; 
public static class Packet {
    public const int REQ_FOLDER = 0;
    public const int SEND_FOLDER = 1;
    public const int TEST = 2;

    public static void RequestFolder(Client client, Environment.SpecialFolder folderPath) {
        NetDataWriter writer = new();
        //var folder = new Folder(Environment.GetFolderPath(folderPath));

        writer.Put(REQ_FOLDER);
        writer.Put((byte)folderPath);
        //writer.Serialize(folder);

        client.peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    public static void SendFolder(Client client, Environment.SpecialFolder folderPath) {
        NetDataWriter writer = new();

        var folder = new Folder(Environment.GetFolderPath(folderPath));

        writer.Put(SEND_FOLDER);
        writer.Serialize(folder);

        client.peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public static void Test(Client client, int numToSend) {
        NetDataWriter writer = new();
        writer.Put(TEST);
        writer.Put(numToSend);

        client.peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}
