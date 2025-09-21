/**
 * TwistGesture.kt
 * 두 손가락을 사용한 회전(비틀기) 제스처를 처리하는 클래스
 *
 * 이 클래스는 사용자가 두 손가락으로 화면에서 회전 동작을 할 때
 * 회전 각도를 계산하고 제스처 이벤트를 관리합니다.
 */
package com.ggumtle.mission.ar.gesture

import android.view.MotionEvent
import com.ggumtle.mission.ar.util.V3
import com.ggumtle.mission.ar.util.dot
import com.ggumtle.mission.ar.util.eq
import com.ggumtle.mission.ar.util.normalize
import com.ggumtle.mission.ar.util.sub
import com.ggumtle.mission.ar.util.toDegrees
import com.ggumtle.mission.ar.util.v3Origin
import com.ggumtle.mission.ar.util.x
import com.ggumtle.mission.ar.util.y
import kotlin.math.abs
import kotlin.math.acos
import kotlin.math.sign

class TwistGesture(
    gesturePointersUtility: GesturePointersUtility,
    motionEvent: MotionEvent,
    private val pointerId2: Int,
) : BaseGesture<TwistGesture>(gesturePointersUtility) {
    interface OnGestureEventListener : BaseGesture.OnGestureEventListener<TwistGesture>

    companion object {
        // 회전 제스처 인식을 위한 최소 회전 각도 임계값 (도 단위)
        private const val SLOP_ROTATION_DEGREES = 15.0f

        /**
         * 이전 위치와 현재 위치를 비교하여 회전 변화량을 계산합니다.
         * @param currentPosition1 첫 번째 손가락의 현재 위치
         * @param currentPosition2 두 번째 손가락의 현재 위치
         * @param previousPosition1 첫 번째 손가락의 이전 위치
         * @param previousPosition2 두 번째 손가락의 이전 위치
         * @return 회전 변화량 (도 단위)
         */
        private fun calculateDeltaRotation(
            currentPosition1: V3,
            currentPosition2: V3,
            previousPosition1: V3,
            previousPosition2: V3
        ): Float {
            val currentDirection: V3 = currentPosition1.sub(currentPosition2).normalize()
            val previousDirection: V3 = previousPosition1.sub(previousPosition2).normalize()

            return (acos(currentDirection.dot(previousDirection)).toDegrees) *
                    sign(previousDirection.x * currentDirection.y - previousDirection.y * currentDirection.x)
        }
    }

    // 첫 번째 손가락의 포인터 ID
    private val pointerId1: Int = motionEvent.getPointerId(motionEvent.actionIndex)

    // 첫 번째 손가락의 시작 위치
    private var startPosition1: V3 =
        GesturePointersUtility.motionEventToPosition(motionEvent, pointerId1)

    // 두 번째 손가락의 시작 위치
    private var startPosition2: V3 =
        GesturePointersUtility.motionEventToPosition(motionEvent, pointerId2)

    // 첫 번째 손가락의 이전 위치
    private var previousPosition1: V3 = startPosition1
    // 두 번째 손가락의 이전 위치
    private var previousPosition2: V3 = startPosition2

    // 회전 변화량 (도 단위)
    var deltaRotationDegrees = 0f
        private set

    override fun canStart(motionEvent: MotionEvent): Boolean {
        if (gesturePointersUtility.isPointerIdRetained(pointerId1)
            || gesturePointersUtility.isPointerIdRetained(pointerId2)
        ) {
            cancel()
            return false
        }

        val actionId = motionEvent.getPointerId(motionEvent.actionIndex)
        val action = motionEvent.actionMasked

        if (action == MotionEvent.ACTION_CANCEL) {
            cancel()
            return false
        }

        val touchEnded = action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP

        if (touchEnded && (actionId == pointerId1 || actionId == pointerId2)) {
            cancel()
            return false
        }

        if (action != MotionEvent.ACTION_MOVE) {
            return false
        }

        val newPosition1: V3 = GesturePointersUtility.motionEventToPosition(motionEvent, pointerId1)
        val newPosition2: V3 = GesturePointersUtility.motionEventToPosition(motionEvent, pointerId2)
        val deltaPosition1: V3 = newPosition1.sub(previousPosition1)
        val deltaPosition2: V3 = newPosition2.sub(previousPosition2)

        previousPosition1 = newPosition1
        previousPosition2 = newPosition2

        // 두 손가락이 모두 움직이고 있는지 확인
        if (deltaPosition1.eq(v3Origin)
            || deltaPosition2.eq(v3Origin)
        ) {
            return false
        }

        val rotation =
            calculateDeltaRotation(newPosition1, newPosition2, startPosition1, startPosition2)

        return abs(rotation) >= SLOP_ROTATION_DEGREES
    }

    override fun onStart(motionEvent: MotionEvent) {
        gesturePointersUtility.retainPointerId(pointerId1)
        gesturePointersUtility.retainPointerId(pointerId2)
    }

    override fun updateGesture(motionEvent: MotionEvent): Boolean {
        val actionId = motionEvent.getPointerId(motionEvent.actionIndex)
        val action = motionEvent.actionMasked

        if (action == MotionEvent.ACTION_CANCEL) {
            cancel()
            return false
        }

        val touchEnded = action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP

        if (touchEnded && (actionId == pointerId1 || actionId == pointerId2)) {
            complete()
            return false
        }

        if (action != MotionEvent.ACTION_MOVE) {
            return false
        }

        val newPosition1: V3 = GesturePointersUtility.motionEventToPosition(motionEvent, pointerId1)
        val newPosition2: V3 = GesturePointersUtility.motionEventToPosition(motionEvent, pointerId2)

        deltaRotationDegrees =
            calculateDeltaRotation(newPosition1, newPosition2, previousPosition1, previousPosition2)

        if (deltaRotationDegrees.isNaN()) {
            deltaRotationDegrees = 0f
        }

        previousPosition1 = newPosition1
        previousPosition2 = newPosition2

        return true
    }

    override fun onCancel() {
    }

    override fun onFinish() {
        gesturePointersUtility.releasePointerId(pointerId1)
        gesturePointersUtility.releasePointerId(pointerId2)
    }

    override val self: TwistGesture
        get() = this
}
