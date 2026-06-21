using System.Net;
using System.Net.Sockets;

namespace UdpCommon;

public sealed class UdpPeer : IDisposable
{
    private readonly UdpClient _udpClient;

    public int LocalPort { get; }

    public UdpPeer(int localPort)
    {
        LocalPort = localPort;
        _udpClient = new UdpClient(localPort);
    }

    public async Task<ReceivedUdpPacket> ReceiveAsync(CancellationToken cancellationToken)
    {
        UdpReceiveResult result = await _udpClient.ReceiveAsync(cancellationToken);

        UdpPacket packet = PacketSerializer.Deserialize(result.Buffer);

        return new ReceivedUdpPacket(packet, result.RemoteEndPoint);
    }

    public async Task<int> SendAsync(UdpPacket packet, IPEndPoint remoteEndPoint)
    {
        byte[] bytes = PacketSerializer.Serialize(packet);

        return await _udpClient.SendAsync(
            bytes,
            bytes.Length,
            remoteEndPoint
        );
    }

    public void Dispose()
    {
        _udpClient.Dispose();
    }
}
