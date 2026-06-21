namespace UdpCommon;

public enum PacketOrderState
{
    FirstPacket,
    InOrder,
    MissingPackets,
    OutOfOrderOrDuplicate
}
