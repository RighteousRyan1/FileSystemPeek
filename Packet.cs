using LiteNetLib;
using LiteNetLib.Utils;

namespace FileSynchronizer; 
public static class Packet {
    public const int MAX_PACKET_CAPACITY = 25000;

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

        var lengthLeft = info.Length;

        var slices = (lengthLeft / MAX_PACKET_CAPACITY) + 1;

        var bytes = File.ReadAllBytes(filePath);

        for (int i = 0; i < slices; i++) {
            NetDataWriter writer = new();

            writer.Put(SEND_FILE);

            // put bytes in our current section only

            var start = i * MAX_PACKET_CAPACITY;
            var num = lengthLeft > MAX_PACKET_CAPACITY ? MAX_PACKET_CAPACITY : (int)lengthLeft;

            writer.Put(num);
            writer.Put(bytes[start..(start + num)]);

            // terminate
            var doTerminate = i == slices - 1;
            writer.Put(doTerminate);

            // should make finalization sent not try and send 10k for something less than 10k?
            lengthLeft -= num;

            if (!doTerminate) {
                // % download complete
                writer.Put(1f - ((float)lengthLeft / info.Length));
            }

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
