using System;
using Features.Game.Managers;
using Features.Scenes.Lobby.Messages;
using Features.Scenes.Lobby.NetworkSources;
using Features.Scenes.Lobby.ViewModels;
using MessagePipe;
using Networks;
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

        // 미리 설정된 토큰들 (7, 8, 9, 0키용)
        private readonly string[] presetTokens = new string[]
        {
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwiaWF0IjoxNzU4MDczOTQ4LCJleHAiOjE3NTkyODM1NDh9.cfillJjXl1d92Cw8-dPPSYbZQ90lKhcj2_B8TM-QK9c", // 0키용 (기본값)
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIyIiwiaWF0IjoxNzU4MDczODg1LCJleHAiOjE3NTkyODM0ODV9.fsICo-1bwGxPVN7KvuXH9TfOnpud0hFlZCSffXKBGc8", // 7키용
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIzIiwiaWF0IjoxNzU4MDc0MDA2LCJleHAiOjE3NTkyODM2MDZ9.YDQcsyMUwJBX2HfNlJKTMC6SUYUIeAWWY3QGjtKuXIM", // 8키용
            "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiI0IiwiaWF0IjoxNzU4MDc0MDU3LCJleHAiOjE3NTkyODM2NTd9.A_bI7-lCgjCTPOrk95OFVIRQynhTF0DXZIWW7UYhtEY", // 9키용
        };

        [SerializeField]
        private string testHost = "p-ryan.iptime.org";

        [SerializeField]
        private int testPort = 8888;

        [SerializeField]
        private int testRoomId = -1; // -1은 자동 매칭

        [Header("UI References")]
        [SerializeField]
        private Button joinRoomButton;

        [SerializeField]
        private Text statusText;

        [SerializeField]
        private InputField tokenInputField;

        [SerializeField]
        private InputField roomIdInputField;

        // 의존성 주입
        private LobbyViewModel _viewModel;
        private GameManager _gameManager;
        private ILobbyNetworkSource _lobbyNetworkSource;
        private ISubscriber<TokenVerifiedMessage> _tokenVerifiedSubscriber;
        private IPublisher<LobbyUIStateMessage> _lobbyUIStatePublisher;
        private readonly CompositeDisposable _disposables = new();
        private readonly System.Collections.Generic.List<System.IDisposable> _messageDisposables =
            new();

        private bool _isUIInitialized = false;

        [Inject]
        public void Construct(
            LobbyViewModel viewModel,
            GameManager gameManager,
            ILobbyNetworkSource lobbyNetworkSource,
            ISubscriber<TokenVerifiedMessage> tokenVerifiedSubscriber,
            IPublisher<LobbyUIStateMessage> lobbyUIStatePublisher
        )
        {
            Debug.Log("[LobbySceneManager] Construct 호출됨!");
            _viewModel = viewModel;
            _gameManager = gameManager;
            _lobbyNetworkSource = lobbyNetworkSource;
            _tokenVerifiedSubscriber = tokenVerifiedSubscriber;
            _lobbyUIStatePublisher = lobbyUIStatePublisher;
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

            // UI 초기화 (한 번만)
            if (!_isUIInitialized)
            {
                InitializeUI();
                _isUIInitialized = true;
            }

            // ViewModel 이벤트 구독
            SubscribeToViewModel();

            // 네트워크 이벤트 구독
            SubscribeToNetworkEvents();

            // UI 상태를 대기 상태로 설정
            PublishUIState(LobbyUIStateMessage.Ready("토큰을 입력하고 게임 시작을 눌러주세요"));

            CallAndroidFunction("hideLoadingScreen");

        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                string testParams =
                    "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwiaWF0IjoxNzU4MDczOTQ4LCJleHAiOjE3NTkyODM1NDh9.cfillJjXl1d92Cw8-dPPSYbZQ90lKhcj2_B8TM-QK9c,-4,p-ryan.iptime.org,8888";
                Debug.Log($"[LobbySceneManager] A키 테스트: {testParams}");
                StartGameCommandFromAndroid(testParams);
            }

            // 숫자 키로 프리셋 토큰 입력
            if (tokenInputField != null)
            {
                if (Input.GetKeyDown(KeyCode.Alpha7))
                {
                    tokenInputField.text = presetTokens[1];
                    Debug.Log("[LobbySceneManager] 7키로 프리셋 토큰 1 입력됨");
                    PublishUIState(LobbyUIStateMessage.Ready("프리셋 토큰 2번 (USER2) 적용됨"));
                }
                else if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    tokenInputField.text = presetTokens[2];
                    Debug.Log("[LobbySceneManager] 8키로 프리셋 토큰 2 입력됨");
                    PublishUIState(LobbyUIStateMessage.Ready("프리셋 토큰 3번 (USER3) 적용됨"));
                }
                else if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    tokenInputField.text = presetTokens[3];
                    Debug.Log("[LobbySceneManager] 9키로 프리셋 토큰 3 입력됨");
                    PublishUIState(LobbyUIStateMessage.Ready("프리셋 토큰 4번 (USER4) 적용됨"));
                }
                else if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    tokenInputField.text = presetTokens[0];
                    Debug.Log("[LobbySceneManager] 0키로 기본 토큰 입력됨");
                    PublishUIState(LobbyUIStateMessage.Ready("기본 토큰 (USER1) 적용됨"));
                }
            }
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

            // 토큰 입력 필드 초기화
            if (tokenInputField != null)
            {
                // 입력 필드가 비어있을 때만 기본 토큰으로 설정
                if (string.IsNullOrEmpty(tokenInputField.text))
                {
                    tokenInputField.text = testAccessToken;
                }
                tokenInputField.placeholder.GetComponent<Text>().text =
                    "액세스 토큰 (7,8,9,0키로 프리셋 변경)";
            }

            // 방 번호 입력 필드 초기화
            if (roomIdInputField != null)
            {
                // 입력 필드가 비어있을 때만 테스트 방 번호로 설정
                if (string.IsNullOrEmpty(roomIdInputField.text))
                {
                    roomIdInputField.text = testRoomId.ToString();
                }
                roomIdInputField.placeholder.GetComponent<Text>().text =
                    "방 번호 입력 (-1은 자동 매칭)";
                Debug.Log(
                    $"[LobbySceneManager] 방 번호 필드 초기화: testRoomId={testRoomId}, 입력값='{roomIdInputField.text}'"
                );
            }

            // 초기 상태 설정
            if (joinRoomButton != null)
                joinRoomButton.interactable = true;

            // InputField들 활성화
            if (tokenInputField != null)
                tokenInputField.interactable = true;

            if (roomIdInputField != null)
                roomIdInputField.interactable = true;

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

        public void StartGameCommandFromAndroid(string paramsString)
        {
            var parts = paramsString.Split(',');

            if (
                parts.Length < 4
                || !int.TryParse(parts[1], out int roomId)
                || !int.TryParse(parts[3], out int port)
            )
            {
                Debug.LogError($"Invalid parameters: {paramsString}");
                return;
            }

            String token = parts[0];
            String host = parts[2];
            testHost = host;
            testPort = port;

            StartGameWithTokenAndRoom(token, roomId);
        }

        /// <summary>
        /// 입력된 토큰과 방 번호로 게임 시작
        /// </summary>
        private async void StartGameWithTokenAndRoom(string token, int roomId)
        {
            try
            {
                PublishUIState(LobbyUIStateMessage.Connecting("서버 연결 중..."));

                // 네트워크 초기화
                await _lobbyNetworkSource.InitializeNetworkAsync(testHost, testPort);
                Debug.Log("[LobbySceneManager] 네트워크 초기화 성공");

                // 토큰 인증
                PublishUIState(LobbyUIStateMessage.Connecting("토큰 인증 중..."));
                var authResult = await _lobbyNetworkSource.VerifyTokenAsync(token);
                Debug.Log($"[LobbySceneManager] 토큰 인증 결과: {authResult.Success}");

                if (authResult.Success)
                {
                    // 방 입장 전 RoomStorage 초기화
                    var roomStorage = RoomStorage.Instance;
                    roomStorage.ClearRoom();
                    Debug.Log("[LobbySceneManager] RoomStorage 초기화 완료");

                    // 방 입장 시도
                    PublishUIState(LobbyUIStateMessage.Connecting($"방 {roomId} 입장 중..."));
                    var joinResult = await _lobbyNetworkSource.JoinRoomAsync(roomId);

                    if (joinResult.Success)
                    {
                        PublishUIState(
                            LobbyUIStateMessage.Connecting(
                                "방 입장 성공! 맵과 플레이어 데이터 대기 중..."
                            )
                        );
                        Debug.Log(
                            $"[LobbySceneManager] 방 {roomId} 입장 성공 - 맵과 플레이어 데이터 대기"
                        );

                        // Room 데이터가 완전히 초기화될 때까지 대기
                        WaitForRoomInitialization();
                    }
                    else
                    {
                        PublishUIState(
                            LobbyUIStateMessage.Error($"방 입장 실패: {joinResult.Result}")
                        );
                        Debug.LogError($"[LobbySceneManager] 방 입장 실패: {joinResult.Result}");
                    }
                }
                else
                {
                    PublishUIState(LobbyUIStateMessage.Error("토큰 인증 실패"));
                    Debug.LogError("[LobbySceneManager] 토큰 인증 실패");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySceneManager] 게임 시작 오류: {e.Message}");
                PublishUIState(LobbyUIStateMessage.Error($"게임 시작 실패: {e.Message}"));
            }
        }

        /// <summary>
        /// Room 초기화 완료까지 대기
        /// </summary>
        private async void WaitForRoomInitialization()
        {
            Debug.Log("[LobbySceneManager] Room 초기화 대기 시작");

            const int maxWaitTime = 60000; // 60초 최대 대기
            const int checkInterval = 100; // 100ms마다 체크
            int elapsedTime = 0;

            while (elapsedTime < maxWaitTime)
            {
                var roomStorage = RoomStorage.Instance;
                if (roomStorage?.HasReceivedNewRoomData() == true)
                {
                    Debug.Log("[LobbySceneManager] Room 초기화 완료! 로딩 씬으로 전환");
                    PublishUIState(
                        LobbyUIStateMessage.Ready("맵과 플레이어 데이터 수신 완료! 로딩 중...")
                    );
                    OnRoomJoinedSuccessfully();
                    return;
                }

                await System.Threading.Tasks.Task.Delay(checkInterval);
                elapsedTime += checkInterval;

                // 중간 상태 업데이트
                if (elapsedTime % 1000 == 0)
                {
                    int remainingSeconds = (maxWaitTime - elapsedTime) / 1000;
                    PublishUIState(
                        LobbyUIStateMessage.Connecting(
                            $"맵과 플레이어 데이터 대기 중... ({remainingSeconds}초 남음)"
                        )
                    );
                    Debug.Log(
                        $"[LobbySceneManager] Room 데이터 대기 중... {remainingSeconds}초 남음, HasReceivedNewRoomData: {roomStorage?.HasReceivedNewRoomData() ?? false}"
                    );
                }
            }

            // 타임아웃
            Debug.LogError("[LobbySceneManager] Room 초기화 타임아웃!");
            PublishUIState(
                LobbyUIStateMessage.Error("서버로부터 게임 데이터를 받는데 실패했습니다 (타임아웃)")
            );
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
            Debug.Log("[LobbySceneManager] 🎮 게임 시작 버튼 클릭!");

            // 입력된 토큰 가져오기
            string inputToken = tokenInputField != null ? tokenInputField.text.Trim() : "";

            if (string.IsNullOrEmpty(inputToken))
            {
                PublishUIState(
                    LobbyUIStateMessage.Error(
                        "토큰을 입력해주세요 (7,8,9,0키로 프리셋 토큰 사용 가능)"
                    )
                );
                return;
            }

            // 입력된 방 번호 가져오기
            int inputRoomId = testRoomId; // 기본값
            Debug.Log($"[LobbySceneManager] 방 번호 파싱 시작: testRoomId={testRoomId}");

            if (roomIdInputField != null)
            {
                string roomIdText = roomIdInputField.text.Trim();
                Debug.Log(
                    $"[LobbySceneManager] 입력 필드 값: '{roomIdText}' (비어있음: {string.IsNullOrEmpty(roomIdText)})"
                );

                if (!string.IsNullOrEmpty(roomIdText))
                {
                    if (!int.TryParse(roomIdText, out inputRoomId))
                    {
                        Debug.LogError($"[LobbySceneManager] 방 번호 파싱 실패: '{roomIdText}'");
                        PublishUIState(LobbyUIStateMessage.Error("방 번호는 숫자여야 합니다"));
                        return;
                    }
                    Debug.Log($"[LobbySceneManager] 방 번호 파싱 성공: {inputRoomId}");
                }
                else
                {
                    Debug.Log(
                        $"[LobbySceneManager] 입력 필드가 비어있어서 기본값 사용: {inputRoomId}"
                    );
                }
            }
            else
            {
                Debug.Log("[LobbySceneManager] 방 번호 입력 필드가 null이어서 기본값 사용");
            }

            Debug.Log($"[LobbySceneManager] 최종 사용할 방 번호: {inputRoomId}");

            // 입력된 토큰과 방 번호로 게임 시작
            StartGameWithTokenAndRoom(inputToken, inputRoomId);
        }

        /// <summary>
        /// 네트워크 이벤트 구독
        /// </summary>
        private void SubscribeToNetworkEvents()
        {
            // VContainer로 주입받은 Subscriber 사용
            var subscription = _tokenVerifiedSubscriber.Subscribe(OnTokenVerified);
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
                PublishUIState(
                    LobbyUIStateMessage.Error($"토큰 인증 실패: {message.ErrorMessage}")
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
            // VContainer로 주입받은 Publisher 사용
            _lobbyUIStatePublisher.Publish(message);
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
                CallAndroidFunction("onInGameLoadingStart");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LobbySceneManager] Loading 씬 전환 실패: {e.Message}");
            }
        }

        void CallAndroidFunction(string functionName, params string[] parameters)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (
                AndroidJavaClass unityPlayer = new AndroidJavaClass(
                    "com.unity3d.player.UnityPlayer"
                )
            )
            {
                using (
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>(
                        "currentActivity"
                    )
                )
                {
                    currentActivity.Call(functionName, parameters);
                    Debug.Log(
                        $"안드로이드 함수 호출: {functionName}({string.Join(", ", parameters)})"
                    );
                }
            }
#else
            Debug.Log(
                $"[에디터/비안드로이드] 안드로이드 함수 호출 시뮬레이션: {functionName}({string.Join(", ", parameters)})"
            );
#endif
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
