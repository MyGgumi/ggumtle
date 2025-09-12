using Networks.Attributes;
using Networks.Packets;
using Networks.Rooms;

namespace Networks.Factories
{
    [CommandFactory(PacketType.GameStart)]
    public class GameStartFactory
    {
        public static GameStartCommand Create(byte[] bytes)
        {
            return new GameStartCommand();
        }
    }
}