using System;
using DotNetty.Transport.Channels;
using Features.MainGame.Services;
using Features.GameResult.Models;
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
        // static CommandHandler를 위한 static MainGameService 저장소
        private static IMainGameService _mainGameService;

        /// <summary>
        /// MainGameService 설정 (DI Container에서 호출)
        /// </summary>
        public static void Initialize(IMainGameService mainGameService)
        {
            _mainGameService = mainGameService;
            Debug.Log("[MainGameNetworkEventHandler] MainGameService 설정 완료");
        }

        /// <summary>
        /// 게임 시작 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GameStart)]
        public static void GameStart(GameStartCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log("[MainGameNetworkEventHandler] 게임 시작 수신");

                if (_mainGameService != null)
                {
                    _mainGameService.StartGame();
                    Debug.Log("[MainGameNetworkEventHandler] MainGameService.StartGame() 호출 완료");
                }
                else
                {
                    Debug.LogError("[MainGameNetworkEventHandler] MainGameService가 설정되지 않음");
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
    }
}