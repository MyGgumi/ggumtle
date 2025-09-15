using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Network;
using Networks.Attributes;
using Networks.Chests;
using Networks.Ggumtle;
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

        [CommandHandler(PacketType.DiggingDoneResponse)]
        public async void DiggingDone(DiggingDoneCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"DiggingDoneCommand: [{command.Id}] : {command.IsRealGgumtle}");
        }

        [CommandHandler(PacketType.FeedForceQuitResponse)]
        public async void FeedForceQuit(FeedForceQuitCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"FeedForceQuitCommand: GgumtleId [{command.GgumtleId}] - LeftFeedCount: {command.LeftFeedCount}");
        }

        [CommandHandler(PacketType.MongdungAttackResponse)]
        public async void MongdungAttackResult(MongdungAttackCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"MongdungAttackResult: Result [{command.result}] - LeftHp: {command.leftHp}");
        }

        [CommandHandler(PacketType.GetItemResponse)]
        public async void GetItemResult(GetItemCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"GetItemResult: Result [{command.Result}] - Items: [{string.Join(", ", command.Items)}]");
        }

        [CommandHandler(PacketType.PutItemResponse)]
        public async void PutItemResult(PutItemCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"PutItemResult: Result [{command.Result}] - Items: [{string.Join(", ", command.Items)}]");
        }

        [CommandHandler(PacketType.MonggingRevivalComplete)]
        public async void MonggingRevivalComplete(MonggingRevivalCompleteCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"MonggingRevivalComplete: RevivedMonggingId [{command.revivedMonggingId}]");
            
            // 몽깅이 부활 완료 처리 로직을 여기에 추가
            // 예: UI 업데이트, 게임 상태 변경 등
        }

        [CommandHandler(PacketType.MongdungSkillResponse)]
        public async void MongdungSkillResult(MongdungSkillCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"MongdungSkillResult: SkillType [{command.skillType}] - Result [{command.result}]");
            
            // 몽둥이 스킬 결과 처리 로직을 여기에 추가
            if (command.skillType == 1) // 공포 스킬
            {
                Debug.Log($"공포 스킬 결과: {command.result}");
                // 공포 스킬 성공 시 모든 플레이어에게 전송됨
                // 공포 스킬 실패 시 몽둥이에게만 전송됨
            }
            else if (command.skillType == 2) // 꿈틀이 심기
            {
                Debug.Log($"꿈틀이 심기 결과: {command.result}");
                // 꿈틀이 심기는 성공/실패 모두 몽둥이에게만 전송됨
            }
        }

        [CommandHandler(PacketType.GgumtleSpawn)]
        public async void GgumtleSpawn(GgumtleSpawnCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"GgumtleSpawn: ID [{command.id}] - Position [{command.position}]");
            
            // 꿈틀이 스폰 처리 로직을 여기에 추가
            // 예: 꿈틀이 오브젝트 생성, UI 업데이트 등
        }

        [CommandHandler(PacketType.GgumtleNirvana)]
        public async void GgumtleNirvana(GgumtleNirvanaCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"GgumtleNirvana: ID [{command.id}]");
            
            // 꿈틀이 성불 처리 로직을 여기에 추가
            // 예: 꿈틀이 오브젝트 제거, UI 업데이트, 효과 재생 등
        }
    }
}