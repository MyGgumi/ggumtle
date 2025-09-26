using System;
using DotNetty.Transport.Channels;
using Features.Player.Messages;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;
using UnityEngine;

namespace Features.Player.NetworkSources
{
    /// <summary>
    /// 플레이어 네트워크 이벤트를 처리하는 핸들러
    /// 원격 플레이어의 이동 데이터를 수신하여 처리
    /// </summary>
    public static class PlayerNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = false; // 과도한 이동 로그 방지

        /// <summary>
        /// 플레이어 이동 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.PlayerMoveResponse)]
        public static void PlayerMove(PlayerMoveCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (command == null)
                {
                    Debug.LogWarning("[PlayerNetworkEventHandler] PlayerMoveCommand가 null입니다.");
                    return;
                }

                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerNetworkEventHandler] 📡 서버로부터 플레이어 이동 수신: ID={command.PlayerId}, Position={command.Position}, Direction={command.Direction}");
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // 이동 상태 판단: direction이 zero가 아니면 이동 중
                    bool isMoving = command.Direction != Vector3.zero;
                    // 속도는 direction의 크기로 추정 (정규화되지 않은 경우)
                    float speed = command.Direction.magnitude;

                    var message = new PlayerMoveResponseMessage(
                        command.PlayerId,
                        command.Position,
                        command.Direction, // 실제 방향 정보 사용
                        isMoving,         // 방향 벡터 기반 이동 상태 판단
                        speed             // 방향 벡터 크기 기반 속도
                    );
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log("[PlayerNetworkEventHandler] 플레이어 이동 메시지 발행 성공 → PlayerManagerService");
                    }
                }
                else
                {
                    Debug.LogError("[PlayerNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerNetworkEventHandler] 플레이어 이동 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 점프 이벤트 처리 (현재는 위치 정보로 처리됨)
        /// </summary>
        public static void HandlePlayerJump(long playerId, bool isJumping)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerNetworkEventHandler] 플레이어 점프: ID={playerId}, IsJumping={isJumping}");
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var message = new PlayerJumpMessage(playerId, isJumping);
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log("[PlayerNetworkEventHandler] 플레이어 점프 메시지 발행 성공 → PlayerManagerService");
                    }
                }
                else
                {
                    Debug.LogError("[PlayerNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerNetworkEventHandler] 플레이어 점프 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 애니메이션 상태 이벤트 처리
        /// </summary>
        public static void HandlePlayerAnimationState(long playerId, string animationState)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerNetworkEventHandler] 플레이어 애니메이션: ID={playerId}, State={animationState}");
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    var message = new PlayerAnimationStateMessage(playerId, animationState);
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log("[PlayerNetworkEventHandler] 플레이어 애니메이션 메시지 발행 성공 → PlayerManagerService");
                    }
                }
                else
                {
                    Debug.LogError("[PlayerNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerNetworkEventHandler] 플레이어 애니메이션 처리 실패: {e.Message}");
            }
        }
    }
}