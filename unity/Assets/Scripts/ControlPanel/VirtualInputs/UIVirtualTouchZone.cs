using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIVirtualTouchZone
    : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler
{
    [System.Serializable]
    public class Event : UnityEvent<Vector2> { }

    [Header("Rect References")]
    public RectTransform containerRect;
    public RectTransform handleRect;

    [Header("Settings")]
    public bool clampToMagnitude;
    public float magnitudeMultiplier = 1f;
    public bool invertXOutputValue;
    public bool invertYOutputValue;

    // 터치 위치 저장
    private Vector2 pointerDownPosition;
    private Vector2 currentPointerPosition;

    [Header("Output")]
    public Event touchZoneOutputEvent;

    [Header("Touch Events")]
    public UnityEvent<Vector2> touchStartEvent;
    public UnityEvent touchEndEvent;

    void Start()
    {
        SetupHandle();
    }

    private void SetupHandle()
    {
        if (handleRect)
        {
            SetObjectActiveState(handleRect.gameObject, false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            containerRect,
            eventData.position,
            eventData.pressEventCamera,
            out pointerDownPosition
        );

        currentPointerPosition = pointerDownPosition;

        // 터치 시작 이벤트 발생 (절대 위치)
        touchStartEvent.Invoke(pointerDownPosition);

        if (handleRect)
        {
            SetObjectActiveState(handleRect.gameObject, true);
            UpdateHandleRectPosition(pointerDownPosition);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            containerRect,
            eventData.position,
            eventData.pressEventCamera,
            out currentPointerPosition
        );

        // 터치 시작점으로부터의 누적 이동량 계산 (절대 오프셋)
        Vector2 touchOffset = GetDeltaBetweenPositions(pointerDownPosition, currentPointerPosition);

        // 클램핑 적용 (선택사항)
        Vector2 processedOffset = clampToMagnitude
            ? ClampValuesToMagnitude(touchOffset)
            : touchOffset;

        // 반전 필터 적용
        Vector2 outputPosition = ApplyInversionFilter(processedOffset);

        // 연속 입력으로 전달 (누적 오프셋)
        OutputPointerEventValue(outputPosition * magnitudeMultiplier);

        // 핸들 위치 업데이트 (시각적 피드백)
        if (handleRect)
        {
            UpdateHandleRectPosition(processedOffset);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDownPosition = Vector2.zero;
        currentPointerPosition = Vector2.zero;

        // 0 입력으로 리셋
        OutputPointerEventValue(Vector2.zero);

        // 터치 종료 이벤트 발생
        touchEndEvent.Invoke();

        if (handleRect)
        {
            SetObjectActiveState(handleRect.gameObject, false);
            UpdateHandleRectPosition(Vector2.zero);
        }
    }

    void OutputPointerEventValue(Vector2 pointerPosition)
    {
        touchZoneOutputEvent.Invoke(pointerPosition);
    }

    void UpdateHandleRectPosition(Vector2 newPosition)
    {
        handleRect.anchoredPosition = newPosition;
    }

    void SetObjectActiveState(GameObject targetObject, bool newState)
    {
        targetObject.SetActive(newState);
    }

    Vector2 GetDeltaBetweenPositions(Vector2 firstPosition, Vector2 secondPosition)
    {
        return secondPosition - firstPosition;
    }

    Vector2 ClampValuesToMagnitude(Vector2 position)
    {
        return Vector2.ClampMagnitude(position, 1);
    }

    Vector2 ApplyInversionFilter(Vector2 position)
    {
        if (invertXOutputValue)
        {
            position.x = InvertValue(position.x);
        }

        if (invertYOutputValue)
        {
            position.y = InvertValue(position.y);
        }

        return position;
    }

    float InvertValue(float value)
    {
        return -value;
    }
}
