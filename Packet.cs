using LiteNetLib;
using LiteNetLib.Utils;

namespace FileSynchronizer; 
public static class Packet {
    public const ushort MAX_PACKET_CAPACITY = 10000;

    public const int SEND_STR = -1;
    public const int REQ_S_FOLDER = 0;
    public const int SEND_S_FOLDER = 1;
    public const int REQ_FILE = 2;
    public const int SEND_FILE = 3;

    public const int REQ_FOLDER = 4;
    public const int SEND_FOLDER = 5;
    public static void SendString(NetPeer peer, string str) {
        NetDataWriter writer = new();

        writer.Put(SEND_STR);
        writer.Put(str);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    public static void RequestSFolder(NetPeer peer, Environment.SpecialFolder folderPath) {
        NetDataWriter writer = new();
        //var folder = new Folder(Environment.GetFolderPath(folderPath));

        writer.Put(REQ_S_FOLDER);
        writer.Put((byte)folderPath);
        //writer.Serialize(folder);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    public static void SendSFolder(NetPeer peer, Environment.SpecialFolder folderPath) {
        NetDataWriter writer = new();

        var folder = new Folder(Environment.GetFolderPath(folderPath));

        writer.Put(SEND_S_FOLDER);
        writer.Serialize(folder);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public static void RequestFile(NetPeer peer, string filePath) {
        NetDataWriter writer = new();

        writer.Put(REQ_FILE);
        writer.Put(filePath);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    public static void SendFile(NetPeer peer, string filePath) {
        var info = new FileInfo(filePath);

        var slices = info.Length / MAX_PACKET_CAPACITY;

        var bytes = File.ReadAllBytes(filePath);

        for (int i = 0; i < slices; i++) {
            NetDataWriter writer = new();

            writer.Put(SEND_FILE);

            // put bytes in our current section only
            writer.Put(bytes[(i * MAX_PACKET_CAPACITY)..(i * (MAX_PACKET_CAPACITY + 1))]);

            // terminate
            writer.Put(i == slices - 1);

            // tbh could be really slow.
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
        }

        // reclaim the memory WASTED by 'bytes'
        GC.Collect();
    }
    public static void RequestFolder(NetPeer peer, string folderPath) {
        NetDataWriter writer = new();
        //var folder = new Folder(Environment.GetFolderPath(folderPath));

        writer.Put(REQ_FOLDER);
        writer.Put(folderPath);
        //writer.Serialize(folder);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
    public static void SendFolder(NetPeer peer, string folderPath) {
        NetDataWriter writer = new();

        var folder = new Folder(folderPath);

        writer.Put(SEND_FOLDER);
        writer.Serialize(folder);

        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }
}
