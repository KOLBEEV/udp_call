namespace UdpCommon;

public sealed class UdpPacket
{
    public PacketType Type { get; }
    public uint SequenceNumber { get; }
    public long Timestamp { get; }
    public byte[] Payload { get; }

    public UdpPacket(
        PacketType type,
        uint sequenceNumber,
        long timestamp,
        byte[] payload)
    {
        Type = type;
        SequenceNumber = sequenceNumber;
        Timestamp = timestamp;
        Payload = payload ?? Array.Empty<byte>();
    }
}
