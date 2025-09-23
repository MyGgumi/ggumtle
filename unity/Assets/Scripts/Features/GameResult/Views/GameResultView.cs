using System;
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
    /// 게임 결과 UI View - MVVM pattern
    /// ViewModel과 UI 요소를 바인딩하고 사용자 입력을 처리
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class GameResultView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private GameResultViewModel viewModel;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        [Header("Character Sprites")]
        public Sprite mongdungSprite;
        public Sprite monggingBaseSprite;

        [Header("Status Images")]
        public Sprite aliveStatusSprite;
        public Sprite deadStatusSprite;

        [Header("Other Sprites")]
        public Sprite coinSprite;
        public Sprite resultBackgroundSprite;

        // UI Document
        private UIDocument _doc;
        private VisualElement _root;

        // UI Elements
        private Label gameResultTitle;
        private Label escapeCount;
        private Label escapeText;
        private Label coinAmount;
        private Button continueButton;
        private VisualElement background;
        private VisualElement coinIcon;

        // Player Elements
        private VisualElement mongdungCharacter;
        private Label mongdungNickname;
        private Label mongdungStatusText;

        private Label[] monggingNicknames = new Label[4];
        private Label[] monggingStatusTexts = new Label[4];
        private VisualElement[] monggingCharacters = new VisualElement[4];
        private VisualElement[] monggingStatusImages = new VisualElement[4];

        // Reactive subscriptions
        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(GameResultViewModel gameResultViewModel)
        {
            viewModel = gameResultViewModel;
            if (enableDebugLogs)
                Debug.Log($"[GameResultView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        void Awake()
        {
            _doc = GetComponent<UIDocument>();
            if (_doc != null)
            {
                _root = _doc.rootVisualElement;
            }

            // ViewModel은 VContainer 의존성 주입으로만 설정
            if (viewModel == null)
            {
                Debug.LogWarning("[GameResultView] ViewModel이 설정되지 않았습니다. VContainer 의존성 주입을 확인하세요.");
            }
        }

        void Start()
        {
            InitializeUI();
            BindViewModel();

            // 초기에는 숨김
            SetVisibility(false);
        }

        void OnDestroy()
        {
            _disposables?.Dispose();
        }

        #region UI Initialization

        private void InitializeUI()
        {
            if (_root == null)
            {
                Debug.LogError("[GameResultView] Root VisualElement가 null입니다!");
                return;
            }

            // UI 요소들 찾기
            FindUIElements();

            // 버튼 이벤트 등록
            SetupButtonEvents();

            // 스프라이트 설정
            SetupSprites();

            if (enableDebugLogs)
            {
                Debug.Log("[GameResultView] UI 초기화 완료");
            }
        }

        private void FindUIElements()
        {
            // 제목 요소들
            gameResultTitle = _root.Q<Label>("GameResultTitle");
            escapeCount = _root.Q<Label>("EscapeCount");
            escapeText = _root.Q<Label>("EscapeText");

            // 배경 및 아이콘 요소들
            background = _root.Q<VisualElement>("Background");
            coinIcon = _root.Q<VisualElement>("CoinIcon");
            coinAmount = _root.Q<Label>("CoinAmount");

            // 버튼 요소들
            continueButton = _root.Q<Button>("ContinueButton");

            // 몽둥이 요소들
            mongdungCharacter = _root.Q<VisualElement>("MongdungCharacter");
            mongdungNickname = _root.Q<Label>("MongdungNickname");
            mongdungStatusText = _root.Q<Label>("MongdungStatusText");

            // 몽깅이 요소들 (4명)
            for (int i = 0; i < 4; i++)
            {
                int playerIndex = i + 1;
                monggingNicknames[i] = _root.Q<Label>($"MonggingNickname{playerIndex}");
                monggingStatusTexts[i] = _root.Q<Label>($"MonggingStatusText{playerIndex}");
                monggingCharacters[i] = _root.Q<VisualElement>($"MonggingCharacter{playerIndex}");
                monggingStatusImages[i] = _root.Q<VisualElement>($"MonggingStatus{playerIndex}");
            }
        }

        private void SetupButtonEvents()
        {
            if (continueButton != null)
            {
                continueButton.clicked += OnContinueButtonClicked;
            }
        }

        private void SetupSprites()
        {
            // 배경 스프라이트 설정
            if (background != null && resultBackgroundSprite != null)
            {
                background.style.backgroundImage = new StyleBackground(resultBackgroundSprite);
            }

            // 몽둥이 캐릭터 스프라이트 설정
            if (mongdungCharacter != null && mongdungSprite != null)
            {
                mongdungCharacter.style.backgroundImage = new StyleBackground(mongdungSprite);
            }

            // 몽깅이들 기본 스프라이트 설정
            for (int i = 0; i < 4; i++)
            {
                if (monggingCharacters[i] != null && monggingBaseSprite != null)
                {
                    monggingCharacters[i].style.backgroundImage = new StyleBackground(monggingBaseSprite);
                }
            }

            // 코인 아이콘 설정
            if (coinIcon != null && coinSprite != null)
            {
                coinIcon.style.backgroundImage = new StyleBackground(coinSprite);
            }

            if (enableDebugLogs)
            {
                Debug.Log("[GameResultView] 스프라이트 설정 완료");
            }
        }

        #endregion

        #region ViewModel Binding

        private void BindViewModel()
        {
            if (viewModel == null)
            {
                Debug.LogError("[GameResultView] ViewModel이 null입니다!");
                return;
            }

            // UI 표시/숨김 바인딩
            viewModel.IsVisible
                .Subscribe(isVisible => SetVisibility(isVisible))
                .AddTo(_disposables);

            // 제목 바인딩
            viewModel.ResultTitle
                .Subscribe(title => {
                    if (gameResultTitle != null)
                        gameResultTitle.text = title;
                })
                .AddTo(_disposables);

            // 탈출 인원 수 바인딩
            viewModel.EscapedCount
                .Subscribe(count => {
                    if (escapeCount != null)
                        escapeCount.text = $"{count}명";
                })
                .AddTo(_disposables);

            // 코인 보상 바인딩
            viewModel.CoinReward
                .Subscribe(reward => {
                    if (coinAmount != null)
                        coinAmount.text = $"+ {reward}";
                })
                .AddTo(_disposables);

            // 게임 결과 데이터 바인딩
            viewModel.GameResult
                .Where(result => result != null)
                .Subscribe(UpdateGameResultDisplay)
                .AddTo(_disposables);

            if (enableDebugLogs)
            {
                Debug.Log("[GameResultView] ViewModel 바인딩 완료");
            }
        }

        #endregion

        #region UI Update Methods

        private void UpdateGameResultDisplay(GameResultModel resultData)
        {
            UpdatePlayerDisplay(resultData);

            if (enableDebugLogs)
            {
                Debug.Log("[GameResultView] 게임 결과 UI 업데이트 완료");
            }
        }

        private void UpdatePlayerDisplay(GameResultModel resultData)
        {
            // 몽둥이 정보 업데이트
            UpdateMongdungPlayer(resultData);

            // 몽깅이들 정보 업데이트
            UpdateMonggingPlayers(resultData);
        }

        private void UpdateMongdungPlayer(GameResultModel resultData)
        {
            if (resultData.MongdungPlayer == null) return;

            var player = resultData.MongdungPlayer;

            // 닉네임 업데이트
            if (mongdungNickname != null)
            {
                mongdungNickname.text = player.PlayerName;
            }

            // 팀 승패 텍스트 업데이트
            if (mongdungStatusText != null)
            {
                string statusText = resultData.TeamResult == TeamResult.MongdungWin ? "승리" : "패배";
                mongdungStatusText.text = statusText;
            }
        }

        private void UpdateMonggingPlayers(GameResultModel resultData)
        {
            if (resultData.MonggingPlayers == null) return;

            // 모든 슬롯 먼저 숨김
            for (int i = 0; i < 4; i++)
            {
                var playerSlot = _root.Q<VisualElement>($"MonggingPlayer{i + 1}");
                if (playerSlot != null)
                {
                    playerSlot.AddToClassList("hidden");
                    playerSlot.RemoveFromClassList("visible");
                }
            }

            // 실제 플레이어 데이터가 있는 슬롯만 표시
            for (int i = 0; i < resultData.MonggingPlayers.Length && i < 4; i++)
            {
                var player = resultData.MonggingPlayers[i];
                if (player == null) continue;

                var playerSlot = _root.Q<VisualElement>($"MonggingPlayer{i + 1}");
                if (playerSlot != null)
                {
                    // 슬롯 표시
                    playerSlot.RemoveFromClassList("hidden");
                    playerSlot.AddToClassList("visible");
                }

                // 닉네임 업데이트
                if (monggingNicknames[i] != null)
                {
                    monggingNicknames[i].text = player.PlayerName;
                }

                // 팀 승패 텍스트 업데이트
                if (monggingStatusTexts[i] != null)
                {
                    string statusText = resultData.TeamResult == TeamResult.MonggingWin ? "승리" : "패배";
                    monggingStatusTexts[i].text = statusText;
                }

                // 개인 생존 상태 이미지 업데이트
                UpdateMonggingStatus(i, player);

                // 몽깅이 색상 업데이트
                UpdateMonggingColor(i, player.MonggingColor);
            }
        }

        private void UpdateMonggingStatus(int index, PlayerResultModel player)
        {
            if (monggingStatusImages[index] == null) return;

            // 기존 클래스 제거
            monggingStatusImages[index].RemoveFromClassList("escaped");
            monggingStatusImages[index].RemoveFromClassList("alive");
            monggingStatusImages[index].RemoveFromClassList("dead");

            if (player.Status == PlayerStatus.Dead)
            {
                // 죽음 - Inspector 이미지가 있으면 사용, 없으면 CSS 클래스
                if (deadStatusSprite != null)
                {
                    monggingStatusImages[index].style.backgroundImage = new StyleBackground(deadStatusSprite);
                }
                else
                {
                    monggingStatusImages[index].AddToClassList("dead");
                }
            }
            else // PlayerStatus.Escaped
            {
                // 탈출(생존) - Inspector 이미지가 있으면 사용, 없으면 CSS 클래스
                if (aliveStatusSprite != null)
                {
                    monggingStatusImages[index].style.backgroundImage = new StyleBackground(aliveStatusSprite);
                }
                else
                {
                    monggingStatusImages[index].AddToClassList("escaped");
                }
            }

            monggingStatusImages[index].style.display = DisplayStyle.Flex;
        }

        private void UpdateMonggingColor(int index, MonggingColor color)
        {
            if (monggingCharacters[index] == null) return;

            // 기존 색상 클래스 제거
            monggingCharacters[index].RemoveFromClassList("mint-mongging");
            monggingCharacters[index].RemoveFromClassList("yellow-mongging");
            monggingCharacters[index].RemoveFromClassList("purple-mongging");

            // 새 색상 클래스 추가
            string colorClass = color switch
            {
                MonggingColor.Mint => "mint-mongging",
                MonggingColor.Yellow => "yellow-mongging",
                MonggingColor.Purple => "purple-mongging",
                _ => "mint-mongging"
            };
            monggingCharacters[index].AddToClassList(colorClass);
        }

        private void SetVisibility(bool visible)
        {
            if (_root != null)
            {
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[GameResultView] UI 표시 상태: {visible}");
            }
        }

        #endregion

        #region Event Handlers

        private void OnContinueButtonClicked()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[GameResultView] 계속 버튼 클릭");
            }

            // ViewModel에 이벤트 전달
            viewModel?.OnContinueClicked();

            // 로비로 이동
            SceneManager.LoadScene("Lobby");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 게임 결과 표시 (외부에서 호출)
        /// </summary>
        public void ShowGameResult(GameResultModel resultData)
        {
            viewModel?.SetGameResult(resultData);
            viewModel?.ShowResult();
        }

        /// <summary>
        /// 서버 패킷으로 게임 결과 표시
        /// </summary>
        public void ShowGameResultFromPacket(byte result, int playerCount, long[][] playerData)
        {
            viewModel?.ProcessServerPacket(result, playerCount, playerData);
            viewModel?.ShowResult();
        }

        /// <summary>
        /// 게임 결과 숨기기
        /// </summary>
        public void HideGameResult()
        {
            viewModel?.HideResult();
        }

        /// <summary>
        /// 현재 표시 상태 확인
        /// </summary>
        public bool IsVisible()
        {
            return viewModel?.IsVisible.CurrentValue ?? false;
        }

        #endregion
    }
}