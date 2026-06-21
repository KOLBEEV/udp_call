using System.Net;
using System.Text;

namespace UdpCommon;

public sealed class UdpSession : IDisposable
{
    private readonly UdpPeer _udpPeer;
    private readonly PacketFactory _packetFactory = new();
    private readonly PingTracker _pingTracker = new();
    private readonly PacketOrderTracker _packetOrderTracker = new();

    public int LocalPort => _udpPeer.LocalPort;

    public UdpSession(int localport)
    {
        _udpPeer = new UdpPeer(localport);
    }
    
    public async Task<ReceivedUdpPacket> ReceiveAsync(CancellationToken cancellationToken)
    {
        return await _udpPeer.ReceiveAsync(cancellationToken);
    }

    public async Task<int> SendTextAsync(string message, IPEndPoint remoteEndPoint)
    {
        UdpPacket packet = _packetFactory.CreateText(message);

        return await _udpPeer.SendAsync(packet, remoteEndPoint);
    } 

    public async Task<int> SendPingAsync(IPEndPoint remoteEndPoint)
    {
        UdpPacket packet = _packetFactory.CreatePing();

        _pingTracker.RegisterPing(packet);

        return await _udpPeer.SendAsync(packet, remoteEndPoint);
    }

    public async Task<int> SendPongAsync(UdpPacket pingPacket, IPEndPoint remoteEndPoint)
    {
        UdpPacket pongPacket = _packetFactory.CreatePong(pingPacket);

        return await _udpPeer.SendAsync(pongPacket, remoteEndPoint);
    }

    public bool TryCompletePong(UdpPacket pongPacket, out long rttMilliseconds)
    {
        return _pingTracker.TryCompletePing(pongPacket, out rttMilliseconds);
    }

    public PacketOrderResult CheckPacketOrder(UdpPacket packet)
    {
        return _packetOrderTracker.Check(packet.SequenceNumber);
    }

    public static string DecodeText(UdpPacket packet)
    {
        return Encoding.UTF8.GetString(packet.Payload);
    }

    public void Dispose()
    {
        _udpPeer.Dispose();
    }
}
