using System;
using DotNetty.Transport.Channels;
using Features.Map.Utils;
using Networks;
using Networks.Attributes;
using Networks.Game;
using Networks.Ggumtle;
using Networks.Packets;
using Networks.Rooms;
using UnityEngine;

namespace Features.Room.NetworkSources
{
    /// <summary>
    /// 방 관련 네트워크 이벤트를 처리하는 핸들러
    /// static CommandHandler들만 가지고 있는 순수 핸들러 클래스
    /// </summary>
    public static class RoomNetworkEventHandler
    {
        // static CommandHandler를 위한 static Room 저장소
        private static Networks.Rooms.Room _staticRoom;

        /// <summary>
        /// 맵 초기화 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.InitializeMapResponse)]
        public static void InitializeMap(InitializeMapCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log("[SERVER_ROOM_DATA] 맵 초기화 수신");
                Debug.Log($"[SERVER_ROOM_DATA]   - 상자: {command.chests?.Count ?? 0}개");
                Debug.Log($"[SERVER_ROOM_DATA]   - 꿈틀이: {command.ggumtles?.Count ?? 0}개");
                Debug.Log($"[SERVER_ROOM_DATA]   - 힐팩: {command.healPacks?.Count ?? 0}개");
                Debug.Log($"[SERVER_ROOM_DATA]   - 스피드팩: {command.speedPacks?.Count ?? 0}개");

                // 상자 상세 정보 로깅
                if (command.chests != null && command.chests.Count > 0)
                {
                    Debug.Log($"[SERVER_ROOM_DATA] 상자 상세 정보:");
                    for (int i = 0; i < command.chests.Count; i++)
                    {
                        var chest = command.chests[i];
                        Debug.Log(
                            $"[SERVER_ROOM_DATA]   [{i}] ID: {chest.Id}, Position: {chest.Position}"
                        );
                    }
                }

                // 꿈틀이 상세 정보 로깅
                if (command.ggumtles != null && command.ggumtles.Count > 0)
                {
                    Debug.Log($"[SERVER_ROOM_DATA] 꿈틀이 상세 정보:");
                    for (int i = 0; i < command.ggumtles.Count; i++)
                    {
                        var ggumtle = command.ggumtles[i];
                        Debug.Log(
                            $"[SERVER_ROOM_DATA]   [{i}] ID: {ggumtle.Id}, Position: {ggumtle.Position}"
                        );
                    }
                }

                // RoomStorage 처리
                var roomStorage = RoomStorage.Instance;
                if (roomStorage != null)
                {
                    if (_staticRoom == null)
                    {
                        _staticRoom = new Networks.Rooms.Room();
                        Debug.Log("[RoomNetworkEventHandler] Room 생성 완료");
                    }

                    _staticRoom.InitMap(command);
                    Debug.Log("[SERVER_ROOM_DATA] Room에 맵 데이터 저장 완료");

                    // 변환된 Room 데이터 확인
                    if (_staticRoom.chests != null && _staticRoom.chests.Count > 0)
                    {
                        Debug.Log($"[SERVER_ROOM_DATA] Room 저장 후 상자 데이터 확인:");
                        for (int i = 0; i < _staticRoom.chests.Count; i++)
                        {
                            var chest = _staticRoom.chests[i];
                            var unityPos = chest.ToVector3();
                            Debug.Log(
                                $"[SERVER_ROOM_DATA]   [{i}] ID: {chest.Id}, Unity Position: {unityPos}"
                            );
                        }
                    }

                    if (_staticRoom.ggumtles != null && _staticRoom.ggumtles.Count > 0)
                    {
                        Debug.Log($"[SERVER_ROOM_DATA] Room 저장 후 꿈틀이 데이터 확인:");
                        for (int i = 0; i < _staticRoom.ggumtles.Count; i++)
                        {
                            var ggumtle = _staticRoom.ggumtles[i];
                            var unityPos = ggumtle.ToVector3();
                            Debug.Log(
                                $"[SERVER_ROOM_DATA]   [{i}] ID: {ggumtle.Id}, Unity Position: {unityPos}"
                            );
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomNetworkEventHandler] 맵 초기화 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 플레이어 초기화 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.InitializePlayerResponse)]
        public static void InitializePlayer(
            InitializePlayerCommand command,
            IChannelHandlerContext ctx
        )
        {
            try
            {
                Debug.Log("[RoomNetworkEventHandler] 플레이어 초기화 수신");
                Debug.Log($"  - 플레이어: {command.players?.Count ?? 0}명");

                // RoomStorage 처리
                var roomStorage = RoomStorage.Instance;
                if (roomStorage != null)
                {
                    if (_staticRoom == null)
                    {
                        _staticRoom = new Networks.Rooms.Room();
                        Debug.Log("[RoomNetworkEventHandler] Room 생성 완료");
                    }

                    _staticRoom.InitPlayer(command);
                    Debug.Log("[RoomNetworkEventHandler] Room에 플레이어 데이터 저장 완료");

                    // 맵과 플레이어 데이터가 모두 준비되면 RoomStorage에 업로드
                    if (_staticRoom.IsInitialized())
                    {
                        Debug.Log(
                            "[SERVER_ROOM_DATA] Room 완전 초기화 완료 - RoomStorage에 업로드 시작"
                        );

                        // 최종 꿈틀이 데이터 확인
                        if (_staticRoom.ggumtles != null && _staticRoom.ggumtles.Count > 0)
                        {
                            Debug.Log($"[SERVER_ROOM_DATA] 최종 꿈틀이 데이터 (업로드 전):");
                            for (int i = 0; i < _staticRoom.ggumtles.Count; i++)
                            {
                                var ggumtle = _staticRoom.ggumtles[i];
                                var unityPos = ggumtle.ToVector3();
                                Debug.Log(
                                    $"[SERVER_ROOM_DATA]   [{i}] ID: {ggumtle.Id}, Unity Position: {unityPos}"
                                );
                            }
                        }

                        roomStorage.UploadRoom(_staticRoom);
                        Debug.Log("[SERVER_ROOM_DATA] RoomStorage에 Room 데이터 업로드 완료");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomNetworkEventHandler] 플레이어 초기화 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 시작 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GameStart)]
        public static void GameStart(GameStartCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log("[RoomNetworkEventHandler] 게임 시작 수신");

                // TODO: RoomService로 처리 위임 필요
                // _roomService.StartGame();
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomNetworkEventHandler] 게임 시작 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 게임 종료 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GameEnd)]
        public static void GameEnd(GameEndCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log(
                    $"[RoomNetworkEventHandler] 게임 종료 수신: Result={command.result}, PlayerCount={command.playerResults?.Count ?? 0}, EscapedCount={command.escapedMonggingCount}"
                );

                // 플레이어 결과 출력
                if (command.playerResults != null)
                {
                    foreach (var playerResult in command.playerResults)
                    {
                        Debug.Log(
                            $"  - 플레이어 결과: Id={playerResult.id}, Status={playerResult.status}"
                        );
                    }
                }

                // TODO: GameService나 GameStateService로 게임 종료 처리 위임
                // _gameService.EndGame(command);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomNetworkEventHandler] 게임 종료 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 꿈틀이 스폰 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GgumtleSpawn)]
        public static void GgumtleSpawn(GgumtleSpawnCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log(
                    $"[RoomNetworkEventHandler] 꿈틀이 스폰: Id={command.id}, Position={command.position}"
                );

                // TODO: RoomService로 처리 위임 필요
                // var unityPosition = new UnityEngine.Vector3(command.position.X, command.position.Y, command.position.Z);
                // _roomService.SpawnGgumtle(command.id, unityPosition);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomNetworkEventHandler] 꿈틀이 스폰 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 꿈틀이 상태 이벤트 처리
        /// </summary>
        [CommandHandler(PacketType.GgumtleState)]
        public static void GgumtleState(GgumtleStatusCommand command, IChannelHandlerContext ctx)
        {
            try
            {
                Debug.Log(
                    $"[RoomNetworkEventHandler] 꿈틀이 상태: Id={command.ggumtleId}, State={command.state}"
                );

                // TODO: RoomService로 처리 위임 필요
                // _roomService.RemoveGgumtle(command.ggumtleId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RoomNetworkEventHandler] 꿈틀이 상태 처리 실패: {e.Message}");
            }
        }

        // TODO: 기타 게임 관련 이벤트들 추가
        // - PlayerMove
        // - MongdungAttack
        // - GetItem/PutItem
        // - MonggingRevival
        // - ExitOpen
        // 등등... 필요에 따라 다른 Handler로 분리할 수도 있음
        // GameStart와 GameEnd 핸들러는 MainGameNetworkEventHandler로 이동됨
    }
}
