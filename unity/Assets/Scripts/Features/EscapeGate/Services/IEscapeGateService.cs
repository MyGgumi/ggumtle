using System.Collections.Generic;
using Features.EscapeGate.Models;
using R3;
using UnityEngine;

namespace Features.EscapeGate.Services
{
    /// <summary>
    /// 탈출 게이트 관리 서비스 인터페이스
    /// </summary>
    public interface IEscapeGateService
    {
        /// <summary>
        /// 현재 감지된 탈출 게이트 ID
        /// </summary>
        ReadOnlyReactiveProperty<int> CurrentGateId { get; }

        /// <summary>
        /// 범위 내에 탈출 게이트가 있는지 여부
        /// </summary>
        ReadOnlyReactiveProperty<bool> IsInRange { get; }

        /// <summary>
        /// 현재 감지된 탈출 게이트의 상태
        /// </summary>
        ReadOnlyReactiveProperty<EscapeGateState> CurrentGateState { get; }

        /// <summary>
        /// 탈출 게이트 등록
        /// </summary>
        void RegisterGate(int gateId, string gateName, Vector3 position, GameObject gateObject);

        /// <summary>
        /// 탈출 게이트 해제
        /// </summary>
        void UnregisterGate(int gateId);

        /// <summary>
        /// 서버에서 탈출구 오픈 신호를 받았을 때 처리
        /// </summary>
        void ActivateGates(int[] gateIds);

        /// <summary>
        /// 탈출 게이트 감지 처리
        /// </summary>
        void OnGateDetected(int gateId, float distance);

        /// <summary>
        /// 탈출 게이트 벗어남 처리
        /// </summary>
        void OnGateLeft(int gateId);

        /// <summary>
        /// 탈출 시도
        /// </summary>
        void AttemptEscape(int gateId);

        /// <summary>
        /// 등록된 모든 탈출 게이트 정보 가져오기
        /// </summary>
        IReadOnlyDictionary<int, EscapeGateData> GetAllGates();

        /// <summary>
        /// 특정 탈출 게이트 정보 가져오기
        /// </summary>
        EscapeGateData GetGate(int gateId);
    }
}