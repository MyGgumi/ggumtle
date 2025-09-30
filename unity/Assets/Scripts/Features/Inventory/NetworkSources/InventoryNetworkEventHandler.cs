using System;
using DotNetty.Transport.Channels;
using Features.Inventory.Messages;
using Networks.Attributes;
using Networks.Chests;
using Networks.Packets;
using Networks.Players;
using UnityEngine;

namespace Features.Inventory.NetworkSources
{
    /// <summary>
    /// 인벤토리 관련 서버 이벤트를 처리하는 Static 핸들러
    /// CommandHandler 어트리뷰트를 사용하여 서버 푸시 이벤트를 수신하고
    /// MessagePipeBridge를 통해 MessagePipe 이벤트로 변환
    /// </summary>
    public class InventoryNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = true;

        /// <summary>
        /// 아이템 획득 응답 처리
        /// </summary>
        [CommandHandler(PacketType.GetItemResponse)]
        public static void OnGetItem(GetItemCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                string resultMessage = command.Result switch
                {
                    0 => "실패",
                    1 => "성공",
                    2 => "인덱스 범위 오류",
                    3 => "상자 없음",
                    4 => "플레이어 없음",
                    5 => "몽깅이가 아님",
                    10 => "아이템 없음",
                    11 => "인벤토리 가득참",
                    12 => "상자가 멀음",
                    _ => "알 수 없는 오류",
                };

                if (_enableDebugLogs)
                {
                    Debug.Log(
                        $"[InventoryNetworkEventHandler] 아이템 획득 응답: {resultMessage} (Result={command.Result}), ChestId={command.ChestId}, ItemId={command.ItemId}"
                    );
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    bool success = command.Result == 1;

                    if (success)
                    {
                        // 성공 시: 아이템 추가 + 상자 상태 동기화
                        var itemMessage = new ItemAddedMessage
                        {
                            ItemId = command.ItemId.ToString(),
                            Count = 1,
                            ChestId = command.ChestId,
                            SlotIndex = -1, // -1로 설정해서 빈 슬롯 자동 찾기
                            Success = true,
                        };
                        if (_enableDebugLogs)
                            Debug.Log(
                                $"[디버그] ItemAddedMessage 발행: 아이템ID={command.ItemId}, 성공=true"
                            );
                        bridge.PublishMessage(itemMessage);

                        // 상자 상태 동기화 (모든 플레이어에게 전송됨)
                        var syncMessage = new Features.Chest.Messages.ChestServerDataSyncMessage(
                            command.ChestId,
                            command.Items
                        );
                        if (_enableDebugLogs)
                            Debug.Log(
                                $"[디버그] ChestServerDataSyncMessage 발행: 상자ID={command.ChestId}"
                            );
                        bridge.PublishMessage(syncMessage);

                        if (_enableDebugLogs)
                        {
                            Debug.Log(
                                $"[InventoryNetworkEventHandler] 아이템 획득 성공: ItemId={command.ItemId}, 상자 동기화 완료"
                            );
                        }
                    }
                    else
                    {
                        // 실패 시: 실패 상태로 ItemAddedMessage 발행 (feeding 증가 방지)
                        var failedItemMessage = new ItemAddedMessage
                        {
                            ItemId = command.ItemId.ToString(),
                            Count = 1,
                            ChestId = command.ChestId,
                            SlotIndex = -1,
                            Success = false, // 실패 상태로 설정
                        };
                        bridge.PublishMessage(failedItemMessage);

                        // 오류 메시지 표시
                        var errorMessage = new Features.Notification.Messages.NotificationMessage(
                            resultMessage,
                            3f,
                            Features.Notification.Models.NotificationType.Error
                        );
                        bridge.PublishMessage(errorMessage);

                        if (_enableDebugLogs)
                        {
                            Debug.Log(
                                $"[InventoryNetworkEventHandler] 아이템 획득 실패: {resultMessage} (ItemId={command.ItemId})"
                            );
                        }
                    }
                }
                else
                {
                    Debug.LogError(
                        "[InventoryNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[InventoryNetworkEventHandler] 아이템 획득 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 아이템 넣기 응답 처리
        /// </summary>
        [CommandHandler(PacketType.PutItemResponse)]
        public static void OnPutItem(PutItemCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    // TODO: 서버 응답 구조 변경 예정으로 임시 로그
                    Debug.Log(
                        $"[InventoryNetworkEventHandler] 아이템 넣기 이벤트 수신 (서버 응답 구조 변경 예정)"
                    );
                }

                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // TODO: 서버 응답 구조 변경 후 다시 구현 예정
                    var message = new ItemRemovedMessage
                    {
                        ItemId = "unknown", // TODO: 서버 응답에서 ItemId 추출
                        Count = 1, // TODO: 서버 응답에서 Count 추출
                        Success = true, // TODO: 서버 응답에서 Success 상태 추출
                    };
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            "[InventoryNetworkEventHandler] 아이템 넣기 메시지 발행 성공 → InventoryService"
                        );
                    }
                }
                else
                {
                    Debug.LogError(
                        "[InventoryNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                    );
                }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[InventoryNetworkEventHandler] 아이템 넣기 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        // TODO: 인벤토리 동기화는 별도의 패킷으로 구현 필요
        // 현재는 GetItem/PutItem 응답으로 대체

        // NOTE: 아이템 사용 응답(MonggingItemUseResponse)은 ItemUsageNetworkEventHandler에서 처리
        // ItemUsageNetworkEventHandler가 ItemRemovedMessage를 발행하여 인벤토리 동기화
    }
}
