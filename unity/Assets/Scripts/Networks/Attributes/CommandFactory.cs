using System;
using Networks.Packets;

namespace Networks.Attributes
{
    public class CommandFactory : Attribute
    {
        public PacketType Type;
        
        public CommandFactory(PacketType type)
        {
            Type = type;
        }
    }
}