using System;
using System.Net.Security;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Networks.Attributes;
using Networks.Packets;
using Networks.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Networks
{
    public class LobbyManager : MonoBehaviour
    {
        [Header("Server")]
        [SerializeField] private string host = "";
        [SerializeField] private int port = 0;
        [SerializeField] private string accessToken = "your_access_token_here";

        [Header("UI Test")]
        [SerializeField] private InputField accessTokenInputField;
        [SerializeField] private Button enterButton;

        [Header("Module")]
        [SerializeField] private NetworkApi networkApi;
        
        private RoomStorage roomStorage;

        private Room _room = null;
        private bool _isVerified = false;
        private bool _isRoomJoined = false;
        private bool _enterCalled = false;

        private static LobbyManager _instance;
        public static LobbyManager Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        void Awake()
        {
            Debug.Log("[LobbyManager] 초기화 시작");
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            Debug.Log("[LobbyManager] 시작");
            
            if (networkApi == null)
            {
                Debug.LogError("[LobbyManager] NetworkApi 컴포넌트를 찾을 수 없습니다. LobbyManager GameObject에 NetworkApi를 추가해주세요.");
                return;
            }

            await networkApi.InitializeNetwork(host, port);
            
            roomStorage = RoomStorage.Instance;
            
            if (roomStorage == null)
            {
                Debug.LogError("[LobbyManager] RoomStorage 컴포넌트를 찾을 수 없습니다.");
                return;
            }
            
            enterButton.onClick.AddListener(OnEnterButtonClicked);
            Debug.Log("[LobbyManager] 버튼 이벤트 연결 완료");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private async Task JoinRoom()
        {
            try
            {
                var command = await networkApi.RoomJoin(-1);

                if (command.Result == RoomJoinResult.Success)
                {
                    CreateRoom();
                    _isRoomJoined = true;
                    Debug.Log("[LobbyManager] 방 조인 성공");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[LobbyManager] 방 조인 실패");
            }
        }

        private async void OnEnterButtonClicked()
        {
            try
            {
                Debug.Log("[LobbyManager] 입장 버튼 클릭");
                // NetworkApi null 체크
                if (networkApi == null)
                {
                    Debug.LogError("[LobbyManager] NetworkApi가 초기화되지 않았습니다.");
                    return;
                }

                if (!_isVerified)
                {
                    var token = accessTokenInputField?.text;
                    if (string.IsNullOrEmpty(token))
                    {
                        Debug.LogError("[LobbyManager] AccessToken이 입력되지 않았습니다.");
                        return;
                    }
                    
                    Debug.Log($"[LobbyManager] AccessToken: {token}");

                    var command = await networkApi.VerifyToken(token);

                    if (command is not { Success: true })
                    {
                        Debug.LogError("[LobbyManager] 인증 실패");
                        return;
                    }
                    
                    _isVerified = true;
                    Debug.Log($"[LobbyManager] 인증 완료: SessionId={command.SessionId}");
                    return;
                }

                if (!_isVerified)
                {
                    Debug.LogWarning("[LobbyManager] 인증 상태 확인 필요");
                    return;
                }

                if (!_isRoomJoined)
                {
                    Debug.Log("[LobbyManager] 방 입장 시도");
                    await JoinRoom();
                }

                if (!_isRoomJoined)
                {
                    Debug.LogError("[LobbyManager] 방 입장 실패");
                    return;
                }
                
                Debug.Log($"[LobbyManager] Room: {_room}");

                if (_room == null)
                {
                    Debug.Log("[LobbyManager] 방 생성");
                    CreateRoom();
                }

                if (_room != null && _room.IsInitialized())
                {
                    EnterRoom();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LobbyManager] 인증 과정 실패: {e.Message}");
            }
        }

        void CreateRoom()
        {
            _room = new Room();
        }

        [CommandHandler(PacketType.InitializeMapResponse)]
        public void InitializeMap(InitializeMapCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log("[LobbyManager] 맵 초기화");
            Debug.Log($"[LobbyManager] 상자들: {command.chests}");
            Debug.Log($"[LobbyManager] 꿈틀이: {command.ggumtles}");
            Debug.Log($"[LobbyManager] 힐팩들: {command.healPacks}");
            Debug.Log($"[LobbyManager] 속도팩: {command.speedPacks}");
            
            _room.InitMap(command);
        }
        
        [CommandHandler(PacketType.InitializePlayerResponse)]
        public void InitializePlayer(InitializePlayerCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log("[LobbyManager] 플레이어 초기화");
            Debug.Log($"[LobbyManager] 플레이어들: {command.players}");
            _room.InitPlayer(command);
        }

        void EnterRoom()
        {
            _enterCalled = true;
            roomStorage.UploadRoom(_room);
            
            SceneManager.LoadScene("Main");
        }
    }
}