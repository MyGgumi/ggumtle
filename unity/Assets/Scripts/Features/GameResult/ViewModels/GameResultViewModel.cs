using System;
using System.Linq;
using Features.GameResult.Models;
using Features.PlayerList.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.GameResult.ViewModels
{
    /// <summary>
    /// 게임 결과 UI 상태를 관리하는 ViewModel
    /// </summary>
    public class GameResultViewModel : IDisposable
    {
        private readonly bool _enableDebugLogs = true;

        // Dependencies
        private readonly IPlayerListService _playerListService;

        // UI State
        private readonly ReactiveProperty<bool> _isVisible = new(false);
        private readonly ReactiveProperty<string> _resultTitle = new("게임 종료");
        private readonly ReactiveProperty<int> _escapedCount = new(0);
        private readonly ReactiveProperty<int> _coinReward = new(0);

        // Game Result Data
        private readonly ReactiveProperty<GameResultModel> _gameResult = new();

        // Public Properties
        public ReadOnlyReactiveProperty<bool> IsVisible => _isVisible;
        public ReadOnlyReactiveProperty<string> ResultTitle => _resultTitle;
        public ReadOnlyReactiveProperty<int> EscapedCount => _escapedCount;
        public ReadOnlyReactiveProperty<int> CoinReward => _coinReward;
        public ReadOnlyReactiveProperty<GameResultModel> GameResult => _gameResult;

        [Inject]
        public GameResultViewModel(IPlayerListService playerListService)
        {
            _playerListService = playerListService;

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultViewModel] 초기화 완료");
            }
        }

        /// <summary>
        /// 게임 결과 데이터 설정
        /// </summary>
        public void SetGameResult(GameResultModel resultData)
        {
            // 플레이어 정보 보완 (닉네임, 역할, 색상)
            EnrichPlayerData(ref resultData);

            // 데이터 업데이트
            _gameResult.Value = resultData;

            // UI 상태 업데이트
            _resultTitle.Value = GetResultTitle(resultData.TeamResult);
            _escapedCount.Value = resultData.EscapedCount;
            _coinReward.Value = resultData.CoinReward;

            if (_enableDebugLogs)
            {
                Debug.Log($"[GameResultViewModel] 게임 결과 설정: {_resultTitle.Value}, 탈출: {_escapedCount.Value}명");
            }
        }

        /// <summary>
        /// 서버 패킷 데이터를 GameResultModel로 변환
        /// 패킷 구조: result(byte), playerCount(int), playerData[playerId, status, isMongging, classId]
        /// </summary>
        public void ProcessServerPacket(byte result, int playerCount, long[][] playerData)
        {
            var gameResult = new GameResultModel
            {
                TeamResult = (TeamResult)result,
                EscapedCount = 0,
                CoinReward = 150, // TODO: 서버에서 실제 코인 값 받기
                MonggingPlayers = new PlayerResultModel[4]
            };

            // 플레이어 데이터 파싱
            int monggingIndex = 0;
            for (int i = 0; i < playerCount && i < playerData.Length; i++)
            {
                long playerId = playerData[i][0];
                int status = (int)playerData[i][1];
                byte isMonggingByte = (byte)playerData[i][2];
                long classId = playerData[i][3];

                var playerResult = new PlayerResultModel
                {
                    PlayerId = playerId,
                    Status = (PlayerStatus)status,
                    IsMongging = isMonggingByte == 1,
                    PlayerName = GetPlayerNickname(playerId)
                };

                // 색상 결정 (몽깅이인 경우에만)
                if (playerResult.IsMongging)
                {
                    playerResult.MonggingColor = GetMonggingColorFromClassId(classId);
                }

                // 역할에 따라 배치
                if (!playerResult.IsMongging) // 몽둥이
                {
                    gameResult.MongdungPlayer = playerResult;
                }
                else if (monggingIndex < 4) // 몽깅이
                {
                    gameResult.MonggingPlayers[monggingIndex++] = playerResult;
                }

                // 탈출한 플레이어 수 계산
                if (status == 1) // Escaped
                {
                    gameResult.EscapedCount++;
                }
            }

            SetGameResult(gameResult);
        }

        /// <summary>
        /// 게임 결과 UI 표시
        /// </summary>
        public void ShowResult()
        {
            _isVisible.Value = true;
            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultViewModel] 게임 결과 UI 표시");
            }
        }

        /// <summary>
        /// 게임 결과 UI 숨기기
        /// </summary>
        public void HideResult()
        {
            _isVisible.Value = false;
            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultViewModel] 게임 결과 UI 숨김");
            }
        }

        /// <summary>
        /// 계속 버튼 클릭 처리
        /// </summary>
        public void OnContinueClicked()
        {
            HideResult();
            // TODO: 로비로 이동 또는 다음 게임 시작 로직
            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultViewModel] 계속 버튼 클릭");
            }
        }

        #region Private Methods

        /// <summary>
        /// 플레이어 데이터 보완 (닉네임, 색상 등)
        /// </summary>
        private void EnrichPlayerData(ref GameResultModel resultData)
        {
            // 몽둥이 플레이어 정보 보완
            if (resultData.MongdungPlayer != null)
            {
                resultData.MongdungPlayer.PlayerName = GetPlayerNickname(resultData.MongdungPlayer.PlayerId);
                resultData.MongdungPlayer.IsMongging = false;
                // 몽둥이는 항상 생존 상태
                resultData.MongdungPlayer.Status = PlayerStatus.Escaped;
            }

            // 몽깅이 플레이어들 정보 보완
            if (resultData.MonggingPlayers != null)
            {
                for (int i = 0; i < resultData.MonggingPlayers.Length; i++)
                {
                    if (resultData.MonggingPlayers[i] != null)
                    {
                        var player = resultData.MonggingPlayers[i];
                        player.PlayerName = GetPlayerNickname(player.PlayerId);
                        player.IsMongging = true;
                        player.MonggingColor = GetMonggingColor(player.PlayerId);
                        resultData.MonggingPlayers[i] = player;
                    }
                }
            }
        }

        /// <summary>
        /// 플레이어 닉네임 조회
        /// </summary>
        private string GetPlayerNickname(long playerId)
        {
            if (_playerListService != null)
            {
                int id = (int)playerId;
                var player = _playerListService.GetPlayer(id);
                if (player != null)
                {
                    return player.nickname;
                }
            }
            return $"Player{playerId}";
        }

        /// <summary>
        /// 몽둥이 플레이어인지 확인
        /// TODO: PlayerListService에서 실제 역할 정보 조회하도록 수정
        /// </summary>
        private bool IsMongdungPlayer(long playerId)
        {
            if (_playerListService != null)
            {
                int id = (int)playerId;
                // TODO: PlayerListData에 역할 정보가 추가되면 여기서 조회
                // 임시 로직: 첫 번째 플레이어를 몽둥이로 설정
                var allPlayers = _playerListService.GetAllPlayersList();
                if (allPlayers.Count > 0 && id == allPlayers.OrderBy(p => p.playerId).First().playerId)
                {
                    return true;  // 몽둥이
                }
            }
            return false;  // 몽깅이
        }

        /// <summary>
        /// classId를 기반으로 몽깅이 색상 결정
        /// TODO: 실제 서버 숫자-색상 맵핑 확인 후 통일 필요
        /// </summary>
        private MonggingColor GetMonggingColorFromClassId(long classId)
        {
            // classId를 MonggingColor enum으로 변환
            // TODO: 실제 숫자 맵핑 확인 필요 (1=Mint?, 2=Yellow?, 3=Purple?)
            return classId switch
            {
                1 => MonggingColor.Mint,
                2 => MonggingColor.Yellow,
                3 => MonggingColor.Purple,
                _ => MonggingColor.Mint // 기본값
            };
        }

        /// <summary>
        /// 몽깅이 색상 조회 (PlayerListService 기반 - 백업용)
        /// </summary>
        private MonggingColor GetMonggingColor(long playerId)
        {
            if (_playerListService != null)
            {
                int id = (int)playerId;
                var playerData = _playerListService.GetPlayer(id);
                if (playerData != null)
                {
                    // colorTheme 문자열을 MonggingColor enum으로 변환
                    return playerData.colorTheme?.ToLower() switch
                    {
                        "mint" => MonggingColor.Mint,
                        "yellow" => MonggingColor.Yellow,
                        "purple" => MonggingColor.Purple,
                        _ => MonggingColor.Mint
                    };
                }
            }

            // 못 찾으면 ID 기반으로 색상 할당 (3가지 색상 순환)
            long colorIndex = playerId % 3;
            return (MonggingColor)(colorIndex + 1);
        }

        /// <summary>
        /// 결과 제목 텍스트 생성
        /// </summary>
        private string GetResultTitle(TeamResult result)
        {
            return result switch
            {
                TeamResult.MonggingWin => "몽깅이 승리",
                TeamResult.MongdungWin => "몽둥이 승리",
                _ => "게임 종료"
            };
        }

        #endregion

        public void Dispose()
        {
            _isVisible?.Dispose();
            _resultTitle?.Dispose();
            _escapedCount?.Dispose();
            _coinReward?.Dispose();
            _gameResult?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultViewModel] 해제됨");
            }
        }
    }
}