using System.Collections.Concurrent;

namespace UdpCommon;

public sealed class PingTracker
{
    private readonly ConcurrentDictionary<uint, long> _pendingPings = new();

    public void RegisterPing(UdpPacket pingPacket)
    {
        _pendingPings[pingPacket.SequenceNumber] = pingPacket.Timestamp;
    }

    public bool TryCompletePing(UdpPacket pongPacket, out long rttMilliseconds)
    {
        long receivedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        bool found = _pendingPings.TryRemove(
            pongPacket.SequenceNumber,
            out long sentAt
        );

        if (!found)
        {
            rttMilliseconds = 0;
            return false;
        }

        rttMilliseconds = receivedAt - sentAt;
        return true;
    }
}
