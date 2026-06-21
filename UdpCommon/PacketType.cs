namespace UdpCommon;

public enum PacketType : byte
{
    Text = 1,
    Ping = 2, 
    Pong = 3,
    Audio = 4,
    Hangup = 5
}
