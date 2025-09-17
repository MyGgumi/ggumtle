using UnityEngine;

namespace Interfaces
{
    /// <summary>
    /// UI 상호작용을 위한 공통 인터페이스
    /// 각 상호작용 객체가 UIInteractionManager와 통신하기 위해 구현
    /// </summary>
    public interface IUIInteractable
    {
        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// UI에 표시할 상호작용 텍스트
        /// </summary>
        string GetInteractionText();

        /// <summary>
        /// 상호작용 범위
        /// </summary>
        float GetInteractionRange();

        /// <summary>
        /// 객체의 Transform (범위 계산용)
        /// </summary>
        Transform GetTransform();

        /// <summary>
        /// 상호작용 실행 (클릭 시)
        /// </summary>
        void OnInteract();

        /// <summary>
        /// 홀드가 필요한 상호작용인지
        /// </summary>
        bool RequiresHold();

        /// <summary>
        /// 홀드 필요 시간 (초)
        /// </summary>
        float GetHoldDuration();

        /// <summary>
        /// 홀드 시작
        /// </summary>
        void OnHoldStart();

        /// <summary>
        /// 홀드 진행 (0.0 ~ 1.0)
        /// </summary>
        void OnHoldProgress(float progress);

        /// <summary>
        /// 홀드 완료
        /// </summary>
        void OnHoldComplete();

        /// <summary>
        /// 홀드 취소
        /// </summary>
        void OnHoldCancelled();
    }
}