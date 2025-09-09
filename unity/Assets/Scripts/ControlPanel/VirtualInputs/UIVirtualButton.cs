using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class UIVirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
{
    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }
    [System.Serializable]
    public class Event : UnityEvent { }

    [Header("Output")]
    public BoolEvent buttonStateOutputEvent;
    public Event buttonClickOutputEvent;
    
    [Header("Hold Events")]
    public Event buttonHoldStartEvent; // 홀드 시작 이벤트
    public Event buttonHoldEndEvent;   // 홀드 종료 이벤트
    
    private bool isHolding = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        OutputButtonStateValue(true);
        StartHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OutputButtonStateValue(false);
        EndHold();
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // 포인터가 버튼 영역을 벗어나면 홀드 종료
        OutputButtonStateValue(false);
        EndHold();
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        OutputButtonClickEvent();
    }

    void OutputButtonStateValue(bool buttonState)
    {
        buttonStateOutputEvent.Invoke(buttonState);
    }

    void OutputButtonClickEvent()
    {
        buttonClickOutputEvent.Invoke();
    }
    
    void StartHold()
    {
        if (!isHolding)
        {
            isHolding = true;
            buttonHoldStartEvent.Invoke();
            Debug.Log("[UIVirtualButton] 홀드 시작");
        }
    }
    
    void EndHold()
    {
        if (isHolding)
        {
            isHolding = false;
            buttonHoldEndEvent.Invoke();
            Debug.Log("[UIVirtualButton] 홀드 종료");
        }
    }
    
    /// <summary>
    /// 현재 홀드 중인지 확인
    /// </summary>
    public bool IsHolding()
    {
        return isHolding;
    }

}
