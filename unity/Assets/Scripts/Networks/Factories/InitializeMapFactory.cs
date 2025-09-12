using System.Collections.Generic;
using DotNetty.Buffers;
using Networks.Attributes;
using Networks.Packets;
using Networks.Rooms;
using Networks.Rooms.Domains;

namespace Networks.Factories
{
    [CommandFactory(PacketType.InitializeMapResponse)]
    public class InitializeMapFactory
    {
        public static InitializeMapCommand Create(byte[] bytes)
        {
            var buffer = Unpooled.WrappedBuffer(bytes);
            
            var chestCount = buffer.ReadInt();
            List<ChestPacket> chests = new();
            for (int i = 0; i < chestCount; i++)
            {
                var id = buffer.ReadInt();
                var x = buffer.ReadInt();
                var y = buffer.ReadInt();
                var z = buffer.ReadInt();
                
                chests.Add(new ChestPacket(id, x, y, z));;
            }
            
            var ggumtleCount = buffer.ReadInt();
            List<GgumtlePacket> ggumtles = new();
            for (int i = 0; i < ggumtleCount; i++)
            {
                var id = buffer.ReadInt();
                var x = buffer.ReadInt();
                var y = buffer.ReadInt();
                var z = buffer.ReadInt();
                
                ggumtles.Add(new GgumtlePacket(id, x, y, z));
            }
            
            var healPackCount = buffer.ReadInt();
            List<HealPackPacket> healPacks = new();
            for (int i = 0; i < healPackCount; i++)
            {
                var id = buffer.ReadInt();
                var x = buffer.ReadInt();
                var y = buffer.ReadInt();
                var z = buffer.ReadInt();
                
                healPacks.Add(new HealPackPacket(id, x, y, z));
            }
            
            var speedPackCount = buffer.ReadInt();
            List<SpeedPackPacket> speedPacks = new();
            for (int i = 0; i < speedPackCount; i++)
            {
                var id = buffer.ReadInt();
                var x = buffer.ReadInt();
                var y = buffer.ReadInt();
                var z = buffer.ReadInt();
                
                speedPacks.Add(new SpeedPackPacket(id, x, y, z));
            }
            
            return new InitializeMapCommand(chests, ggumtles, healPacks, speedPacks);
        }
    }
}