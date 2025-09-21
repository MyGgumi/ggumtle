/**
 * DragGestureRecognizer.kt
 * 드래그(끌기) 제스처를 인식하는 클래스
 *
 * 이 클래스는 사용자의 터치 입력을 분석하여 드래그 제스처를 감지하고
 * 해당 제스처 객체를 생성합니다.
 */
package com.ggumtle.mission.ar.gesture

import android.view.MotionEvent

class DragGestureRecognizer(gesturePointersUtility: GesturePointersUtility) :
    BaseGestureRecognizer<DragGesture>(gesturePointersUtility) {
    interface OnGestureStartedListener : BaseGestureRecognizer.OnGestureStartedListener<DragGesture>

    /**
     * 모션 이벤트를 분석하여 드래그 제스처를 생성을 시도합니다.
     * @param motionEvent 분석할 모션 이벤트
     */
    override fun tryCreateGestures(motionEvent: MotionEvent) {
        val action = motionEvent.actionMasked
        val actionId = motionEvent.getPointerId(motionEvent.actionIndex)

        // 터치가 시작되었는지 확인
        val touchBegan = action == MotionEvent.ACTION_DOWN ||
                action == MotionEvent.ACTION_POINTER_DOWN

        // 터치가 시작되었고 해당 포인터가 다른 제스처에서 사용되지 않는 경우 드래그 제스처 생성
        if (touchBegan && !gesturePointersUtility.isPointerIdRetained(actionId)) {
            gestures.add(DragGesture(gesturePointersUtility, motionEvent))
        }
    }
}
