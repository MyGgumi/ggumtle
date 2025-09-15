using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MVVM.UI;

namespace Views
{
    public class PlayerListView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField] private PlayerListViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _topRightIcons;

        [Header("Player Icons")]
        private VisualElement[] _playerIcons = new VisualElement[4];
        private Label[] _playerNames = new Label[4];
        private VisualElement[] _playerAreas = new VisualElement[4];

        // 하이라이트 효과용
        private Dictionary<int, Coroutine> _highlightCoroutines = new Dictionary<int, Coroutine>();

        public void Initialize(VisualElement root, PlayerListViewModel viewModel)
        {
            _root = root;
            this.viewModel = viewModel;

            CacheUIElements();
            SubscribeToViewModel();
            UpdateAllPlayersUI();

            Debug.Log("[PlayerListView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _topRightIcons = _root.Q<VisualElement>("topRightIcons");

            // 플레이어 아이콘과 이름 캐싱
            for (int i = 0; i < 4; i++)
            {
                int playerNum = i + 1;
                _playerIcons[i] = _root.Q<VisualElement>($"icon{playerNum}");
                _playerNames[i] = _root.Q<Label>($"player{playerNum}Name");
                _playerAreas[i] = _root.Q<VisualElement>($"player{playerNum}Area");

                // UXML의 하드코딩된 플레이어 이름 초기화
                if (_playerNames[i] != null)
                {
                    _playerNames[i].text = "";
                }
            }

            Debug.Log($"[PlayerListView] UI 요소 캐싱 완료: " +
                     $"플레이어리스트={(_topRightIcons != null ? "OK" : "NULL")}, " +
                     $"플레이어아이콘들={(_playerIcons[0] != null && _playerIcons[3] != null ? "OK" : "NULL")}");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null) return;

            viewModel.PlayerUpdated += OnPlayerUpdated;
            viewModel.PlayerStatusChanged += OnPlayerStatusChanged;
            viewModel.PlayerConnectionChanged += OnPlayerConnectionChanged;
            viewModel.PlayerHighlighted += OnPlayerHighlighted;
            viewModel.AllPlayersCleared += OnAllPlayersCleared;
            viewModel.SpritesUpdated += OnSpritesUpdated;

            Debug.Log("[PlayerListView] ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (viewModel == null) return;

            viewModel.PlayerUpdated -= OnPlayerUpdated;
            viewModel.PlayerStatusChanged -= OnPlayerStatusChanged;
            viewModel.PlayerConnectionChanged -= OnPlayerConnectionChanged;
            viewModel.PlayerHighlighted -= OnPlayerHighlighted;
            viewModel.AllPlayersCleared -= OnAllPlayersCleared;
            viewModel.SpritesUpdated -= OnSpritesUpdated;

            Debug.Log("[PlayerListView] ViewModel 이벤트 구독 해제 완료");
        }

        #region ViewModel Event Handlers

        private void OnPlayerUpdated(int playerId, PlayerListData playerData)
        {
            UpdatePlayerUI(playerId, playerData);
        }

        private void OnPlayerStatusChanged(int playerId, string status)
        {
            if (viewModel != null)
            {
                var playerData = viewModel.GetPlayer(playerId);
                if (playerData != null)
                {
                    UpdatePlayerUI(playerId, playerData);
                }
            }
        }

        private void OnPlayerConnectionChanged(int playerId, bool isOnline)
        {
            if (viewModel != null)
            {
                var playerData = viewModel.GetPlayer(playerId);
                if (playerData != null)
                {
                    UpdatePlayerUI(playerId, playerData);
                }
            }
        }

        private void OnPlayerHighlighted(int playerId, bool highlight, float duration)
        {
            ApplyPlayerHighlight(playerId, highlight, duration);
        }

        private void OnAllPlayersCleared()
        {
            ClearAllPlayersUI();
        }

        private void OnSpritesUpdated(Sprite defaultIcon, Sprite faintIcon, Sprite deadIcon, Sprite escapeIcon)
        {
            UpdateAllPlayersUI();
        }

        #endregion

        #region UI Update Methods

        private void UpdatePlayerUI(int playerId, PlayerListData playerData)
        {
            if (playerId < 1 || playerId > 4) return;

            int index = playerId - 1;

            // 닉네임 업데이트
            if (_playerNames[index] != null)
            {
                _playerNames[index].text = playerData.nickname;
                _playerNames[index].style.display = DisplayStyle.Flex;
            }

            // 아이콘 업데이트
            if (_playerIcons[index] != null)
            {
                // 모든 기존 스타일 초기화
                _playerIcons[index].style.backgroundImage = StyleKeyword.None;
                _playerIcons[index].style.unityBackgroundImageTintColor = StyleKeyword.None;
                _playerIcons[index].style.backgroundColor = StyleKeyword.None;
                _playerIcons[index].ClearClassList();

                // 새 스프라이트 설정
                if (playerData.avatarSprite != null)
                {
                    _playerIcons[index].style.backgroundImage = new StyleBackground(playerData.avatarSprite);
                    _playerIcons[index].style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    _playerIcons[index].style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                    _playerIcons[index].style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    _playerIcons[index].style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                }

                // 색상 적용
                SetPlayerColor(playerId, playerData.colorTheme);

                // 온라인/오프라인 상태 클래스 적용
                ApplyPlayerStateClasses(_playerIcons[index], playerData);

                _playerIcons[index].style.display = DisplayStyle.Flex;
            }

            // 플레이어 영역 표시
            if (_playerAreas[index] != null)
            {
                _playerAreas[index].style.display = DisplayStyle.Flex;
            }

            Debug.Log($"[PlayerListView] 플레이어 {playerId} UI 업데이트: {playerData.nickname} ({playerData.status})");
        }

        private void SetPlayerColor(int playerId, string colorType)
        {
            if (playerId < 1 || playerId > 4) return;

            VisualElement playerIcon = _playerIcons[playerId - 1];
            if (playerIcon != null && viewModel != null)
            {
                Color color = viewModel.GetPlayerColor(colorType);
                playerIcon.style.unityBackgroundImageTintColor = color;
            }
        }

        private void ApplyPlayerStateClasses(VisualElement icon, PlayerListData player)
        {
            // 기존 상태 클래스 제거
            icon.RemoveFromClassList("player-online");
            icon.RemoveFromClassList("player-offline");

            // 새 상태 클래스 적용
            if (player.isOnline)
                icon.AddToClassList("player-online");
            else
                icon.AddToClassList("player-offline");
        }

        private void ApplyPlayerHighlight(int playerId, bool highlight, float duration)
        {
            if (playerId < 1 || playerId > 4) return;

            var icon = _playerIcons[playerId - 1];
            if (icon == null) return;

            // 기존 하이라이트 코루틴 중단
            if (_highlightCoroutines.ContainsKey(playerId))
            {
                if (_highlightCoroutines[playerId] != null)
                {
                    StopCoroutine(_highlightCoroutines[playerId]);
                }
                _highlightCoroutines.Remove(playerId);
            }

            if (highlight)
            {
                icon.AddToClassList("player-highlighted");
                // 지정된 시간 후 하이라이트 자동 해제
                _highlightCoroutines[playerId] = StartCoroutine(RemoveHighlightAfterDelay(icon, playerId, duration));
            }
            else
            {
                icon.RemoveFromClassList("player-highlighted");
            }
        }

        private IEnumerator RemoveHighlightAfterDelay(VisualElement icon, int playerId, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (icon != null)
            {
                icon.RemoveFromClassList("player-highlighted");
            }

            if (_highlightCoroutines.ContainsKey(playerId))
            {
                _highlightCoroutines.Remove(playerId);
            }
        }

        private void UpdateAllPlayersUI()
        {
            if (viewModel == null) return;

            foreach (var kvp in viewModel.PlayerListDataCache)
            {
                UpdatePlayerUI(kvp.Key, kvp.Value);
            }

            // 전체 플레이어 리스트 컨테이너 표시
            if (_topRightIcons != null)
            {
                _topRightIcons.style.display = DisplayStyle.Flex;
            }

            Debug.Log("[PlayerListView] 모든 플레이어 UI 업데이트 완료");
        }

        private void ClearAllPlayersUI()
        {
            // 모든 하이라이트 코루틴 중단
            foreach (var coroutine in _highlightCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _highlightCoroutines.Clear();

            // UI 초기화
            for (int i = 0; i < 4; i++)
            {
                if (_playerNames[i] != null)
                    _playerNames[i].text = $"플레이어{i + 1}";

                if (_playerIcons[i] != null && viewModel?.IconMongingDefault != null)
                {
                    _playerIcons[i].style.backgroundImage = new StyleBackground(viewModel.IconMongingDefault);
                    _playerIcons[i].ClearClassList();
                    _playerIcons[i].AddToClassList("player-online");
                }
            }

            Debug.Log("[PlayerListView] 모든 플레이어 UI 초기화");
        }

        #endregion

        #region Public API

        public void SetPlayerListVisibility(bool visible)
        {
            if (_topRightIcons != null)
            {
                _topRightIcons.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void RefreshAllPlayers()
        {
            UpdateAllPlayersUI();
        }

        public void SetPlayerHighlight(int playerId, bool highlight, float duration = 1f)
        {
            if (viewModel != null)
            {
                viewModel.HighlightPlayer(playerId, highlight, duration);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();

            // 모든 하이라이트 코루틴 정리
            foreach (var coroutine in _highlightCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _highlightCoroutines.Clear();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log($"[PlayerListView] UI State:\n" +
                     $"  TopRightIcons Visible: {(_topRightIcons?.style.display.value == DisplayStyle.Flex)}\n" +
                     $"  Player1 Name: {(_playerNames[0]?.text ?? "NULL")}\n" +
                     $"  Player2 Name: {(_playerNames[1]?.text ?? "NULL")}\n" +
                     $"  Player3 Name: {(_playerNames[2]?.text ?? "NULL")}\n" +
                     $"  Player4 Name: {(_playerNames[3]?.text ?? "NULL")}\n" +
                     $"  Active Highlights: {_highlightCoroutines.Count}\n" +
                     $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}");
        }

        [ContextMenu("Test Highlight Player 1")]
        private void TestHighlightPlayer1() => SetPlayerHighlight(1, true, 2f);

        #endregion
    }
}