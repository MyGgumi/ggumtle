using Features.Game.Managers;
using Features.Scenes.Lobby.Messages;
using Features.Scenes.Lobby.NetworkSources;
using Features.Scenes.Lobby.ViewModels;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Features.Scenes.Lobby.Managers
{
    /// <summary>
    /// 로비 씬을 관리하는 매니저
    /// UI 이벤트를 처리하고 ViewModel과 연동하여 게임 플로우를 제어
    /// </summary>
    public class LobbySceneManager : MonoBehaviour
    {
        [Header("더미 테스트 데이터")]
        [SerializeField]
        private string testAccessToken =
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwiaWF0IjoxNzU4MDczOTQ4LCJleHAiOjE3NTkyODM1NDh9.cfillJjXl1d92Cw8-dPPSYbZQ90lKhcj2_B8TM-QK9c";

        [SerializeField]
        private string testHost = "p-ryan.iptime.org";

        [SerializeField]
        private int testPort = 8888;

        [SerializeField]
        private int testRoomId = -4; // -1은 자동 매칭

        [Header("UI References")]
        [SerializeField]
        private Button joinRoomButton;

        [SerializeField]
        private Text statusText;

        // 의존성 주입
        private LobbyViewModel _viewModel;
        private GameManager _gameManager;
        private ILobbyNetworkSource _lobbyNetworkSource;
        private readonly CompositeDisposable _disposables = new();
        private readonly System.Collections.Generic.List<System.IDisposable> _messageDisposables =
            new();


        [Inject]
        public void Construct(
            LobbyViewModel viewModel,
            GameManager gameManager,
            ILobbyNetworkSource lobbyNetworkSource
        )
        {
            Debug.Log("[LobbySceneManager] Construct 호출됨!");
            _viewModel = viewModel;
            _gameManager = gameManager;
            _lobbyNetworkSource = lobbyNetworkSource;
            Debug.Log(
                $"[LobbySceneManager] ViewModel: {_viewModel != null}, GameManager: {_gameManager != null}, NetworkSource: {_lobbyNetworkSource != null}"
            );
        }

        void Awake()
        {
            Debug.Log("[LobbySceneManager] Awake 호출");
        }

        void Start()
        {
            Debug.Log("[LobbySceneManager] 로비 씬 시작");


            // UI 초기화
            InitializeUI();

            // ViewModel 이벤트 구독
            SubscribeToViewModel();

            // 네트워크 이벤트 구독
            SubscribeToNetworkEvents();

            // 더미 데이터로 자동 초기화
            InitializeNetworkWithDummyData();
        }

        void OnDestroy()
        {

            // R3 구독 해제
            _disposables.Dispose();

            // MessagePipe 구독 해제
            foreach (var disposable in _messageDisposables)
            {
                disposable.Dispose();
            }
            _messageDisposables.Clear();
        }

        /// <summary>
        /// UI 초기화
        /// </summary>
        private void InitializeUI()
        {
            // 버튼 이벤트 연결
            if (joinRoomButton != null)
                joinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);

            // 초기 상태 설정
            if (joinRoomButton != null)
                joinRoomButton.interactable = false;

            Debug.Log("[LobbySceneManager] UI 초기화 완료");
        }

        /// <summary>
        /// ViewModel 이벤트 구독
        /// </summary>
        private void SubscribeToViewModel()
        {
            if (_viewModel == null)
            {
                Debug.LogError("[LobbySceneManager] ViewModel이 주입되지 않음");
                return;
            }

            // 상태 메시지 바인딩
            _viewModel
                .StatusMessage.Subscribe(message =>
                {
                    if (statusText != null)
                        statusText.text = message;
                })
                .AddTo(_disposables);

            // 인증 상태 바인딩
            _viewModel
                .IsAuthenticated.Subscribe(isAuthenticated =>
                {
                    if (joinRoomButton != null)
                        joinRoomButton.interactable = isAuthenticated;
                })
                .AddTo(_disposables);

            // 연결 상태 바인딩
            _viewModel
                .IsConnecting.Subscribe(isConnecting =>
                {
                    if (joinRoomButton != null && _viewModel.IsAuthenticated.CurrentValue)
                        joinRoomButton.interactable = !isConnecting;
                })
                .AddTo(_disposables);


            Debug.Log("[LobbySceneManager] ViewModel 이벤트 구독 완료");
        }


        /// <summary>
        /// 더미 데이터로 초기화
        /// </summary>
        private async void InitializeNetworkWithDummyData()
        {
            try
            {
                // UI 상태 업데이트: 연결 시작
                PublishUIState(LobbyUIStateMessage.Connecting("서버 연결 중..."));

                // 네트워크 초기화
                await _lobbyNetworkSource.InitializeNetworkAsync(testHost, testPort);
                Debug.Log("[LobbySceneManager] 네트워크 초기화 성공");

                // UI 상태 업데이트: 연결 완료
                PublishUIState(LobbyUIStateMessage.Ready("서버 연결 완료"));

                // 토큰 인증 시도
                await AttemptTokenAuthentication();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySceneManager] 네트워크 초기화 실패: {e.Message}");
                PublishUIState(
                    LobbyUIStateMessage.Error($"서버 연결 실패 (더미 모드로 진행): {e.Message}")
                );
                // 더미 모드로 인증 성공 처리
                PublishUIState(LobbyUIStateMessage.Authenticated("더미 모드로 인증 완료"));
            }
        }

        /// <summary>
        /// 방 입장 버튼 클릭 이벤트
        /// </summary>
        private void OnJoinRoomButtonClicked()
        {
            Debug.Log("[LobbySceneManager] 🎮 방 입장 버튼 클릭!");
            Debug.Log($"[LobbySceneManager] testRoomId: {testRoomId}");

            // 방 입장 시도 (LobbySceneManager에서 직접 처리)
            AttemptJoinRoom(testRoomId);
        }

        /// <summary>
        /// 네트워크 이벤트 구독
        /// </summary>
        private void SubscribeToNetworkEvents()
        {
            var tokenVerifiedSubscriber = GlobalMessagePipe.GetSubscriber<TokenVerifiedMessage>();
            var subscription = tokenVerifiedSubscriber.Subscribe(OnTokenVerified);
            _messageDisposables.Add(subscription);

            Debug.Log("[LobbySceneManager] 네트워크 이벤트 구독 완료");
        }

        /// <summary>
        /// 토큰 검증 완료 이벤트 처리 (비즈니스 로직)
        /// </summary>
        private void OnTokenVerified(TokenVerifiedMessage message)
        {
            Debug.Log(
                $"[LobbySceneManager] 토큰 검증 결과 수신: Success={message.IsSuccess}, SessionId={message.SessionId}"
            );

            if (message.IsSuccess)
            {
                PublishUIState(
                    LobbyUIStateMessage.Authenticated(
                        $"인증 완료 - 게임 시작 준비됨 (세션 ID: {message.SessionId})"
                    )
                );
            }
            else
            {
                Debug.LogWarning($"[LobbySceneManager] 토큰 인증 실패: {message.ErrorMessage}");
                // 더미 모드로 진행
                PublishUIState(
                    LobbyUIStateMessage.Authenticated("토큰 인증 실패 - 더미 모드로 진행")
                );
            }
        }

        /// <summary>
        /// 토큰 인증 시도 (비즈니스 로직)
        /// </summary>
        private async System.Threading.Tasks.Task AttemptTokenAuthentication()
        {
            try
            {
                if (!string.IsNullOrEmpty(testAccessToken))
                {
                    PublishUIState(LobbyUIStateMessage.Connecting("토큰 인증 중..."));
                    Debug.Log("[LobbySceneManager] 토큰 인증 시작");

                    var result = await _lobbyNetworkSource.VerifyTokenAsync(testAccessToken);
                    Debug.Log($"[LobbySceneManager] 토큰 인증 완료: Success={result.Success}");
                    // 결과는 TokenVerifiedMessage로 처리됨
                }
                else
                {
                    Debug.Log("[LobbySceneManager] 토큰이 없어 더미 모드로 인증 처리");
                    PublishUIState(
                        LobbyUIStateMessage.Authenticated("토큰 없음 - 더미 모드로 진행")
                    );
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySceneManager] 토큰 인증 실패: {e.Message}");
                PublishUIState(
                    LobbyUIStateMessage.Authenticated("토큰 인증 실패 - 더미 모드로 진행")
                );
            }
        }

        /// <summary>
        /// UI 상태 메시지 발행
        /// </summary>
        private void PublishUIState(LobbyUIStateMessage message)
        {
            var publisher = GlobalMessagePipe.GetPublisher<LobbyUIStateMessage>();
            publisher.Publish(message);
            Debug.Log($"[LobbySceneManager] UI 상태 메시지 발행: {message.StatusMessage}");
        }

        /// <summary>
        /// 방 입장 시도 (비즈니스 로직)
        /// </summary>
        public async void AttemptJoinRoom(int roomId = -1)
        {
            Debug.Log($"[LobbySceneManager] 방 입장 시도: roomId={roomId}");

            try
            {
                PublishUIState(LobbyUIStateMessage.Connecting("방 입장 중..."));

                var result = await _lobbyNetworkSource.JoinRoomAsync(roomId);

                if (result.Success)
                {
                    PublishUIState(LobbyUIStateMessage.Ready("방 입장 성공! 로딩 중..."));
                    Debug.Log($"[LobbySceneManager] 방 입장 성공: RoomId={roomId}");
                    OnRoomJoinedSuccessfully();
                }
                else
                {
                    PublishUIState(LobbyUIStateMessage.Error($"방 입장 실패: {result.Result}"));
                    Debug.LogError($"[LobbySceneManager] 방 입장 실패: {result.Result}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySceneManager] 방 입장 오류: {e.Message}");
                PublishUIState(LobbyUIStateMessage.Ready("더미 모드로 방 입장 성공! 로딩 중..."));
                OnRoomJoinedSuccessfully();
            }
        }

        /// <summary>
        /// 방 입장 성공 시 호출되는 콜백
        /// </summary>
        private async void OnRoomJoinedSuccessfully()
        {
            Debug.Log("[LobbySceneManager] 방 입장 성공 - Loading 씬으로 전환");

            try
            {
                await _gameManager.TransitionToLoading();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySceneManager] Loading 씬 전환 실패: {e.Message}");
            }
        }

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugTriggerJoinRoom()
        {
            OnJoinRoomButtonClicked();
        }

        #endregion
    }
}
