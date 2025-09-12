using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Network;
using Networks.Attributes;
using Networks.Packets;
using UnityEngine;

namespace Networks.Rooms
{
    public class RoomManager : MonoBehaviour
    {
        private RoomStorage _roomStorage;
        private Room _room;
        
        private static RoomManager _instance;
        public static RoomManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RoomManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RoomManager");
                        _instance = go.AddComponent<RoomManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private NetworkApi _networkApi;
        
        // 초기화 상태 추적
        private bool _isMapInitialized = false;
        private bool _isPlayerInitialized = false;
        
        // 저장된 데이터
        public InitializeMapCommand MapData { get; private set; }
        public InitializePlayerCommand PlayerData { get; private set; }
        
        // 초기화 완료 이벤트
        public event Action OnInitializationComplete;
        
        void Awake()
        {
            Debug.Log("RoomManager Awake");
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // Start is called before the first frame update
        async void Start()
        {
            Debug.Log("RoomManager Start");
            
            _roomStorage = RoomStorage.Instance;

            _room = _roomStorage.Room;

            if (_room == null || !_room.IsInitialized())
            {
                Debug.LogError("Room 초기화 안 됨!");
                return;
            }
            
            Debug.Log("Room 초기화 완료!");
            
            var go = GameObject.Find("NetworkApi");
            _networkApi = go.GetComponent<NetworkApi>();

            if (_networkApi == null)
            {
                Debug.LogError("[RoomManager] NetworkApi 초기화 안 됨!!");
            }

            try
            {
                var command = await _networkApi.SceneChange();

                if (command.Success)
                {
                    Debug.Log("씬 전환 성공!!!");
                }
                else
                {
                    Debug.Log("씬 전환 실패 ㅜㅜ");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"{e.Message}");
                throw;
            }
        }

        [CommandHandler(PacketType.GameStart)]
        public async void GameStart(GameStartCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log("게임 시작 들어옴");
        }

        /// <summary>
        /// 씬 전환 시작
        /// </summary>
        private async void StartSceneTransition()
        {
            try
            {
                // 1단계: 서버에 씬 전환 요청
                Debug.Log("1단계: 서버에 씬 전환 요청");
                var sceneChangeResponse = await _networkApi.SceneChange();
                
                if (!sceneChangeResponse.Success)
                {
                    Debug.LogError($"씬 전환 요청 실패: Result={sceneChangeResponse.Result}");
                    return;
                }
                
                Debug.Log($"씬 전환 요청 성공: Result={sceneChangeResponse.Result}");
                
                // 2단계: Unity 씬 전환
                Debug.Log("2단계: Unity 씬 전환");
                await MainThreadDispatcher.Instance.EnqueueAsync(async () =>
                {
                    try
                    {
                        await ChangeScene("Main");
                        Debug.Log("Unity 씬 전환 완료");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Unity 씬 전환 중 오류 발생: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"씬 전환 중 오류 발생: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Unity 씬 전환
        /// </summary>
        private async Task ChangeScene(string sceneName)
        {
            try
            {
                Debug.Log($"Unity 씬 전환 시작: {sceneName}");
                
                // 씬이 빌드 설정에 있는지 확인
                var sceneIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{sceneName}.unity");
                if (sceneIndex == -1)
                {
                    Debug.LogError($"씬 '{sceneName}'이 빌드 설정에 없습니다. File > Build Settings에서 씬을 추가해주세요.");
                    throw new Exception($"씬 '{sceneName}'이 빌드 설정에 없습니다.");
                }
                
                // Unity의 SceneManager를 사용한 씬 전환
                var loadSceneAsync = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
                
                if (loadSceneAsync == null)
                {
                    Debug.LogError($"씬 '{sceneName}' 로딩에 실패했습니다.");
                    throw new Exception($"씬 '{sceneName}' 로딩에 실패했습니다.");
                }
                
                // 씬 로딩 완료까지 대기
                while (!loadSceneAsync.isDone)
                {
                    await Task.Delay(100); // 100ms마다 체크
                }
                
                Debug.Log($"Unity 씬 전환 완료: {sceneName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unity 씬 전환 중 오류 발생: {ex.Message}");
                throw;
            }
        }

    }
}