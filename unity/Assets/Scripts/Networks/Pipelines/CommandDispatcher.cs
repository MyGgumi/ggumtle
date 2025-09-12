using System.Collections.Generic;
using System;
using DotNetty.Transport.Channels;
using Network;
using Networks.Packets;
using System.Reflection;
using Networks.Attributes;
using Networks.Rooms;
using UnityEngine;

namespace Networks.Pipelines
{
    public class CommandDispatcher
    {
        private readonly Dictionary<PacketType, Action<Command, IChannelHandlerContext>> _commandMap = new();
        
        private static CommandDispatcher _instance;
        public static CommandDispatcher Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = new CommandDispatcher();
                _instance.Initialize();
                return _instance;
            }
        }

        private void Initialize()
        {
            Debug.Log($"CommandDispatcher 초기화");

            var assembly = Assembly.GetExecutingAssembly();

            // 클래스에 CommandHandler가 붙은 게 아니라, 메서드에 CommandHandler가 붙어있음
            // 따라서 모든 타입의 모든 메서드를 순회하면서 CommandHandler attribute가 붙은 메서드를 찾아야 함
            var allTypes = assembly.GetTypes();

            foreach (var type in allTypes)
            {
                var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

                foreach (var method in methods)
                {
                    var handlerAttr = method.GetCustomAttribute<CommandHandler>(inherit: false);
                    if (handlerAttr != null)
                    {
                        // Static 메서드인지 확인
                        if (method.IsStatic)
                        {
                            _commandMap[handlerAttr.Type] = (command, ctx) => method.Invoke(null, new object[] { command, ctx });
                            Debug.Log($"CommandDispatcher - Static {type.Name}.{method.Name}에 CommandHandler({handlerAttr.Type}) 발견");
                        }
                        else
                        {
                            // Instance 메서드인 경우 싱글톤 인스턴스 사용 (지연 초기화)
                            _commandMap[handlerAttr.Type] = (command, ctx) => {
                                object instance = null;
                                
                                // 싱글톤 패턴을 가진 클래스들에 대한 처리
                                if (type == typeof(LobbyManager))
                                {
                                    instance = LobbyManager.Instance;
                                } 
                                else if (type == typeof(RoomManager))
                                {
                                    instance = RoomManager.Instance;
                                }
                                else
                                {
                                    // 다른 클래스의 경우 기존 방식대로 인스턴스 생성
                                    instance = Activator.CreateInstance(type);
                                }
                                
                                method.Invoke(instance, new object[] { command, ctx });
                            };
                            Debug.Log($"CommandDispatcher - Instance {type.Name}.{method.Name}에 CommandHandler({handlerAttr.Type}) 발견");
                        }
                    }
                }
            }
        }

        public void Dispatch(Command command, IChannelHandlerContext ctx)
        {
            Debug.Log($"CommandDispatcher: {command.Type} 패킷 처리 시작");
            
            // 1. CommandHandler가 있는지 먼저 확인
            if (_commandMap.TryGetValue(command.Type, out var handler))
            {
                Debug.Log($"CommandHandler 발견: {command.Type}");
                handler(command, ctx);
                return;
            }
            
            // 2. CommandHandler가 없으면 NetworkApi로 처리
            Debug.Log($"CommandHandler 없음, NetworkApi로 처리: {command.Type}");
            if (NetworkApi.Instance != null)
            {
                NetworkApi.Instance.HandleResponse(command);
            }
            else
            {
                Debug.LogWarning($"NetworkApi.Instance가 null입니다. {command.Type} 패킷을 처리할 수 없습니다.");
            }
        }
    }
}