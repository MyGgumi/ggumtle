using System;
using System.Threading.Tasks;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using Network;
using Networks.Pipelines;
using UnityEngine;

namespace Networks
{
    public class Client
    {
        private readonly string _host;
        private readonly int _port;
        
        private IEventLoopGroup _group;
        private IChannel _channel;
        private readonly PacketHandler _packetHandler;

        public Client(string host, int port)
        {
            _host = host;
            _port = port;
            _packetHandler = new PacketHandler();
        }

        public async Task ConnectAsync()
        {
            _group = new MultithreadEventLoopGroup();

            var bootstrap = new Bootstrap();
            bootstrap
                .Group(_group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.TcpNodelay, true)
                .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                {
                    channel.Pipeline
                        .AddLast(new PacketDecoder())
                        .AddLast(new PacketEncoder())
                        .AddLast(_packetHandler);
                }));
            
            _channel = await bootstrap.ConnectAsync(_host, _port);
            
            Debug.Log($"{_host}:{_port} 서버에 연결되었습니다.");
        }

        public void Disconnect()
        {
            _channel?.CloseAsync();
        }

        public void Send(Sendable body)
        {
            var data = body.ToBytes();
            var header = new PacketHeader(body.Type, data.Length, DateTime.UtcNow.ToBinary());
            var packet = new Packet(header, data);

            _channel.WriteAndFlushAsync(packet);
        }
    }
}
