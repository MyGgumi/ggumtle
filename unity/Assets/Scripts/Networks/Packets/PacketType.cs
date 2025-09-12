namespace Networks.Packets
{
    public enum PacketType : short
    {
        // Send
        VerifyToken = 1,
        RoomJoin = 10,
        SceneChange = 30,
        PlayerMove = 40,
        
        // Receive
        VerifyTokenResponse = 2,
        RoomJoinResponse = 11,
        SceneChangeResponse = 31,
        InitializeMapResponse = 20,
        InitializePlayerResponse = 21,
        GameStart = 35,
        
        PlayerMoveResponse = 41,
    }
}