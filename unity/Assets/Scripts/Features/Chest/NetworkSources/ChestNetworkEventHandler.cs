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
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkEventHandler] 상자 열림 이벤트: Success={command.Success}, Result={command.Result}");
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
                Debug.LogError($"[ChestNetworkEventHandler] 상자 열림 이벤트 처리 실패: {e.Message}");
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
                if (_enableDebugLogs)
                {
                    Debug.Log($"[ChestNetworkEventHandler] 상자 닫힘 이벤트: Success={command.Success}, Result={command.Result}");
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