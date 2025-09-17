using UnityEngine;
using Models;
using System.Collections.Generic;

namespace Services
{
    /// <summary>
    /// 꿈틀이 비즈니스 로직을 담당하는 서비스 인터페이스
    /// 순수 C# 클래스로 구현되어 테스트 가능하고 DI 친화적
    /// </summary>
    public interface IGgumtleService
    {
        #region Ggumtle Management

        /// <summary>
        /// 꿈틀이 등록
        /// </summary>
        void RegisterGgumtle(string ggumtleId, string name, Vector3 position);

        /// <summary>
        /// 꿈틀이 데이터 조회
        /// </summary>
        GgumtleData GetGgumtleData(string ggumtleId);

        /// <summary>
        /// 모든 꿈틀이 데이터 조회
        /// </summary>
        Dictionary<string, GgumtleData> GetAllGgumtleData();

        /// <summary>
        /// 꿈틀이 제거
        /// </summary>
        void UnregisterGgumtle(string ggumtleId);

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

        #region Utility

        /// <summary>
        /// 서비스 상태 리셋
        /// </summary>
        void ResetService();

        #endregion
    }
}