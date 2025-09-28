using System;
using DotNetty.Transport.Channels;
using Features.MainGame.Services;
using Features.GameResult.Models;
using Features.Notification.Services;
using Networks;
using Networks.Attributes;
using Networks.Game;
using Networks.Packets;
using Networks.Rooms;
using UnityEngine;

namespace Features.MainGame.NetworkSources
{
    /// <summary>
    /// 메인 게임 관련 네트워크 이벤트를 처리하는 핸들러
    /// static CommandHandler들만 가지고 있는 순수 핸들러 클래스
    /// </summary>
    public static class MainGameNetworkEventHandler
    {
        // static CommandHandler를 위한 static 서비스 저장소
        private static IMainGameService _mainGameService;
        private static INotificationService _notificationService;

        /// <summary>
        /// 서비스들 설정 (DI Container에서 호출)
        /// </summary>
        public static void Initialize(IMainGameService mainGameService, INotificationService notificationService)
        {
            _mainGameService = mainGameService;
            _notificationService = notificationService;
            Debug.Log($"[MainGameNetworkEventHandler] ===== 서비스들 설정 완료 ===== MainGameService: {_mainGameService != null}, NotificationService: {_notificationService != null}");
        }

        /// <summary>
        /// 게임 시작 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GameStart)]
        public static void GameStart(GameStartCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log($"[MainGameNetworkEventHandler] ===== 게임 시작 패킷 수신 ===== Command: {command != null}, Context: {ctx != null}");

                if (_mainGameService != null)
                {
                    _mainGameService.StartGame();
                    Debug.Log("[MainGameNetworkEventHandler] MainGameService.StartGame() 호출 완료");
                }
                else
                {
                    Debug.LogError("[MainGameNetworkEventHandler] MainGameService가 설정되지 않음");
                }

                // 게임 시작 알림 추가 표시 (MainGameService에서도 표시하지만 추가로)
                if (_notificationService != null)
                {
                    _notificationService.ShowNotification("모든 플레이어가 준비되었습니다!", 2f);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainGameNetworkEventHandler] 게임 시작 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 종료 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GameEnd)]
        public static void GameEnd(GameEndCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log(
                    $"[MainGameNetworkEventHandler] 게임 종료 수신: Result={command.result}, PlayerCount={command.playerResults?.Count ?? 0}, EscapedCount={command.escapedMonggingCount}"
                );

                // 플레이어 결과 출력
                if (command.playerResults != null)
                {
                    foreach (var playerResult in command.playerResults)
                    {
                        Debug.Log(
                            $"  - 플레이어 결과: Id={playerResult.id}, Status={playerResult.status}"
                        );
                    }
                }

                if (_mainGameService != null)
                {
                    // GameEndCommand의 result를 TeamResult로 변환
                    TeamResult teamResult = ConvertToTeamResult(command.result);
                    string reason = $"서버 게임 종료 - 탈출한 몽깅이: {command.escapedMonggingCount}명";

                    _mainGameService.EndGame(teamResult, reason);

                    // NotificationService로 게임 결과 알림 표시
                    ShowGameResultNotification(teamResult, command.escapedMonggingCount);

                    Debug.Log("[MainGameNetworkEventHandler] MainGameService.EndGame() 호출 완료");
                }
                else
                {
                    Debug.LogError("[MainGameNetworkEventHandler] MainGameService가 설정되지 않음");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MainGameNetworkEventHandler] 게임 종료 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 서버 게임 결과를 TeamResult로 변환
        /// </summary>
        private static TeamResult ConvertToTeamResult(int serverResult)
        {
            // 서버 결과 값에 따른 변환 로직 (실제 서버 스펙에 맞게 조정 필요)
            return serverResult switch
            {
                0 => TeamResult.MonggingWin,    // 몽깅이 승리
                1 => TeamResult.MongdungWin,    // 몽둥이 승리
                _ => TeamResult.MonggingWin     // 기본값
            };
        }

        /// <summary>
        /// 게임 결과를 NotificationService로 알림 표시
        /// </summary>
        private static void ShowGameResultNotification(TeamResult teamResult, int escapedMonggingCount)
        {
            if (_notificationService == null)
            {
                Debug.LogWarning("[MainGameNetworkEventHandler] NotificationService가 설정되지 않아 게임 결과 알림을 표시할 수 없습니다");
                return;
            }

            string resultMessage;
            switch (teamResult)
            {
                case TeamResult.MonggingWin:
                    resultMessage = $"🎉 몽깅이 승리! {escapedMonggingCount}명이 탈출했습니다!";
                    _notificationService.ShowSuccessNotification(resultMessage);
                    break;

                case TeamResult.MongdungWin:
                    resultMessage = $"💀 몽둥이 승리! 아무도 탈출하지 못했습니다.";
                    _notificationService.ShowNotification(resultMessage, 5f);
                    break;

                default:
                    resultMessage = "게임이 종료되었습니다.";
                    _notificationService.ShowNotification(resultMessage, 3f);
                    break;
            }

            Debug.Log($"[MainGameNetworkEventHandler] 게임 결과 알림 표시: {resultMessage}");
        }
    }
}