using System.Collections.Generic;
using System;
using DotNetty.Transport.Channels;
using Network;
using Networks.Packets;
using System.Reflection;
using Networks.Attributes;
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
                        _commandMap[handlerAttr.Type] = (command, ctx) => method.Invoke(null, new object[] { command, ctx });
                        Debug.Log($"CommandDispatcher - {type.Name}.{method.Name}에 CommandHandler({handlerAttr.Type}) 발견");
                    }
                }
            }
        }

        public void Dispatch(Command command, IChannelHandlerContext ctx)
        {
            // NetworkApi의 비동기 응답 처리 먼저 시도
            if (NetworkApi.Instance != null)
            {
                NetworkApi.Instance.HandleResponse(command);
            }
            
            // 기존 CommandHandler도 실행
            if (_commandMap.TryGetValue(command.Type, out var handler))
            {
                handler(command, ctx);
            }
        }
    }
}