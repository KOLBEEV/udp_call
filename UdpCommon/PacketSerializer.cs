using System.Buffers.Binary;
using System.IO;

namespace UdpCommon;

public static class PacketSerializer
{
    private const int HeaderSize = 1 + 4 + 8 + 2;

    public static byte[] Serialize(UdpPacket packet)
    {
        if (packet.Payload.Length > ushort.MaxValue)
            throw new InvalidDataException("Payload слишком большой.");
        
        byte[] buffer = new byte[HeaderSize + packet.Payload.Length];

        buffer[0] = (byte)packet.Type;

        BinaryPrimitives.WriteUInt32BigEndian(
            buffer.AsSpan(1, 4),
            packet.SequenceNumber
        );

        BinaryPrimitives.WriteInt64BigEndian(
            buffer.AsSpan(5, 8),
            packet.Timestamp
        );

        BinaryPrimitives.WriteUInt16BigEndian(
            buffer.AsSpan(13, 2),
            (ushort)packet.Payload.Length
        );

        packet.Payload.CopyTo(buffer.AsSpan(HeaderSize));

        return buffer;
    }

    public static UdpPacket Deserialize(byte[] buffer)
    {
        if (buffer.Length < HeaderSize)
            throw new InvalidOperationException("Пакет слишком маленький.");
        
        PacketType type = (PacketType)buffer[0];

        uint sequenceNumber = BinaryPrimitives.ReadUInt32BigEndian(
            buffer.AsSpan(1, 4)
        );

        long timestamp = BinaryPrimitives.ReadInt64BigEndian(
            buffer.AsSpan(5, 8)
        );

        ushort payloadLength = BinaryPrimitives.ReadUInt16BigEndian(
            buffer.AsSpan(13, 2)
        );

        if (buffer.Length != HeaderSize + payloadLength)
            throw new InvalidDataException("Размер payload не совпадает с размером пакета.");
        
        byte[] payload = buffer
            .AsSpan(HeaderSize, payloadLength)
            .ToArray();
        
        return new UdpPacket(
            type,
            sequenceNumber,
            timestamp,
            payload
        );
    }
}
