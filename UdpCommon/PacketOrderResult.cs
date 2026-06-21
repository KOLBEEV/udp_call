namespace UdpCommon;

public sealed class PacketOrderResult
{
    public PacketOrderState State { get; }
    public uint? ExpectedSequenceNumber { get; }
    public uint CurrentSequenceNumber { get; }
    public uint? LastSequenceNumber { get; }
    public uint MissingCount { get; }

    public PacketOrderResult(
        PacketOrderState state,
        uint? expectedSequenceNumber,
        uint currentSequenceNumber,
        uint? lastSequenceNumber,
        uint missingCount)
    {
        State = state;
        ExpectedSequenceNumber = expectedSequenceNumber;
        CurrentSequenceNumber = currentSequenceNumber;
        LastSequenceNumber = lastSequenceNumber;
        MissingCount = missingCount;
    }
}
