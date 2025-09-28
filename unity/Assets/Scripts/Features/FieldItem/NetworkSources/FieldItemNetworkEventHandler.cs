using System;
using DotNetty.Transport.Channels;
using Features.FieldItem.Messages;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;
using UnityEngine;

namespace Features.FieldItem.NetworkSources
{
    /// <summary>
    /// 필드 아이템 네트워크 이벤트를 처리하는 핸들러
    /// 서버로부터 필드 아이템 사용 브로드캐스트를 수신하여 처리
    /// </summary>
    public static class FieldItemNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = true;

        /// <summary>
        /// 필드 아이템 사용 응답 브로드캐스트 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.MonggingFieldItemUseResponse)]
        public static void OnFieldItemUseResponse(
            MonggingFieldItemUseCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                if (command == null)
                {
                    Debug.LogWarning("[FieldItemNetworkEventHandler] MonggingFieldItemUseCommand가 null입니다.");
                    return;
                }

                // Result 코드에 따른 성공/실패 판정
                bool isSuccess = command.Result == MonggingFieldItemUseResult.Success;

                if (_enableDebugLogs)
                {
                    string resultMessage = command.Result switch
                    {
                        MonggingFieldItemUseResult.Success => "성공",
                        MonggingFieldItemUseResult.FieldItemNotFound => "ID에 대응하는 필드템 없음",
                        MonggingFieldItemUseResult.NotMongging => "몽깅이가 아님",
                        MonggingFieldItemUseResult.AlreadyUsed => "이미 쓴 아이템",
                        MonggingFieldItemUseResult.TooFar => "아이템 근처에 없음",
                        _ => $"알 수 없는 결과 코드: {command.Result}"
                    };

                    Debug.Log($"[FieldItemNetworkEventHandler] 📡 필드 아이템 사용 응답: FieldItemId={command.fieldItemId}, Result={command.Result}({resultMessage})");
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // 필드 아이템 전역 사용 메시지 발행 (result 기반으로 성공/실패 판정)
                    var globalUsedMessage = new FieldItemGlobalUsedMessage(command.fieldItemId, isSuccess);
                    bridge.PublishMessage(globalUsedMessage);

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[FieldItemNetworkEventHandler] 필드 아이템 전역 사용 메시지 발행: FieldItemId={command.fieldItemId}, Success={isSuccess}");
                    }
                }
                else
                {
                    Debug.LogError("[FieldItemNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[FieldItemNetworkEventHandler] 필드 아이템 사용 브로드캐스트 처리 실패: {e.Message}");
            }
        }
    }
}