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
            Debug.Log("이거 로딩이 안 되나?");
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            Debug.Log("Start 왜 안 됨?");
            
            if (networkApi == null)
            {
                Debug.LogError("NetworkApi 컴포넌트를 찾을 수 없습니다. LobbyManager GameObject에 NetworkApi를 추가해주세요.");
                return;
            }

            await networkApi.InitializeNetwork(host, port);
            
            roomStorage = RoomStorage.Instance;
            
            if (roomStorage == null)
            {
                Debug.LogError("DataManager 컴포넌트를 찾을 수 없습니다.");
                return;
            }
            
            enterButton.onClick.AddListener(OnEnterButtonClicked);
            Debug.Log("버튼 이벤트 연결");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private async Task JoinRoom()
        {
            try
            {
                var command = await networkApi.RoomJoin(-1);

                if (command.Result == 1)
                {
                    CreateRoom();
                    _isRoomJoined = true;
                    Debug.Log("방 조인에 성공함");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("방 조인에 실패함");
            }
        }

        private async void OnEnterButtonClicked()
        {
            try
            {
                Debug.Log("이거 되나?");
                // NetworkApi null 체크
                if (networkApi == null)
                {
                    Debug.LogError("NetworkApi가 초기화되지 않았습니다.");
                    return;
                }

                if (!_isVerified)
                {
                    var token = accessTokenInputField?.text;
                    if (string.IsNullOrEmpty(token))
                    {
                        Debug.LogError("AccessToken이 입력되지 않았습니다.");
                        return;
                    }
                    
                    Debug.Log($"AccessToken: {token}");

                    var command = await networkApi.VerifyToken(token);

                    if (command is not { Success: true })
                    {
                        Debug.Log("인증에 실패했습니다");
                        return;
                    }
                    
                    _isVerified = true;
                    Debug.Log($"인증이 완료되었습니다. sessionId: {command.SessionId}");
                    return;
                }

                if (!_isVerified)
                {
                    Debug.Log("여전히 인증 안 되어 있음");
                    return;
                }

                if (!_isRoomJoined)
                {
                    Debug.Log("방 입장 안 되어있음");
                    await JoinRoom();
                }

                if (!_isRoomJoined)
                {
                    Debug.Log("여전히 방 입장 안 되어있음");
                    return;
                }
                
                Debug.Log($"Room: {_room}");

                if (_room == null)
                {
                    Debug.Log("방만들기");
                    CreateRoom();
                }

                if (_room != null && _room.IsInitialized())
                {
                    EnterRoom();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"인증 과정에 실패했습니다. {e.Message}");
            }
        }

        void CreateRoom()
        {
            _room = new Room();
        }

        [CommandHandler(PacketType.InitializeMapResponse)]
        public void InitializeMap(InitializeMapCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"맵 초기화");
            Debug.Log($"상자들: {command.chests}");
            Debug.Log($"꿈틀이: {command.ggumtles}");
            Debug.Log($"힐팩들: {command.healPacks}");
            Debug.Log($"속도팩: {command.speedPacks}");
            
            _room.InitMap(command);
        }
        
        [CommandHandler(PacketType.InitializePlayerResponse)]
        public void InitializePlayer(InitializePlayerCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log("플레이어 초기화");
            Debug.Log($"플레이어들: {command.players}");
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