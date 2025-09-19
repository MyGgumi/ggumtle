using Features.Room.Models;
using Networks.Rooms;
using System;

namespace Features.Room.Services
{
    /// <summary>
    /// 방 데이터 관리를 담당하는 서비스 인터페이스
    /// </summary>
    public interface IRoomService
    {
        /// <summary>
        /// 현재 방 데이터
        /// </summary>
        RoomData CurrentRoom { get; }

        /// <summary>
        /// 방 데이터 변경 이벤트
        /// </summary>
        event Action<RoomData> OnRoomChanged;

        /// <summary>
        /// 맵 초기화 완료 이벤트
        /// </summary>
        event Action<RoomData> OnMapInitialized;

        /// <summary>
        /// 플레이어 초기화 완료 이벤트
        /// </summary>
        event Action<RoomData> OnPlayersInitialized;

        /// <summary>
        /// 게임 시작 이벤트
        /// </summary>
        event Action OnGameStarted;

        /// <summary>
        /// 방 데이터 설정
        /// </summary>
        /// <param name="room">설정할 방 데이터</param>
        void SetCurrentRoom(Networks.Rooms.Room room);

        /// <summary>
        /// 맵 데이터로 초기화
        /// </summary>
        /// <param name="command">맵 초기화 명령</param>
        void InitializeMap(InitializeMapCommand command);

        /// <summary>
        /// 플레이어 데이터로 초기화
        /// </summary>
        /// <param name="command">플레이어 초기화 명령</param>
        void InitializePlayers(InitializePlayerCommand command);

        /// <summary>
        /// 게임 시작 처리
        /// </summary>
        void StartGame();

        /// <summary>
        /// 꿈틀이 스폰 처리
        /// </summary>
        /// <param name="id">꿈틀이 ID</param>
        /// <param name="position">스폰 위치</param>
        void SpawnGgumtle(int id, UnityEngine.Vector3 position);

        /// <summary>
        /// 꿈틀이 성불 처리
        /// </summary>
        /// <param name="id">꿈틀이 ID</param>
        void RemoveGgumtle(int id);

        /// <summary>
        /// 방이 초기화되었는지 확인
        /// </summary>
        /// <returns>초기화 상태</returns>
        bool IsRoomInitialized();

        /// <summary>
        /// 방 데이터 정리
        /// </summary>
        void ClearRoom();
    }
}