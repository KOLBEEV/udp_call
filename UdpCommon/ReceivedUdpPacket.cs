using System.Net;

namespace UdpCommon;

public sealed class ReceivedUdpPacket
{
    public UdpPacket Packet { get; }
    public IPEndPoint RemoteEndPoint { get; }

    public ReceivedUdpPacket(UdpPacket packet, IPEndPoint remoteEndPoint)
    {
        Packet = packet;
        RemoteEndPoint = remoteEndPoint;
    }
}
