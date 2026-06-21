namespace UdpCommon;

public sealed class PacketOrderTracker
{
    private uint? _lastSequenceNumber = null;

    public PacketOrderResult Check(uint currentSequenceNumber)
    {
        if (_lastSequenceNumber is null)
        {
            _lastSequenceNumber = currentSequenceNumber;

            return new PacketOrderResult(
                PacketOrderState.FirstPacket,
                null,
                currentSequenceNumber,
                null,
                0
            );
        }

        uint expectedSequenceNumber = _lastSequenceNumber.Value + 1;

        if (currentSequenceNumber == expectedSequenceNumber)
        {
            uint previous = _lastSequenceNumber.Value;
            _lastSequenceNumber = currentSequenceNumber;

            return new PacketOrderResult(
                PacketOrderState.InOrder,
                expectedSequenceNumber,
                currentSequenceNumber,
                previous,
                0
            );
        }

        if (currentSequenceNumber > expectedSequenceNumber)
        {
            uint previous = _lastSequenceNumber.Value;
            uint missingCount = currentSequenceNumber - expectedSequenceNumber;

            _lastSequenceNumber = currentSequenceNumber;

            return new PacketOrderResult(
                PacketOrderState.MissingPackets,
                expectedSequenceNumber,
                currentSequenceNumber,
                previous,
                missingCount
            );
        }

        return new PacketOrderResult(
            PacketOrderState.OutOfOrderOrDuplicate,
            expectedSequenceNumber,
            currentSequenceNumber,
            _lastSequenceNumber,
            0
        );
    }
}
