using System;
using DotNetty.Transport.Channels;
using Features.Chest.Messages;
using Networks.Attributes;
using Networks.chests;
using Networks.Packets;
using UnityEngine;

namespace Features.Chest.NetworkSources
{
    /// <summary>
    /// 상자 관련 서버 이벤트를 처리하는 Static 핸들러
    /// CommandHandler 어트리뷰트를 사용하여 서버 푸시 이벤트를 수신하고
    /// MessagePipeBridge를 통해 MessagePipe 이벤트로 변환
    /// </summary>
    public class ChestNetworkEventHandler
    {
        private static readonly bool _enableDebugLogs = true;

        /// <summary>
        /// 상자 열림 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.ChestOpenResponse)]
        public static void ChestOpen(ChestOpenCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (command != null)
                {
                    string itemsInfo = command.Items != null && command.Items.Count > 0
                        ? $"[{string.Join(", ", command.Items)}]"
                        : "없음";

                    Debug.Log($"[상자열기응답] ID:{command.ChestId} | 성공:{command.Success} | 아이템:{itemsInfo}");

                    if (command.Success)
                    {
                        // 성공한 경우 MessagePipeBridge를 통해 데이터 동기화 메시지 발행
                        var syncMessage = new Features.Chest.Messages.ChestServerDataSyncMessage(command.ChestId, command.Items);
                        Networks.MessagePipeBridge.Instance?.PublishMessage(syncMessage);
                    }
                    else
                    {
                        // 실패한 경우 상자 닫기 메시지 발행
                        var closeMessage = new Features.Chest.Messages.ChestClosedMessage(command.ChestId);
                        Networks.MessagePipeBridge.Instance?.PublishMessage(closeMessage);
                    }
                }

                // NetworkApi.HandleResponse를 통해 대기 중인 요청에 응답 전달
                var networkApi = Networks.NetworkApi.Instance;
                if (networkApi != null)
                {
                    networkApi.HandleResponse(command);
                }
                else
                {
                    Debug.LogError("[ChestNetworkEventHandler] NetworkApi Instance를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestNetworkEventHandler] 상자 열림 이벤트 처리 실패");
                Debug.LogError($"[ChestNetworkEventHandler] 예외 타입: {e.GetType().Name}");
                Debug.LogError($"[ChestNetworkEventHandler] 예외 메시지: {e.Message}");
                Debug.LogError($"[ChestNetworkEventHandler] 스택 트레이스: {e.StackTrace}");
                Debug.LogError($"[ChestNetworkEventHandler] Command 정보: Success={command?.Success}, ChestId={command?.ChestId}, ItemSize={command?.ItemSize}");
                if (e.InnerException != null)
                {
                    Debug.LogError($"[ChestNetworkEventHandler] 내부 예외: {e.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// 상자 닫힘 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.ChestCloseResponse)]
        public static void ChestClose(ChestCloseCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                if (command != null)
                {
                    Debug.Log($"[상자닫기응답] 성공:{command.Success}");
                }

                // NetworkApi.HandleResponse를 통해 대기 중인 요청에 응답 전달
                var networkApi = Networks.NetworkApi.Instance;
                if (networkApi != null)
                {
                    networkApi.HandleResponse(command);
                }
                else
                {
                    Debug.LogError("[ChestNetworkEventHandler] NetworkApi Instance를 찾을 수 없음!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChestNetworkEventHandler] 상자 닫힘 이벤트 처리 실패: {e.Message}");
            }
        }


    }
}