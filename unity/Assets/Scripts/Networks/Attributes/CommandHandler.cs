using System;
using Networks.Packets;

namespace Networks.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandHandler : Attribute
    {
        public PacketType Type;

        public CommandHandler(PacketType type)
        {
            Type = type;
        }
    }
}