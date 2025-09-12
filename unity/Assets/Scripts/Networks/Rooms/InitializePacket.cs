using System.Collections.Generic;
using Network;
using Networks.Packets;
using Networks.Rooms.Domains;

namespace Networks.Rooms
{
    public class InitializeMapCommand : Command
    {
        public override PacketType Type => PacketType.InitializeMapResponse;
        
        public List<ChestPacket> chests;
        public List<GgumtlePacket> ggumtles;
        public List<HealPackPacket> healPacks;
        public List<SpeedPackPacket> speedPacks;
        
        public InitializeMapCommand(List<ChestPacket> chests, List<GgumtlePacket> ggumtles, List<HealPackPacket> healPacks, List<SpeedPackPacket> speedPacks)
        {
            this.chests = chests;
            this.ggumtles = ggumtles;
            this.healPacks = healPacks;
            this.speedPacks = speedPacks;
        }
    }

    public class InitializePlayerCommand : Command
    {
        public override PacketType Type => PacketType.InitializePlayerResponse;

        public List<PlayerPacket> players;
        
        public InitializePlayerCommand(List<PlayerPacket> players)
        {
            this.players = players;
        }
    }
}