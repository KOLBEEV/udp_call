namespace UdpCommon;

public sealed class PacketSequenceGenerator
{
    private readonly object _lock = new object();

    private uint _currentSequenceNumber = 0;

    public uint GetNext()
    {
        lock(_lock)
        {
            uint result = _currentSequenceNumber;
            _currentSequenceNumber++;
            return result;
        }
    }
}
