using System.Text;

namespace UdpCommon;

public sealed class PacketFactory
{
    private readonly PacketSequenceGenerator _sequenceGenerator = new();

    public UdpPacket CreateText(string message)
    {
        byte[] payload = Encoding.UTF8.GetBytes(message);

        return new UdpPacket(
            PacketType.Text,
            _sequenceGenerator.GetNext(),
            GetCurrentUnixTimeMillisecond(),
            payload
        );
    }

    public UdpPacket CreatePing()
    {
        return new UdpPacket(
            PacketType.Ping,
            _sequenceGenerator.GetNext(),
            GetCurrentUnixTimeMillisecond(),
            Array.Empty<byte>()
        );
    }

    public UdpPacket CreatePong(UdpPacket pingPacket)
    {
        return new UdpPacket(
            PacketType.Pong,
            pingPacket.SequenceNumber,
            GetCurrentUnixTimeMillisecond(),
            Array.Empty<byte>()
        );
    }

    private static long GetCurrentUnixTimeMillisecond()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
