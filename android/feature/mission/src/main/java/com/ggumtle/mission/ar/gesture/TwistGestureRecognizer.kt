/**
 * TwistGestureRecognizer.kt
 * 회전(비틀기) 제스처를 인식하는 클래스
 *
 * 이 클래스는 두 손가락을 사용한 회전 제스처를 감지하고
 * 해당 제스처 객체를 생성합니다.
 */
package com.ggumtle.mission.ar.gesture

import android.view.MotionEvent

class TwistGestureRecognizer(gesturePointersUtility: GesturePointersUtility) :
    BaseGestureRecognizer<TwistGesture>(gesturePointersUtility) {
    interface OnGestureStartedListener :
        BaseGestureRecognizer.OnGestureStartedListener<TwistGesture>

    /**
     * 모션 이벤트를 분석하여 회전 제스처 생성을 시도합니다.
     * @param motionEvent 분석할 모션 이벤트
     */
    override fun tryCreateGestures(motionEvent: MotionEvent) {
        // 두 개 미만의 포인터가 있으면 회전 제스처 불가능
        if (motionEvent.pointerCount < 2) {
            return
        }

        val actionId = motionEvent.getPointerId(motionEvent.actionIndex)
        val action = motionEvent.actionMasked

        // 터치가 시작되었는지 확인
        val touchBegan = action == MotionEvent.ACTION_DOWN ||
                action == MotionEvent.ACTION_POINTER_DOWN

        if (!touchBegan || gesturePointersUtility.isPointerIdRetained(actionId)) {
            return
        }

        // 아직 유지되지 않은 다른 포인터 ID가 있는지 확인
        for (i in 0 until motionEvent.pointerCount) {
            val pointerId = motionEvent.getPointerId(i)

            if (pointerId == actionId) {
                continue
            }

            if (gesturePointersUtility.isPointerIdRetained(pointerId)) {
                continue
            }

            // 사용 가능한 두 번째 포인터를 찾으면 회전 제스처 생성
            gestures.add(TwistGesture(gesturePointersUtility, motionEvent, pointerId))
        }
    }
}
