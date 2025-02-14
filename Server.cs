using LiteNetLib;
using LiteNetLib.Utils;

namespace FileSynchronizer;

#pragma warning disable
public class Server {
    public NetManager netManager;
    public EventBasedNetListener netListener;

    public Client[] Clients { get; }

    public string Password;
    public string Address;
    public string Name;

    public Server() {
        netListener = new();
        netManager = new(netListener);

        Clients = [];

        netListener.NetworkReceiveEvent += Receive;
    }
    public void Poll() => netManager.PollEvents();
    public void Start(int port, string password) {
        netManager.Start(port);

        // serverNetManager.NatPunchEnabled = true;

        netListener.ConnectionRequestEvent += request => {
            if (netManager.ConnectedPeersCount < 2) {
                request.AcceptIfKey(password);
            }
            else {
                request.Reject();
            }
            netListener.PeerConnectedEvent += peer => {
                //if (peer != FileSynchronizer.client.peer)
                    //Console.WriteLine("A PC has connected to the server..");
            };
        };
    }
    private void Receive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod) {
        int packet = reader.GetInt();
        NetDataWriter writer = new();

        writer.Put(packet);

        // stop crashing out
        switch (packet) {
            case Packet.REQ_FOLDER:
                var specialPath = (Environment.SpecialFolder)reader.GetByte();

                writer.Put((byte)specialPath);

                netManager.SendToAll(writer, deliveryMethod, peer);
                break;
            case Packet.SEND_FOLDER:
                var folder = reader.DeserializeFolder();

                writer.Serialize(folder);

                netManager.SendToAll(writer, deliveryMethod, peer);
                break;
            case Packet.TEST:
                var num = reader.GetInt();

                writer.Put(num);

                // send it back to us
                netManager.SendToAll(writer, deliveryMethod);
                break;
        }
    }
}
