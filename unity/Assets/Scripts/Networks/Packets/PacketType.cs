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
        GetItem = 54,
        PutItem = 56,
        DiggingStart = 100,
        DiggingQuit = 102,
        FeedStart = 110,
        FeedQuit = 112,
        MongdungAttack = 60,
        MonggingRevivalStart = 62,
        MonggingRevivalStop = 65,
        MonggingItemUse = 69,
        MonggingFieldItemUse = 71,
        
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
        GetItemResponse = 55,
        PutItemResponse = 57,
        DiggingStartResponse = 101,
        DiggingQuitResponse = 103,
        DiggingDoneResponse = 104,
        FeedStartResponse = 111,
        FeedQuitResponse = 113,
        FeedForceQuitResponse = 114,
        MongdungAttackResponse = 61,
        MonggingRevivalStartResponse = 63,
        MonggingRevivalComplete = 64,
        MonggingRevivalStopResponse = 66,
        MonggingItemUseResponse = 70,
        MonggingFieldItemUseResponse = 72,
    }
}