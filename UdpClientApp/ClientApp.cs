using System.Net;
using System.Text;
using UdpCommon;

namespace UdpClientApp;

public sealed class ClientApp : IDisposable
{
    private readonly UdpPeer _udpPeer;
    private readonly IPEndPoint _serverEndPoint;

    private readonly PacketFactory _packetFactory = new();
    private readonly PingTracker _pingTracker = new();
    private readonly PacketOrderTracker _packetOrderTracker = new();

    public ClientApp(int localPort, string serverIp, int serverPort)
    {
        _udpPeer = new UdpPeer(localPort);
        _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
    }

    private void PrintPacketOrderInfo(uint sequenceNumber)
    {
        PacketOrderResult result = _packetOrderTracker.Check(sequenceNumber);

        if (result.State == PacketOrderState.MissingPackets)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Возможно, потеряны пакеты. " +
                $"Ожидался #{result.ExpectedSequenceNumber}, " +
                $"пришёл #{result.CurrentSequenceNumber}. " +
                $"Пропущено: {result.MissingCount}"
            );
        }

        if (result.State == PacketOrderState.OutOfOrderOrDuplicate)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"Пакет пришёл не по порядку или повторно. " +
                $"Последний был #{result.LastSequenceNumber}, " +
                $"пришёл #{result.CurrentSequenceNumber}"
            );
        }
    }

    public void Dispose()
    {
        _udpPeer.Dispose();
    }
}