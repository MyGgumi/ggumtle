using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Network;
using Networks.Attributes;
using Networks.Packets;
using Networks.Players;
using Networks.Rooms;
using UnityEngine;

namespace Networks
{
    public class RoomManager : MonoBehaviour
    {
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

        private RoomStorage _roomStorage;
        private Room _room;
        
        private NetworkApi _networkApi;

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

        [CommandHandler(PacketType.PlayerMoveResponse)]
        public async void PlayerMove(PlayerMoveCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"PlayerMoveCommand: [{command.PlayerId}] : {command.Position.ToString()}");
        }
    }
}