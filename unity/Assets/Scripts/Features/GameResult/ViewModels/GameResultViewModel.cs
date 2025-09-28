using System;
using System.Linq;
using Features.GameResult.Models;
using Features.GameResult.Services;
using Features.PlayerList.Services;
using Features.Player.Services;
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
        private readonly IGameResultService _gameResultService;
        private readonly PlayerManagerService _playerManagerService;

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
        public GameResultViewModel(IPlayerListService playerListService, IGameResultService gameResultService, PlayerManagerService playerManagerService)
        {
            _playerListService = playerListService;
            _gameResultService = gameResultService;
            _playerManagerService = playerManagerService;

            // GameResultService 이벤트 구독
            _gameResultService.OnGameResultUpdated += HandleGameResultUpdated;
            _gameResultService.OnShowGameResult += HandleShowGameResult;

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
                CoinReward = 0, // 서버에서 받은 실제 코인 값 사용
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
            // UI는 숨기지 않고 로비 이동만 처리
            _gameResultService?.ReturnToLobby();
            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultViewModel] 계속 버튼 클릭 - 로비 이동 시작");
            }
        }

        /// <summary>
        /// GameResultService에서 게임 결과 업데이트 이벤트 처리
        /// </summary>
        private void HandleGameResultUpdated(GameResultData resultData)
        {
            Debug.Log($"[GameResultViewModel] ===== GameResultData 수신 =====");
            Debug.Log($"[GameResultViewModel] Result: {resultData.Result}");
            Debug.Log($"[GameResultViewModel] EscapedCount: {resultData.EscapedMonggingCount}");
            Debug.Log($"[GameResultViewModel] CoinReward: {resultData.CoinReward}");

            // GameResultData를 기존 GameResultModel 구조로 변환
            var gameResult = ConvertToGameResultModel(resultData);

            Debug.Log($"[GameResultViewModel] ===== GameResultModel 변환 완료 =====");
            Debug.Log($"[GameResultViewModel] TeamResult: {gameResult.TeamResult}");
            Debug.Log($"[GameResultViewModel] EscapedCount: {gameResult.EscapedCount}");
            Debug.Log($"[GameResultViewModel] CoinReward: {gameResult.CoinReward}");

            SetGameResult(gameResult);
        }

        /// <summary>
        /// GameResultService에서 게임 결과 표시 이벤트 처리
        /// </summary>
        private void HandleShowGameResult()
        {
            ShowResult();
        }

        /// <summary>
        /// GameResultData를 GameResultModel로 변환
        /// </summary>
        private GameResultModel ConvertToGameResultModel(GameResultData resultData)
        {
            // 로컬 플레이어의 코인 찾기
            int localPlayerCoin = GetLocalPlayerCoin(resultData);

            var gameResult = new GameResultModel
            {
                TeamResult = resultData.Result,
                EscapedCount = resultData.EscapedMonggingCount,
                CoinReward = localPlayerCoin, // 로컬 플레이어의 코인 사용
                MonggingPlayers = new PlayerResultModel[4]
            };

            if (_enableDebugLogs)
            {
                Debug.Log($"[GameResultViewModel] 로컬 플레이어 코인: {localPlayerCoin}");
            }

            // 플레이어 결과 변환
            int monggingIndex = 0;
            foreach (var playerResult in resultData.PlayerResults)
            {
                var playerModel = new PlayerResultModel
                {
                    PlayerId = playerResult.Id,
                    Status = playerResult.Status,
                    IsMongging = true, // TODO: 실제 역할 정보로 수정 필요
                    PlayerName = GetPlayerNickname(playerResult.Id)
                };

                if (monggingIndex < 4)
                {
                    gameResult.MonggingPlayers[monggingIndex++] = playerModel;
                }
            }

            return gameResult;
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
        /// 플레이어 닉네임 조회 (PlayerManagerService 사용)
        /// </summary>
        private string GetPlayerNickname(long playerId)
        {
            if (_playerManagerService != null)
            {
                var playerInfo = _playerManagerService.GetPlayerInfo(playerId);
                if (playerInfo != null)
                {
                    return playerInfo.NickName;
                }
            }

            // 백업으로 PlayerListService 사용
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

        /// <summary>
        /// 로컬 플레이어의 코인을 찾아서 반환
        /// </summary>
        private int GetLocalPlayerCoin(GameResultData resultData)
        {
            if (_playerManagerService == null || resultData?.PlayerResults == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning("[GameResultViewModel] PlayerManagerService 또는 PlayerResults가 null입니다.");
                }
                return 0;
            }

            // 로컬 플레이어 ID 가져오기
            var localPlayerInfo = _playerManagerService.GetLocalPlayer();
            if (localPlayerInfo == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning("[GameResultViewModel] 로컬 플레이어 정보를 찾을 수 없습니다.");
                }
                return 0;
            }

            long localPlayerId = localPlayerInfo.Id;

            if (_enableDebugLogs)
            {
                Debug.Log($"[GameResultViewModel] 로컬 플레이어 ID: {localPlayerId}");
                Debug.Log($"[GameResultViewModel] 서버 플레이어 결과 개수: {resultData.PlayerResults.Count}");
            }

            // 서버에서 받은 PlayerResults에서 로컬 플레이어 찾기
            foreach (var playerResult in resultData.PlayerResults)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[GameResultViewModel] 플레이어 확인: ID={playerResult.Id}, Coin={playerResult.Coin}");
                }

                if (playerResult.Id == localPlayerId)
                {
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[GameResultViewModel] 로컬 플레이어 발견! 코인: {playerResult.Coin}");
                    }
                    return playerResult.Coin;
                }
            }

            if (_enableDebugLogs)
            {
                Debug.LogWarning($"[GameResultViewModel] 로컬 플레이어 ID {localPlayerId}를 서버 결과에서 찾을 수 없습니다.");
            }
            return 0;
        }

        #endregion

        public void Dispose()
        {
            // GameResultService 이벤트 구독 해제
            if (_gameResultService != null)
            {
                _gameResultService.OnGameResultUpdated -= HandleGameResultUpdated;
                _gameResultService.OnShowGameResult -= HandleShowGameResult;
            }

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