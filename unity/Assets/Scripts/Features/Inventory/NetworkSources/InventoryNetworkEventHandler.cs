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
                if (_enableDebugLogs)
                {
                    // TODO: 서버 응답 구조 변경 예정으로 임시 로그
                    Debug.Log(
                        $"[InventoryNetworkEventHandler] 아이템 획득 이벤트 수신 (서버 응답 구조 변경 예정)"
                    );
                }

                // MessagePipeBridge를 통해 이벤트 발행
                var bridge = Networks.MessagePipeBridge.Instance;
                if (bridge != null)
                {
                    // TODO: 서버 응답 구조 변경 후 다시 구현 예정
                    var message = new ItemAddedMessage
                    {
                        ItemId = "unknown", // TODO: 서버 응답에서 ItemId 추출
                        Count = 1, // TODO: 서버 응답에서 Count 추출
                        ChestId = 0, // TODO: 서버 응답에서 ChestId 추출
                        SlotIndex = 0, // TODO: 서버 응답에서 SlotIndex 추출
                        Success = true, // TODO: 서버 응답에서 Success 상태 추출
                    };
                    bridge.PublishMessage(message);

                    if (_enableDebugLogs)
                    {
                        Debug.Log(
                            "[InventoryNetworkEventHandler] 아이템 획득 메시지 발행 성공 → InventoryService"
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

        /// <summary>
        /// 아이템 사용 응답 처리
        /// </summary>
        [CommandHandler(PacketType.MonggingItemUseResponse)]
        public static void OnItemUsed(MonggingItemUseCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    // TODO: 서버 응답 구조 변경 예정으로 임시 로그
                    Debug.Log(
                        $"[InventoryNetworkEventHandler] 아이템 사용 이벤트 수신 (서버 응답 구조 변경 예정)"
                    );
                }

                // var bridge = Networks.MessagePipeBridge.Instance;
                // if (bridge != null)
                // {
                //     // TODO: 서버 응답 구조 변경 후 다시 구현 예정
                //     var message = new ItemUsedMessage
                //     {
                //         ItemId = "unknown", // TODO: 서버 응답에서 ItemId 추출
                //         Success = true, // TODO: 서버 응답에서 Success 상태 추출
                //         Result = "success", // TODO: 서버 응답에서 Result 추출
                //     };
                //     bridge.PublishMessage(message);

                //     if (_enableDebugLogs)
                //     {
                //         Debug.Log(
                //             "[InventoryNetworkEventHandler] 아이템 사용 메시지 발행 성공 → InventoryService"
                //         );
                //     }
                // }
                // else
                // {
                //     Debug.LogError(
                //         "[InventoryNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!"
                //     );
                // }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[InventoryNetworkEventHandler] 아이템 사용 이벤트 처리 실패: {e.Message}"
                );
            }
        }

        /// <summary>
        /// 필드 아이템 사용 응답 처리
        /// </summary>
        [CommandHandler(PacketType.MonggingFieldItemUseResponse)]
        public static void OnFieldItemUsed(
            MonggingFieldItemUseCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                if (_enableDebugLogs)
                {
                    // TODO: 서버 응답 구조 변경 예정으로 임시 로그
                    Debug.Log(
                        $"[InventoryNetworkEventHandler] 필드 아이템 사용 이벤트 수신 (서버 응답 구조 변경 예정)"
                    );
                }

                // var bridge = Networks.MessagePipeBridge.Instance;
                // if (bridge != null)
                // {
                //     // TODO: 서버 응답 구조 변경 후 다시 구현 예정
                //     var message = new FieldItemUsedMessage
                //     {
                //         FieldItemId = "unknown", // TODO: 서버 응답에서 FieldItemId 추출
                //         Success = true, // TODO: 서버 응답에서 Success 상태 추출
                //         Result = "success" // TODO: 서버 응답에서 Result 추출
                //     };
                //     bridge.PublishMessage(message);

                //     if (_enableDebugLogs)
                //     {
                //         Debug.Log("[InventoryNetworkEventHandler] 필드 아이템 사용 메시지 발행 성공 → InventoryService");
                //     }
                // }
                // else
                // {
                //     Debug.LogError("[InventoryNetworkEventHandler] MessagePipeBridge를 찾을 수 없음!");
                // }
            }
            catch (Exception e)
            {
                Debug.LogError(
                    $"[InventoryNetworkEventHandler] 필드 아이템 사용 이벤트 처리 실패: {e.Message}"
                );
            }
        }
    }
}
