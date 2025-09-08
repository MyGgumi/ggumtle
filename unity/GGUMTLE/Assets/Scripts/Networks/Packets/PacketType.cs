namespace Networks.Packets
{
    public enum PacketType : short
    {
        // Send
        VerifyToken = 1,
        RoomJoin = 10,
        
        // Receive
        VerifyTokenResponse = 2,
        RoomJoinResponse = 11,
    }
}