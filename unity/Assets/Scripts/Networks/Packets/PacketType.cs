namespace Networks.Packets
{
    public enum PacketType : short
    {
        // Send
        VerifyToken = 1,
        RoomJoin = 10,
        SceneChange = 30,
        PlayerMove = 40,
        ChestOpen = 50,
        ChestClose = 52,
        DiggingStart = 100,
        DiggingQuit = 102,
        
        // Receive
        VerifyTokenResponse = 2,
        RoomJoinResponse = 11,
        SceneChangeResponse = 31,
        InitializeMapResponse = 20,
        InitializePlayerResponse = 21,
        GameStart = 35,
        PlayerMoveResponse = 41,
        ChestOpenResponse = 51,
        ChestCloseResponse = 53,
        DiggingStartResponse = 101,
        DiggingQuitResponse = 103,
    }
}