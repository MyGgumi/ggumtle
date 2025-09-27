using System;
using System.Collections;
using System.Collections.Generic;
using Features.PlayerList.Models;
using Features.PlayerList.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using R3DisposableBag = R3.CompositeDisposable;

namespace Features.PlayerList.Views
{
    /// <summary>
    /// 플레이어 리스트 UI를 담당하는 View (UI Toolkit 기반)
    /// GgumtleUIView 패턴을 따라 VisualElement + R3로 구현
    /// </summary>
    public class PlayerListUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private PlayerListViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _topRightIcons;

        [Header("Player Icons")]
        private VisualElement[] _playerIcons = new VisualElement[4];
        private Label[] _playerNames = new Label[4];
        private VisualElement[] _playerAreas = new VisualElement[4];

        [Header("Sprites")]
        [SerializeField]
        private Sprite iconMongingDefault;
        [SerializeField]
        private Sprite iconMongingFaint;
        [SerializeField]
        private Sprite iconMongingDead;
        [SerializeField]
        private Sprite iconMongingEscape;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        // 하이라이트 효과용
        private Dictionary<int, Coroutine> _highlightCoroutines = new Dictionary<int, Coroutine>();

        private CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(PlayerListViewModel playerListViewModel)
        {
            viewModel = playerListViewModel;
            if (enableDebugLogs)
                Debug.Log($"[PlayerListUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (viewModel == null)
            {
                Debug.LogError("[PlayerListUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            InitializeUI();
            InitializeSprites();

            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[PlayerListUIView] Root VisualElement가 null입니다.");
                return;
            }

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

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerListUIView] UI 요소 캐싱 완료: "
                        + $"플레이어리스트={(_topRightIcons != null ? "OK" : "NULL")}, "
                        + $"플레이어아이콘들={(_playerIcons[0] != null && _playerIcons[3] != null ? "OK" : "NULL")}"
                );
            }
        }

        private void InitializeUI()
        {
            // 초기 UI 상태 설정
            SetPlayerListVisibility(true);

            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] UI 초기화 완료");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
                return;

            // R3 Observable 구독
            viewModel.Player1
                .Subscribe(playerData => UpdatePlayerUI(1, playerData))
                .AddTo(_disposables);

            viewModel.Player2
                .Subscribe(playerData =>
                {
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerListUIView] Player2 Observable 수신: {playerData?.nickname} ({playerData?.status})");
                    UpdatePlayerUI(2, playerData);
                })
                .AddTo(_disposables);

            viewModel.Player3
                .Subscribe(playerData => UpdatePlayerUI(3, playerData))
                .AddTo(_disposables);

            viewModel.Player4
                .Subscribe(playerData => UpdatePlayerUI(4, playerData))
                .AddTo(_disposables);

            // 전체 플레이어 데이터 변경 구독
            viewModel.AllPlayers
                .Subscribe(allPlayers => UpdateAllPlayersUI())
                .AddTo(_disposables);

            // 스프라이트는 Inspector에서 직접 관리하므로 구독 불필요

            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] ViewModel 구독 완료");
        }

        #region UI Update Methods

        private void UpdatePlayerUI(int playerId, PlayerListData playerData)
        {
            if (enableDebugLogs)
                Debug.Log($"[PlayerListUIView] UpdatePlayerUI 호출: Player{playerId}, Data={playerData?.nickname} ({playerData?.status})");

            if (playerId < 1 || playerId > 4 || playerData == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[PlayerListUIView] UpdatePlayerUI 중단: Player{playerId}, PlayerData={playerData != null}");
                return;
            }

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

                // Inspector에서 설정한 스프라이트를 상태에 따라 사용
                Sprite spriteToUse = GetPlayerStateSprite(playerData.status);

                if (enableDebugLogs)
                {
                    Debug.Log($"[PlayerListUIView] Player{playerId} 아이콘 설정: " +
                        $"Status={playerData.status}, " +
                        $"Sprite={spriteToUse != null}");
                }

                if (spriteToUse != null)
                {
                    // 레거시 코드처럼 완전히 초기화하고 새로 설정
                    _playerIcons[index].style.backgroundImage = new StyleBackground(spriteToUse);
                    _playerIcons[index].style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
                    _playerIcons[index].style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                    _playerIcons[index].style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    _playerIcons[index].style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                    _playerIcons[index].style.display = DisplayStyle.Flex;
                    _playerIcons[index].style.width = 100;
                    _playerIcons[index].style.height = 100;
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.LogWarning($"[PlayerListUIView] Player{playerId} Inspector에서 {playerData.status} 스프라이트가 설정되지 않았습니다!");
                }

                // 색상 적용
                SetPlayerColor(playerId, playerData.colorTheme);

                // 온라인/오프라인 상태 클래스 적용
                ApplyPlayerStateClasses(_playerIcons[index], playerData);
            }

            // 플레이어 영역 표시
            if (_playerAreas[index] != null)
            {
                _playerAreas[index].style.display = DisplayStyle.Flex;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerListUIView] 플레이어 {playerId} UI 업데이트: {playerData.nickname} ({playerData.status})");
            }
        }

        private void SetPlayerColor(int playerId, string colorType)
        {
            if (playerId < 1 || playerId > 4)
                return;

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

        public void ApplyPlayerHighlight(int playerId, bool highlight, float duration)
        {
            if (playerId < 1 || playerId > 4)
                return;

            var icon = _playerIcons[playerId - 1];
            if (icon == null)
                return;

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
            if (viewModel == null)
                return;

            // 전체 플레이어 리스트 컨테이너 표시
            if (_topRightIcons != null)
            {
                _topRightIcons.style.display = DisplayStyle.Flex;
            }

            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] 모든 플레이어 UI 업데이트 완료");
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

                if (_playerIcons[i] != null && iconMongingDefault != null)
                {
                    _playerIcons[i].style.backgroundImage = new StyleBackground(iconMongingDefault);
                    _playerIcons[i].ClearClassList();
                    _playerIcons[i].AddToClassList("player-online");
                }
            }

            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] 모든 플레이어 UI 초기화");
        }


        #endregion

        #region Public API (레거시 호환성)

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

        // 레거시 호환성 메서드들
        public void UpdatePlayer(int playerId, string nickname, string colorTheme, string status = "default", bool isOnline = true, bool isHost = false)
        {
            if (viewModel != null)
            {
                viewModel.UpdatePlayer(playerId, nickname, colorTheme, status, isOnline, isHost);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            _disposables?.Dispose();

            // 모든 하이라이트 코루틴 정리
            foreach (var coroutine in _highlightCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            _highlightCoroutines.Clear();

            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] OnDestroy - Dispose 완료");
        }

        #endregion

        #region Sprite Initialization

        private void InitializeSprites()
        {
            // 스프라이트는 Player 상태에 따라 동적으로 적용됨
            if (enableDebugLogs)
                Debug.Log("[PlayerListUIView] 플레이어 상태 스프라이트 준비 완료");
        }


        private Sprite GetPlayerStateSprite(string status)
        {
            return status switch
            {
                "faint" => iconMongingFaint,
                "dead" => iconMongingDead,
                "escape" => iconMongingEscape,
                _ => iconMongingDefault
            };
        }

        #endregion
    }
}