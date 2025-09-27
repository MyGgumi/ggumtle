using System;
using DotNetty.Transport.Channels;
using Features.Ggumtle.Messages;
using Networks.Attributes;
using Networks.Game;
using Networks.Ggumtle;
using Networks.Packets;
using UnityEngine;

namespace Features.Ggumtle.NetworkSources
{
    /// <summary>
    /// 꿈틀이 관련 서버 이벤트를 처리하는 Static 핸들러
    /// CommandHandler 어트리뷰트를 사용하여 서버 푸시 이벤트를 수신하고
    /// MessagePipeBridge를 통해 MessagePipe 이벤트로 변환
    /// </summary>
    public class GgumtleNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = true;
        private static System.Collections.Generic.Dictionary<int, int> _previousStates = new System.Collections.Generic.Dictionary<int, int>();

        private static int GetPreviousState(int ggumtleId)
        {
            return _previousStates.TryGetValue(ggumtleId, out var state) ? state : -1;
        }

        private static void SetPreviousState(int ggumtleId, int state)
        {
            _previousStates[ggumtleId] = state;
        }

        /// <summary>
        /// 꿈틀이 파기 시작 응답 처리
        /// </summary>
        [CommandHandler(PacketType.DiggingStartResponse)]
        public static void DiggingStart(DiggingStartCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 꿈틀이 파기 시작 응답: Result={command.Result}, Success={command.Success}");
                }

                // NetworkApi의 pending request 완료
                var networkApi = GameObject.Find("NetworkApi")?.GetComponent<Networks.NetworkApi>();
                networkApi?.HandleResponse(command);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkEventHandler] 파기 시작 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 꿈틀이 파기 중단 응답 처리
        /// </summary>
        [CommandHandler(PacketType.DiggingQuitResponse)]
        public static void DiggingQuit(DiggingQuitCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 꿈틀이 파기 중단 응답: Result={command.Result}, Success={command.Success}");
                }

                // NetworkApi의 pending request 완료
                var networkApi = GameObject.Find("NetworkApi")?.GetComponent<Networks.NetworkApi>();
                networkApi?.HandleResponse(command);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkEventHandler] 파기 중단 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 젤리 먹이기 시작 응답 처리
        /// </summary>
        [CommandHandler(PacketType.JellyStartResponse)]
        public static void JellyFeedingStart(JellyStartCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 젤리 먹이기 시작 응답: Result={command.Result}, Success={command.Success}");
                }

                // NetworkApi의 pending request는 이미 CommandDispatcher에서 처리됨
                // 여기서는 추가 로깅만 수행
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkEventHandler] 젤리 먹이기 시작 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 젤리 먹이기 중단 응답 처리
        /// </summary>
        [CommandHandler(PacketType.JellyQuitResponse)]
        public static void JellyFeedingQuit(JellyQuitCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 젤리 먹이기 중단 응답: Result={command.Result}, Success={command.Success}, LeftCount={command.LeftJellyCount}");
                }

                // // NetworkApi의 pending request 완료
                // var networkApi = GameObject.Find("NetworkApi")?.GetComponent<Networks.NetworkApi>();
                // networkApi?.HandleResponse(command);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkEventHandler] 젤리 먹이기 중단 응답 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 젤리 먹이기 강제 종료 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.JellyForceQuitResponse)]
        public static void JellyForceQuit(JellyForceQuitCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[GgumtleNetworkEventHandler] 젤리 강제 종료 이벤트: GgumtleId={command.GgumtleId}, LeftJellyCount={command.LeftJellyCount}"
                    );
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var message = new GgumtleJellyForceQuitMessage(command.GgumtleId, command.LeftJellyCount);
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            "[GgumtleNetworkEventHandler] 젤리 강제 종료 메시지 발행 성공 → GgumtleService"
                        );
                    }
                }
                else
                {
                    Debug.LogError("[GgumtleNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
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
        public static void GgumtleSpawn(GgumtleSpawnCommand command, IChannelHandlerContext ctx)
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

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var message = new GgumtleSpawnMessage(command.id, unityPosition);
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            "[GgumtleNetworkEventHandler] 꿈틀이 스폰 메시지 발행 성공 → GgumtleService"
                        );
                    }
                }
                else
                {
                    Debug.LogError("[GgumtleNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GgumtleNetworkEventHandler] 꿈틀이 스폰 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 젤리 개수 업데이트 처리
        /// </summary>
        [CommandHandler(PacketType.JellyCount)]
        public static void JellyCount(JellyCountCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 젤리 개수 업데이트: {command.JellyCount}");
                }

                // MessagePipeBridge를 통해 젤리 개수 업데이트 메시지 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var jellyCountMessage = new Features.Feeding.Messages.JellyCountUpdateMessage(command.JellyCount);
                    bridge.PublishMessage(jellyCountMessage);

                    if (_enableDebugLogs)
                        Debug.Log($"[GgumtleNetworkEventHandler] 젤리 개수 업데이트 메시지 발행: {command.JellyCount}");
                }
                else
                {
                    Debug.LogError("[GgumtleNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkEventHandler] 젤리 개수 업데이트 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 꿈틀이 상태 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GgumtleState)]
        public static void GgumtleState(GgumtleStatusCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 꿈틀이 상태: Id={command.ggumtleId}, State={command.state}({(int)command.state})");

                    // 비정상적인 상태 전환 감지
                    var prevState = GetPreviousState(command.ggumtleId);
                    if (prevState == 10 && (int)command.state == 2)  // PullUp → Digging
                    {
                        Debug.LogWarning($"[GgumtleNetworkEventHandler] 비정상적인 상태 전환 감지! PullUp(10) → Digging(2)");
                    }
                    SetPreviousState(command.ggumtleId, (int)command.state);
                }

                // 상태 브로드캐스트 메시지 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var stateBroadcastMessage = new GgumtleStateBroadcastMessage(command.ggumtleId, (int)command.state);
                    bridge.PublishMessage(stateBroadcastMessage);

                    if (_enableDebugLogs)
                        Debug.Log($"[GgumtleNetworkEventHandler] 상태 브로드캐스트 메시지 발행: State={command.state}");
                }
                else
                {
                    Debug.LogError("[GgumtleNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[GgumtleNetworkEventHandler] 꿈틀이 상태 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 꿈틀이별 먹은 젤리 개수 업데이트 처리
        /// </summary>
        [CommandHandler(PacketType.GgumtleJellyEaten)]
        public static void GgumtleJellyEaten(GgumtleJellyEatenCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GgumtleNetworkEventHandler] 꿈틀이 먹은 젤리 개수: GgumtleId={command.GgumtleId}, EatenCount={command.EatenCount}");
                }

                // MessagePipeBridge를 통해 꿈틀이별 먹은 젤리 개수 업데이트 메시지 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var jellyEatenMessage = new GgumtleJellyEatenMessage(command.GgumtleId, command.EatenCount);
                    bridge.PublishMessage(jellyEatenMessage);

                    if (_enableDebugLogs)
                        Debug.Log($"[GgumtleNetworkEventHandler] 꿈틀이 먹은 젤리 개수 업데이트 메시지 발행: GgumtleId={command.GgumtleId}, EatenCount={command.EatenCount}");
                }
                else
                {
                    Debug.LogError("[GgumtleNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleNetworkEventHandler] 꿈틀이 먹은 젤리 개수 업데이트 처리 실패: {e.Message}");
            }
        }
    }
}
