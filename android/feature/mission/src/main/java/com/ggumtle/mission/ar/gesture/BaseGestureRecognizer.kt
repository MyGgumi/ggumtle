/**
 * BaseGestureRecognizer.kt
 * 제스처 인식기의 기본 추상 클래스
 *
 * 제스처 인식기는 터치 입력을 처리하여 제스처가 시작되어야 하는지 판단하고
 * 제스처가 시작될 때 이벤트를 발생시킵니다.
 * 제스처가 완료/업데이트되는 시점을 확인하려면 제스처 객체의 이벤트를 수신하세요.
 */
package com.ggumtle.mission.ar.gesture

import android.view.MotionEvent

abstract class BaseGestureRecognizer<T : BaseGesture<T>>(val gesturePointersUtility: GesturePointersUtility) {
    interface OnGestureStartedListener<T : BaseGesture<T>> {
        fun onGestureStarted(gesture: T)
    }

    // 현재 처리 중인 제스처들의 목록
    val gestures = ArrayList<T>()
    // 제스처 시작 이벤트 리스너들
    private val gestureStartedListeners: ArrayList<OnGestureStartedListener<T>> = ArrayList()

    /**
     * 제스처 시작 이벤트 리스너를 추가합니다.
     * @param listener 추가할 리스너
     */
    fun addOnGestureStartedListener(listener: OnGestureStartedListener<T>) {
        if (!gestureStartedListeners.contains(listener)) {
            gestureStartedListeners.add(listener)
        }
    }

    /**
     * 제스처 시작 이벤트 리스너를 제거합니다.
     * @param listener 제거할 리스너
     */
    @Suppress("unused")
    fun removeOnGestureStartedListener(listener: OnGestureStartedListener<T>) {
        gestureStartedListeners.remove(listener)
    }

    fun onTouch(motionEvent: MotionEvent) {
        // 터치 입력을 바탕으로 제스처 인스턴스를 생성합니다.
        // 제스처가 생성되었다고 해서 바로 시작되는 것은 아닙니다.
        // 예를 들어, DragGesture는 사용자가 터치를 시작할 때 생성되지만
        // 실제로는 터치가 임계값을 넘어 이동해야 시작됩니다.
        tryCreateGestures(motionEvent)

        // 제스처들에게 이벤트를 전파하고 시작 여부를 판단합니다.
        for (gesture in gestures) {
            gesture.onTouch(motionEvent)

            if (gesture.justStarted()) {
                dispatchGestureStarted(gesture)
            }
        }

        removeFinishedGestures()
    }

    /**
     * 모션 이벤트를 바탕으로 제스처 생성을 시도하는 추상 메서드
     * @param motionEvent 처리할 모션 이벤트
     */
    abstract fun tryCreateGestures(motionEvent: MotionEvent)

    /**
     * 제스처 시작 이벤트를 모든 리스너에게 전달합니다.
     * @param gesture 시작된 제스처
     */
    private fun dispatchGestureStarted(gesture: T) {
        for (listener in gestureStartedListeners) {
            listener.onGestureStarted(gesture)
        }
    }

    /**
     * 완료된 제스처들을 목록에서 제거합니다.
     */
    private fun removeFinishedGestures() {
        gestures.removeIf { it.hasFinished() }
    }
}
