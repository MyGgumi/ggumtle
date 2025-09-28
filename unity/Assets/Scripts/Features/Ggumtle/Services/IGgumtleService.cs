using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.Ggumtle.Models;
using UnityEngine;

namespace Features.Ggumtle.Services
{
    /// <summary>
    /// 꿈틀이 비즈니스 로직을 담당하는 서비스 인터페이스
    /// 순수 C# 클래스로 구현되어 테스트 가능하고 DI 친화적
    /// </summary>
    public interface IGgumtleService
    {
        #region Ggumtle Management

        /// <summary>
        /// 꿈틀이 등록 (서버 ID 기반)
        /// </summary>
        void RegisterGgumtle(int ggumtleId, string name, Vector3 position);

        /// <summary>
        /// 꿈틀이 등록 (기존 호환용)
        /// </summary>
        void RegisterGgumtle(string ggumtleId, string name, Vector3 position);

        /// <summary>
        /// 꿈틀이 데이터 조회 (서버 ID 기반)
        /// </summary>
        GgumtleData GetGgumtleData(int ggumtleId);

        /// <summary>
        /// 꿈틀이 데이터 조회 (기존 호환용)
        /// </summary>
        GgumtleData GetGgumtleData(string ggumtleId);

        /// <summary>
        /// 모든 꿈틀이 데이터 조회
        /// </summary>
        Dictionary<int, GgumtleData> GetAllGgumtleData();

        /// <summary>
        /// 꿈틀이 제거 (서버 ID 기반)
        /// </summary>
        void UnregisterGgumtle(int ggumtleId);

        #endregion

        #region Hold Interaction

        /// <summary>
        /// 홀드 시작
        /// </summary>
        void StartHold(string ggumtleId);

        /// <summary>
        /// 홀드 진행
        /// </summary>
        void UpdateHoldProgress(string ggumtleId, float progress);

        /// <summary>
        /// 홀드 완료
        /// </summary>
        void CompleteHold(string ggumtleId);

        /// <summary>
        /// 홀드 취소
        /// </summary>
        void CancelHold(string ggumtleId);

        #endregion

        #region Jelly Management

        /// <summary>
        /// 특정 꿈틀이가 먹은 젤리 개수 조회
        /// </summary>
        int GetGgumtleJellyEaten(int ggumtleId);

        #endregion

        #region Feeding System

        /// <summary>
        /// 플레이어가 빛젤리를 가지고 있는지 확인
        /// </summary>
        bool HasLightJelly();

        /// <summary>
        /// 꿈틀이에게 먹이주기
        /// </summary>
        bool FeedGgumtle(string ggumtleId, int amount);

        /// <summary>
        /// 정화 완료 처리
        /// </summary>
        void CompletePurification(string ggumtleId);

        #endregion

        #region Network Integration

        /// <summary>
        /// 네트워크를 통해 꿈틀이 파기 시작
        /// </summary>
        UniTask<bool> StartNetworkDiggingAsync(string ggumtleId);

        /// <summary>
        /// 네트워크를 통해 꿈틀이 파기 중단
        /// </summary>
        UniTask<bool> StopNetworkDiggingAsync();

        /// <summary>
        /// 네트워크를 통해 빛젤리 먹이기 시작
        /// </summary>
        UniTask<bool> StartNetworkFeedingAsync(string ggumtleId);

        /// <summary>
        /// 네트워크를 통해 빛젤리 먹이기 중단 (Fire-and-forget)
        /// </summary>
        bool StopNetworkFeeding();

        /// <summary>
        /// 서버에서 파기 완료 이벤트 처리
        /// </summary>
        void HandleDiggingDone(int ggumtleId, bool isRealGgumtle);

        /// <summary>
        /// 서버에서 강제 먹이기 종료 이벤트 처리
        /// </summary>
        void HandleJellyForceQuit(int ggumtleId, int leftJellyCount);

        /// <summary>
        /// 서버에서 꿈틀이 스폰 이벤트 처리
        /// </summary>
        void HandleGgumtleSpawn(int ggumtleId, Vector3 position);

        /// <summary>
        /// 서버에서 꿈틀이 성불 이벤트 처리
        /// </summary>
        void HandleGgumtleNirvana(int ggumtleId);

        #endregion

        #region Utility

        /// <summary>
        /// 디버그용 - 등록된 꿈틀이 목록과 상태 출력
        /// </summary>
        void LogGgumtleStatus();

        /// <summary>
        /// 서비스 상태 리셋
        /// </summary>
        void ResetService();

        #endregion
    }
}
