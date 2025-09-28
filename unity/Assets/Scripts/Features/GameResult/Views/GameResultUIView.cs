using System;
using System.Collections.Generic;
using Features.GameResult.Models;
using Features.GameResult.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VContainer;

namespace Features.GameResult.Views
{
    /// <summary>
    /// 게임 결과 UI View
    /// GameResultViewModel과 연동하여 게임 결과 화면 표시
    /// HUDInitializer의 root를 사용하는 통합형 방식
    /// </summary>
    public class GameResultUIView : MonoBehaviour
    {
        private readonly bool _enableDebugLogs = true;

        // Dependencies
        private GameResultViewModel _viewModel;

        // UI Elements
        private VisualElement _root;
        private VisualElement _gameResultContainer;

        // Result UI Elements
        private Label _resultTitle;
        private Label _escapeCount;
        private Label _escapeText;
        private Label _coinAmount;
        private Button _continueButton;
        private VisualElement _background;
        private VisualElement _coinIcon;

        // Player UI Elements
        private VisualElement _mongdungPlayer;
        private VisualElement _mongdungCharacter;
        private Label _mongdungNickname;
        private Label _mongdungStatusText;

        private readonly List<VisualElement> _monggingPlayers = new();
        private readonly List<Label> _monggingNicknames = new();
        private readonly List<Label> _monggingStatusTexts = new();
        private readonly List<VisualElement> _monggingStatusBubbles = new();
        private readonly List<VisualElement> _monggingCharacters = new();

