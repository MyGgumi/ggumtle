using System;
using DotNetty.Transport.Channels;
using Features.Ggumtle.Services;
using Networks.Attributes;
using Networks.Game;
using Networks.Ggumtle;
using Networks.Packets;
using UnityEngine;
using VContainer;

namespace Features.Ggumtle.NetworkSources
{
    /// <summary>
    /// 꿈틀이 관련 서버 이벤트를 처리하는 핸들러
    /// CommandHandler 어트리뷰트를 사용하여 서버 푸시 이벤트를 수신하고
    /// GgumtleService에 비즈니스 로직을 위임
    /// </summary>
    public class GgumtleNetworkEventHandler
    {
        private readonly IGgumtleService _ggumtleService;
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public GgumtleNetworkEventHandler(IGgumtleService ggumtleService)
        {
            _ggumtleService =
                ggumtleService ?? throw new ArgumentNullException(nameof(ggumtleService));

            if (_enableDebugLogs)
            {
                Debug.Log("[GgumtleNetworkEventHandler] 초기화 완료");
            }
        }

        /// <summary>
        /// 꿈틀이 파기 완료 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.DiggingDoneResponse)]
        public async void DiggingDone(DiggingDoneCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkEventHandler] 꿈틀이 파기 완료 이벤트: Id={command.Id}, IsRealGgumtle={command.IsRealGgumtle}"
                    );
                }

                _ggumtleService.HandleDiggingDone(command.Id, command.IsRealGgumtle);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GgumtleNetworkEventHandler] 파기 완료 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 젤리 강제 종료 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.JellyForceQuitResponse)]
        public async void JellyForceQuit(JellyForceQuitCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkEventHandler] 젤리 강제 종료 이벤트: GgumtleId={command.GgumtleId}, LeftJellyCount={command.LeftJellyCount}"
                    );
                }

                _ggumtleService.HandleJellyForceQuit(command.GgumtleId, command.LeftJellyCount);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GgumtleNetworkEventHandler] 젤리 강제 종료 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 꿈틀이 스폰 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GgumtleSpawn)]
        public async void GgumtleSpawn(GgumtleSpawnCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkEventHandler] 꿈틀이 스폰 이벤트: Id={command.id}, Position={command.position}"
                    );
                }

                // System.Numerics.Vector3를 UnityEngine.Vector3로 변환
                var unityPosition = new UnityEngine.Vector3(
                    command.position.X,
                    command.position.Y,
                    command.position.Z
                );
                _ggumtleService.HandleGgumtleSpawn(command.id, unityPosition);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GgumtleNetworkEventHandler] 꿈틀이 스폰 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 꿈틀이 성불 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GgumtleNirvana)]
        public async void GgumtleNirvana(GgumtleNirvanaCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 꿈틀이 성불 이벤트: Id={command.id}");
                }

                _ggumtleService.HandleGgumtleNirvana(command.id);
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GgumtleNetworkEventHandler] 꿈틀이 성불 이벤트 처리 실패: {e.Message}"
                );
            }
        }
    }
}
