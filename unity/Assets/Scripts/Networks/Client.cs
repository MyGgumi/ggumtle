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
    public class Client : MonoBehaviour
    {
        private IEventLoopGroup _group;
        private IChannel _channel;
        private PacketHandler _packetHandler;

        public bool IsConnected => _channel != null && _channel.Active;

        private void Awake()
        {
            Debug.Log("[Client] 초기화 시작");

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Debug.Log("[Client] 시작");
        }

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                Debug.Log($"[Client] 서버 연결 시도: {host}:{port}");

                if (string.IsNullOrEmpty(host))
                {
                    Debug.LogError(
                        "[Client] Host가 설정되지 않았습니다. Inspector에서 Host를 설정해주세요."
                    );
                    return;
                }

                if (port <= 0)
                {
                    Debug.LogError(
                        $"[Client] Port가 올바르지 않습니다: {port}. Inspector에서 Port를 설정해주세요."
                    );
                    return;
                }

                _group = new MultithreadEventLoopGroup();
                _packetHandler = new PacketHandler();

                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(_group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Handler(
                        new ActionChannelInitializer<ISocketChannel>(channel =>
                        {
                            channel
                                .Pipeline.AddLast(new PacketDecoder())
                                .AddLast(new PacketEncoder())
                                .AddLast(_packetHandler);
                        })
                    );

                Debug.Log($"[Client] 서버 연결 중: {host}:{port}");
                _channel = await bootstrap.ConnectAsync(host, port);

                Debug.Log($"[Client] 서버 연결 성공: {host}:{port}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Client] 서버 연결 실패: {ex.Message}");
                Debug.LogError($"[Client] 연결 시도한 주소: {host}:{port}");
                throw;
            }
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
