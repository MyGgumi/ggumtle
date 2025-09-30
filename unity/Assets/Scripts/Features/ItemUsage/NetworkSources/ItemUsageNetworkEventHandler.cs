using System;
using DotNetty.Transport.Channels;
using Features.Inventory.Messages;
using Features.ItemUsage.Messages;
using MessagePipe;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;
using UnityEngine;

namespace Features.ItemUsage.NetworkSources
{
    /// <summary>
    /// 아이템 사용 네트워크 이벤트를 처리하는 핸들러
    /// 서버로부터 아이템 사용 브로드캐스트를 수신하여 메시지로 변환
    /// </summary>
    public static class ItemUsageNetworkEventHandler
    {
        private static IPublisher<ItemUsedBroadcastMessage> _itemUsedBroadcastPublisher;
        private static readonly bool _enableDebugLogs = true;

        /// <summary>
        /// Publisher 설정 (DI Container에서 호출)
        /// </summary>
        public static void Initialize(IPublisher<ItemUsedBroadcastMessage> itemUsedBroadcastPublisher)
        {
            _itemUsedBroadcastPublisher = itemUsedBroadcastPublisher;
            Debug.Log("[ItemUsageNetworkEventHandler] Publisher 설정 완료");
        }

        /// <summary>
        /// 아이템 사용 응답 브로드캐스트 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.MonggingItemUseResponse)]
        public static void OnItemUseResponse(
            MonggingItemUseCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                if (command == null)
                {
                    Debug.LogWarning("[ItemUsageNetworkEventHandler] MonggingItemUseCommand가 null입니다.");
                    return;
                }

                if (_enableDebugLogs)
                {
                    string resultMessage = command.Result switch
                    {
                        MonggingItemUseResult.Success => "성공",
                        MonggingItemUseResult.Miss => "빗나감",
                        MonggingItemUseResult.Fail => "실패",
                        MonggingItemUseResult.ItemNotFound => "아이템 없음",
                        MonggingItemUseResult.NotMongging => "몽깅이가 아님",
                        MonggingItemUseResult.NoMongdung => "몽둥이 없음",
                        MonggingItemUseResult.NotAttackItem => "공격 아이템 아님",
                        MonggingItemUseResult.ItemNotOwned => "아이템 소유 안 함",
                        _ => $"알 수 없는 결과: {command.Result}"
                    };

                    Debug.Log($"[ItemUsageNetworkEventHandler] 📡 아이템 사용 응답: ItemId={command.itemId}, Result={command.Result}({resultMessage}), Position={command.position}");
                }

                // MessagePipe를 통해 브로드캐스트 메시지 발행
                if (_itemUsedBroadcastPublisher != null)
                {
                    var message = new ItemUsedBroadcastMessage(command.itemId, command.position, command.Result);
                    _itemUsedBroadcastPublisher.Publish(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[ItemUsageNetworkEventHandler] ItemUsedBroadcastMessage 발행 완료");
                    }
                }
                else
                {
                    Debug.LogError("[ItemUsageNetworkEventHandler] ItemUsedBroadcastPublisher가 설정되지 않음!");
                }

                // 인벤토리 동기화: 성공/Miss일 때 아이템 제거
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null && command.Success)
                {
                    var itemRemovedMessage = new ItemRemovedMessage
                    {
                        ItemId = command.itemId.ToString(),
                        Count = 1,
                        Success = true,
                    };
                    bridge.PublishMessage(itemRemovedMessage);

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[ItemUsageNetworkEventHandler] 아이템 사용 성공 → 인벤토리 동기화: ItemId={command.itemId}");
                    }
                }
                else if (bridge == null)
                {
                    Debug.LogError("[ItemUsageNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemUsageNetworkEventHandler] 아이템 사용 브로드캐스트 처리 실패: {e.Message}");
            }
        }
    }
}