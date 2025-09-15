using UnityEngine;

/// <summary>
/// 상호작용 가능한 객체들이 구현해야 하는 인터페이스
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// 상호작용 가능한지 확인
    /// </summary>
    bool CanInteract();

    /// <summary>
    /// 상호작용 실행
    /// </summary>
    void Interact();

    /// <summary>
    /// 상호작용 종료 (거리 멀어지거나 강제 종료시)
    /// </summary>
    void OnInteractionEnd();

    /// <summary>
    /// 상호작용 범위 반환
    /// </summary>
    float GetInteractionRange();

    /// <summary>
    /// 상호작용 가능한 객체의 Transform 반환
    /// </summary>
    Transform GetTransform();

    /// <summary>
    /// 상호작용 가능한 객체의 이름 반환
    /// </summary>
    string GetInteractableName();

    /// <summary>
    /// 홀드 시작 (홀드가 필요한 상호작용에서만 호출)
    /// </summary>
    void OnHoldStart();

    /// <summary>
    /// 홀드 진행 중 (0.0 ~ 1.0 진행도)
    /// </summary>
    /// <param name="progress">홀드 진행도 (0.0 ~ 1.0)</param>
    void OnHoldProgress(float progress);

    /// <summary>
    /// 홀드 완료
    /// </summary>
    void OnHoldComplete();

    /// <summary>
    /// 홀드 취소 (중간에 손 뗐을 때)
    /// </summary>
    void OnHoldCancelled();

    /// <summary>
    /// 홀드에 필요한 시간 (초)
    /// </summary>
    /// <returns>홀드 필요 시간, 0이면 홀드 불필요</returns>
    float GetHoldDuration();
}
