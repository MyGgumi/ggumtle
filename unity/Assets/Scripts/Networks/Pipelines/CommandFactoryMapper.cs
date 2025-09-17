using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Network;
using Networks.Attributes;
using Networks.Packets;
using UnityEngine;

namespace Networks.Pipelines
{
    public class CommandFactoryMapper
    {
        private readonly Dictionary<PacketType, Func<byte[], Command>> _commandMap = new();

        private static CommandFactoryMapper _instance;
        public static CommandFactoryMapper Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = new CommandFactoryMapper();
                _instance.Initialize();
                return _instance;
            }
        }

        private void Initialize()
        {
            // Reflection을 통해 모든 CommandFactory 클래스를 찾아서 등록
            var assembly = Assembly.GetExecutingAssembly();

            var attributeTypes = assembly
                .GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(CommandFactory), inherit: false).Any())
                .ToList();

            foreach (var type in attributeTypes)
            {
                var attr = type.GetCustomAttribute<CommandFactory>();

                Debug.Log($"{type.Name}: {attr.Type}");
                var infos = type.GetMethods();

                foreach (var info in infos)
                {
                    if (info.Name == "Create")
                    {
                        _commandMap[attr.Type] = (bytes) =>
                            (Command)info.Invoke(null, new object[] { bytes });
                    }
                }
            }
        }
        
        public Command CreateCommand(PacketType packetType, byte[] data)
        {
            if (_commandMap.TryGetValue(packetType, out var factory))
            {
                return factory(data);
            }
            
            Debug.LogWarning($"CommandFactory를 찾을 수 없습니다: {packetType}");
            return null;
        }
    }
}
