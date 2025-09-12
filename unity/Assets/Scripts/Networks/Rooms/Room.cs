using System.Collections.Generic;
using Networks.Rooms.Domains;

namespace Networks.Rooms
{
    public class Room
    {
        public List<ChestPacket> chests;
        public List<GgumtlePacket> ggumtles;
        public List<HealPackPacket> healPacks;
        public List<SpeedPackPacket> speedPacks;
        public List<PlayerPacket> players;
        
        public Room()
        {
            
        }

        public Room(List<ChestPacket> chests, List<GgumtlePacket> ggumtles, List<HealPackPacket> healPacks,
            List<SpeedPackPacket> speedPacks, List<PlayerPacket> players)
        {
            this.chests = chests;
            this.ggumtles = ggumtles;
            this.healPacks = healPacks;
            this.speedPacks = speedPacks;
            this.players = players;
        }

        public void InitMap(InitializeMapCommand command)
        {
            chests = command.chests;
            ggumtles = command.ggumtles;
            healPacks = command.healPacks;
            speedPacks = command.speedPacks;
        }
        
        public void InitPlayer(InitializePlayerCommand command)
        {
            players = command.players;
        }
        
        public bool IsInitialized()
        {
            return chests != null && ggumtles != null && healPacks != null && speedPacks != null && players != null;
        }
    }
}