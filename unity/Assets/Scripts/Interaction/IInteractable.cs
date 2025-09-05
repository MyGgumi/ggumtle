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
}
