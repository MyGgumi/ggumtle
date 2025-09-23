using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;
using Network;
using Networks.Attributes;
using Networks.Chests;
using Networks.Game;
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
            Debug.Log("[RoomManager] 초기화 시작");
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        // Start is called before the first frame update
        async void Start()
        {
            Debug.Log("[RoomManager] 시작");
            
            _roomStorage = RoomStorage.Instance;

            _room = _roomStorage.Room;

            if (_room == null || !_room.IsInitialized())
            {
                Debug.LogError("[RoomManager] 방 초기화 실패");
                return;
            }
            
            Debug.Log("[RoomManager] 방 초기화 완료");
            
            var go = GameObject.Find("NetworkApi");
            _networkApi = go.GetComponent<NetworkApi>();

            if (_networkApi == null)
            {
                Debug.LogError("[RoomManager] NetworkApi 초기화 실패");
            }

            try
            {
                var command = await _networkApi.SceneChange();

                if (command.Success)
                {
                    Debug.Log("[RoomManager] 씬 전환 성공");
                }
                else
                {
                    Debug.LogError("[RoomManager] 씬 전환 실패");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomManager] 씬 전환 오류: {e.Message}");
                throw;
            }
        }

        [CommandHandler(PacketType.GameStart)]
        public async void GameStart(GameStartCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log("[RoomManager] 게임 시작 수신");
        }

        [CommandHandler(PacketType.PlayerMoveResponse)]
        public async void PlayerMove(PlayerMoveCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 플레이어 이동: PlayerId={command.PlayerId}, Position={command.Position}");
        }

        [CommandHandler(PacketType.DiggingDoneResponse)]
        public async void DiggingDone(DiggingDoneCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 꿈틀이 파기 완료: Id={command.Id}, IsRealGgumtle={command.IsRealGgumtle}");
        }

        [CommandHandler(PacketType.JellyForceQuitResponse)]
        public async void JellyForceQuit(JellyForceQuitCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 젤리 강제 종료: GgumtleId={command.GgumtleId}, LeftJellyCount={command.LeftJellyCount}");
        }

        [CommandHandler(PacketType.MongdungAttackResponse)]
        public async void MongdungAttackResult(MongdungAttackCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 몽둥이 공격 결과: Result={command.Result}, LeftHp={command.leftHp}");
        }

        [CommandHandler(PacketType.GetItemResponse)]
        public async void GetItemResult(GetItemCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 아이템 획득 결과: Result={command.Result}, Items=[{string.Join(", ", command.Items)}]");
        }

        [CommandHandler(PacketType.PutItemResponse)]
        public async void PutItemResult(PutItemCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 아이템 넣기 결과: Result={command.Result}, Items=[{string.Join(", ", command.Items)}]");
        }

        [CommandHandler(PacketType.MonggingRevivalComplete)]
        public async void MonggingRevivalComplete(MonggingRevivalCompleteCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 몽깅이 부활 완료: RevivedMonggingId={command.revivedMonggingId}");
            
            // 몽깅이 부활 완료 처리 로직을 여기에 추가
            // 예: UI 업데이트, 게임 상태 변경 등
        }

        [CommandHandler(PacketType.MongdungSkillResponse)]
        public async void MongdungSkillResult(MongdungSkillCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 몽둥이 스킬 결과: SkillType={command.skillType}, Result={command.Result}");
            
            // 몽둥이 스킬 결과 처리 로직을 여기에 추가
            if (command.skillType == 1) // 공포 스킬
            {
                Debug.Log($"[RoomManager] 공포 스킬 결과: {command.Result}");
                // 공포 스킬 성공 시 모든 플레이어에게 전송됨
                // 공포 스킬 실패 시 몽둥이에게만 전송됨
            }
            else if (command.skillType == 2) // 꿈틀이 심기
            {
                Debug.Log($"[RoomManager] 꿈틀이 심기 결과: {command.Result}");
                // 꿈틀이 심기는 성공/실패 모두 몽둥이에게만 전송됨
            }
        }

        [CommandHandler(PacketType.GgumtleSpawn)]
        public async void GgumtleSpawn(GgumtleSpawnCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 꿈틀이 스폰: Id={command.id}, Position={command.position}");
            
            // 꿈틀이 스폰 처리 로직을 여기에 추가
            // 예: 꿈틀이 오브젝트 생성, UI 업데이트 등
        }

        [CommandHandler(PacketType.GgumtleNirvana)]
        public async void GgumtleNirvana(GgumtleNirvanaCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 꿈틀이 성불: Id={command.id}");
            
            // 꿈틀이 성불 처리 로직을 여기에 추가
            // 예: 꿈틀이 오브젝트 제거, UI 업데이트, 효과 재생 등
        }

        [CommandHandler(PacketType.MonggingStateBroadcast)]
        public async void MonggingStateBroadcast(MonggingStateBroadcastCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 몽깅이 상태 전파: PlayerId={command.playerId}, Type={command.type}");
            
            // 몽깅이 상태 전파 처리 로직을 여기에 추가
            // 예: 몽깅이 상태 변경, UI 업데이트, 애니메이션 재생 등
        }

        [CommandHandler(PacketType.ExitOpen)]
        public async void ExitOpen(ExitOpenCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 탈출구 오픈: Count={command.count}, Exits=[{string.Join(", ", command.exits)}]");
            
            // 탈출구 오픈 처리 로직을 여기에 추가
            // 예: 탈출구 오브젝트 활성화, UI 업데이트, 효과 재생 등
        }

        [CommandHandler(PacketType.GameEnd)]
        public async void GameEnd(GameEndCommand command, IChannelHandlerContext ctx)
        {
            Debug.Log($"[RoomManager] 게임 종료: Result={command.result}, PlayerCount={command.playerResults?.Count ?? 0}, EscapedCount={command.escapedMonggingCount}");
            
            // 플레이어 결과 출력
            foreach (var playerResult in command.playerResults)
            {
                Debug.Log($"[RoomManager] 플레이어 결과: Id={playerResult.id}, Status={playerResult.status}");
            }
            
            // 게임 종료 처리 로직을 여기에 추가
            // 예: 결과 화면 표시, 점수 계산, UI 업데이트 등
        }
    }
}