        // R3 Disposables
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(GameResultViewModel viewModel)
        {
            _viewModel = viewModel;

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] ViewModel 주입 완료");
            }
        }

        public void Initialize(VisualElement root)
        {
            _root = root;
            FindUIElements();
            BindViewModel();
            SetupEventHandlers();

            // 초기에는 숨김
            HideGameResult();

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] 초기화 완료");
            }
        }

        private void FindUIElements()
        {
            // 게임 결과 컨테이너 찾기
            _gameResultContainer = _root.Q<VisualElement>("gameResultUI");

            if (_gameResultContainer == null)
            {
                Debug.LogError("[GameResultUIView] gameResultUI 컨테이너를 찾을 수 없습니다.");
                return;
            }

            // 결과 UI 요소들 찾기
            _resultTitle = _gameResultContainer.Q<Label>("GameResultTitle");
            _escapeCount = _gameResultContainer.Q<Label>("EscapeCount");
            _escapeText = _gameResultContainer.Q<Label>("EscapeText");
            _coinAmount = _gameResultContainer.Q<Label>("CoinAmount");
            _continueButton = _gameResultContainer.Q<Button>("ContinueButton");
            _background = _gameResultContainer.Q<VisualElement>("Background");
            _coinIcon = _gameResultContainer.Q<VisualElement>("CoinIcon");

            // 몽둥이 플레이어 UI 요소들 찾기
            _mongdungPlayer = _gameResultContainer.Q<VisualElement>("MongdungPlayer");
            _mongdungCharacter = _gameResultContainer.Q<VisualElement>("MongdungCharacter");
            _mongdungNickname = _gameResultContainer.Q<Label>("MongdungNickname");
            _mongdungStatusText = _gameResultContainer.Q<Label>("MongdungStatusText");

            // 몽깅이 플레이어들 UI 요소들 찾기
            for (int i = 1; i <= 4; i++)
            {
                var monggingPlayer = _gameResultContainer.Q<VisualElement>($"MonggingPlayer{i}");
                var monggingNickname = _gameResultContainer.Q<Label>($"MonggingNickname{i}");
                var monggingStatusText = _gameResultContainer.Q<Label>($"MonggingStatusText{i}");
                var monggingStatusBubble = _gameResultContainer.Q<VisualElement>(
                    $"MonggingStatus{i}"
                );
                var monggingCharacter = _gameResultContainer.Q<VisualElement>(
                    $"MonggingCharacter{i}"
                );

                _monggingPlayers.Add(monggingPlayer);
                _monggingNicknames.Add(monggingNickname);
                _monggingStatusTexts.Add(monggingStatusText);
                _monggingStatusBubbles.Add(monggingStatusBubble);
                _monggingCharacters.Add(monggingCharacter);
            }

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] UI 요소 찾기 완료");
            }
        }

        private void BindViewModel()
        {
            if (_viewModel == null)
                return;

            // ViewModel 상태 구독 (R3 사용법 - PlayerListViewModel 참고)
            _viewModel.IsVisible.Subscribe(OnVisibilityChanged).AddTo(_disposables);
            _viewModel.ResultTitle.Subscribe(OnResultTitleChanged).AddTo(_disposables);
            _viewModel.EscapedCount.Subscribe(OnEscapedCountChanged).AddTo(_disposables);
            _viewModel.CoinReward.Subscribe(OnCoinRewardChanged).AddTo(_disposables);
            _viewModel.GameResult.Subscribe(OnGameResultChanged).AddTo(_disposables);

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] ViewModel 바인딩 완료");
            }
        }

        private void SetupEventHandlers()
        {
            // Continue 버튼 클릭 이벤트
            _continueButton?.RegisterCallback<ClickEvent>(evt =>
            {
                OnContinueButtonClicked();
            });

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] 이벤트 핸들러 설정 완료");
            }
        }

        private void OnContinueButtonClicked()
        {
            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] Continue 버튼 클릭됨");
            }

            // ViewModel에 이벤트 전달 (Service에서 로비 이동 처리)
            _viewModel?.OnContinueClicked();
        }

        #region ViewModel Event Handlers

        private void OnVisibilityChanged(bool isVisible)
        {
            if (isVisible)
            {
                ShowGameResult();
            }
            else
            {
                HideGameResult();
            }
        }

        private void OnResultTitleChanged(string title)
        {
            if (_resultTitle != null)
            {
                _resultTitle.text = title;
            }
        }

        private void OnEscapedCountChanged(int count)
        {
            if (_escapeCount != null)
            {
                _escapeCount.text = $"{count}명";
            }
        }

        private void OnCoinRewardChanged(int coin)
        {
            if (_coinAmount != null)
            {
                _coinAmount.text = $"+ {coin}";
            }
        }

        private void OnGameResultChanged(GameResultModel result)
        {
            if (result == null)
                return;

            UpdatePlayerUI(result);

            if (_enableDebugLogs)
            {
                Debug.Log($"[GameResultUIView] 게임 결과 UI 업데이트: {result.TeamResult}");
            }
        }

        #endregion

        #region UI Update Methods

        private void UpdatePlayerUI(GameResultModel result)
        {
            UpdateMongdungPlayer(result);
            UpdateMonggingPlayers(result);
        }

        private void UpdateMongdungPlayer(GameResultModel result)
        {
            if (result.MongdungPlayer == null)
                return;

            var player = result.MongdungPlayer;

            // 몽둥이 플레이어 표시
            if (_mongdungPlayer != null)
            {
                _mongdungPlayer.style.display = DisplayStyle.Flex;
            }

            // 닉네임 업데이트
            if (_mongdungNickname != null)
            {
                _mongdungNickname.text = player.PlayerName;
            }

            // 팀 승패 텍스트 업데이트
            if (_mongdungStatusText != null)
            {
                string statusText = result.TeamResult == TeamResult.MongdungWin ? "승리" : "패배";
                _mongdungStatusText.text = statusText;

                // 승패에 따른 스타일 클래스 적용
                _mongdungStatusText.ClearClassList();
                _mongdungStatusText.AddToClassList("status-text");
                if (result.TeamResult == TeamResult.MongdungWin)
                {
                    _mongdungStatusText.AddToClassList("victory-status");
                }
                else
                {
                    _mongdungStatusText.AddToClassList("defeat-status");
                }
            }
        }

        private void UpdateMonggingPlayers(GameResultModel result)
        {
            if (result.MonggingPlayers == null)
                return;

            // 모든 슬롯 먼저 숨김
            for (int i = 0; i < 4; i++)
            {
                if (i < _monggingPlayers.Count && _monggingPlayers[i] != null)
                {
                    _monggingPlayers[i].style.display = DisplayStyle.None;
                }
            }

            // 실제 플레이어 데이터가 있는 슬롯만 표시
            for (int i = 0; i < result.MonggingPlayers.Length && i < 4; i++)
            {
                var player = result.MonggingPlayers[i];
                if (player == null)
                    continue;

                // 슬롯 표시
                if (i < _monggingPlayers.Count && _monggingPlayers[i] != null)
                {
                    _monggingPlayers[i].style.display = DisplayStyle.Flex;
                }

                // 닉네임 업데이트
                if (i < _monggingNicknames.Count && _monggingNicknames[i] != null)
                {
                    _monggingNicknames[i].text = player.PlayerName;
                }

                // 팀 승패 텍스트 업데이트
                if (i < _monggingStatusTexts.Count && _monggingStatusTexts[i] != null)
                {
                    string statusText =
                        result.TeamResult == TeamResult.MonggingWin ? "승리" : "패배";
                    _monggingStatusTexts[i].text = statusText;

                    // 승패에 따른 스타일 클래스 적용
                    _monggingStatusTexts[i].ClearClassList();
                    _monggingStatusTexts[i].AddToClassList("status-text");
                    if (result.TeamResult == TeamResult.MonggingWin)
                    {
                        _monggingStatusTexts[i].AddToClassList("victory-status");
                    }
                    else
                    {
                        _monggingStatusTexts[i].AddToClassList("defeat-status");
                    }
                }

                // 개인 생존 상태 이미지 업데이트
                UpdateMonggingStatus(i, player);

                // 몽깅이 색상 업데이트
                UpdateMonggingColor(i, player.MonggingColor);
            }
        }

        private void UpdateMonggingStatus(int index, PlayerResultModel player)
        {
            if (index >= _monggingStatusBubbles.Count || _monggingStatusBubbles[index] == null)
                return;

            var statusBubble = _monggingStatusBubbles[index];

            // 기존 클래스 제거
            statusBubble.ClearClassList();
            statusBubble.AddToClassList("status-bubble");

            if (player.Status == PlayerStatus.Dead)
            {
                statusBubble.AddToClassList("dead");
            }
            else // PlayerStatus.Escaped
            {
                statusBubble.AddToClassList("escaped");
            }

            statusBubble.style.display = DisplayStyle.Flex;
        }

        private void UpdateMonggingColor(int index, MonggingColor color)
        {
            if (index >= _monggingCharacters.Count || _monggingCharacters[index] == null)
                return;

            var character = _monggingCharacters[index];

            // 기존 색상 클래스 제거
            character.RemoveFromClassList("mint-mongging");
            character.RemoveFromClassList("yellow-mongging");
            character.RemoveFromClassList("purple-mongging");

            // 새 색상 클래스 추가
            string colorClass = GetMonggingColorClass(color);
            character.AddToClassList("character-icon");
            character.AddToClassList("mongging-character");
            character.AddToClassList(colorClass);
        }

        private string GetStatusText(PlayerStatus status, TeamResult teamResult)
        {
            // 팀 결과에 따라 승리/패배 결정
            return teamResult == TeamResult.MonggingWin ? "승리" : "패배";
        }

        private string GetMonggingColorClass(MonggingColor color)
        {
            return color switch
            {
                MonggingColor.Mint => "mint-mongging",
                MonggingColor.Yellow => "yellow-mongging",
                MonggingColor.Purple => "purple-mongging",
                _ => "mint-mongging",
            };
        }

        private void ShowGameResult()
        {
            if (_gameResultContainer != null)
            {
                _gameResultContainer.style.display = DisplayStyle.Flex;

                if (_enableDebugLogs)
                {
                    Debug.Log("[GameResultUIView] 게임 결과 UI 표시");
                }
            }
        }

        private void HideGameResult()
        {
            if (_gameResultContainer != null)
            {
                _gameResultContainer.style.display = DisplayStyle.None;
                _viewModel?.HideResult();

                if (_enableDebugLogs)
                {
                    Debug.Log("[GameResultUIView] 게임 결과 UI 숨김");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// 게임 결과 표시 (외부에서 호출)
        /// </summary>
        public void ShowGameResult(GameResultModel resultData)
        {
            _viewModel?.SetGameResult(resultData);
            _viewModel?.ShowResult();
        }

        /// <summary>
        /// 서버 패킷으로 게임 결과 표시
        /// </summary>
        public void ShowGameResultFromPacket(byte result, int playerCount, long[][] playerData)
        {
            _viewModel?.ProcessServerPacket(result, playerCount, playerData);
            _viewModel?.ShowResult();
        }

        /// <summary>
        /// 현재 표시 상태 확인
        /// </summary>
        public bool IsVisible()
        {
            return _viewModel?.IsVisible.CurrentValue ?? false;
        }

        #endregion

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[GameResultUIView] 해제됨");
            }
        }
    }
}
