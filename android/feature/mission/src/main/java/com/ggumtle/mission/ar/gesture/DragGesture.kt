/**
 * 드래그 제스처 처리
 * - 단일 손가락 드래그 동작 감지 및 처리
 * - 시작 위치에서의 이동 거리 및 델타 계산
 * - SLOP 임계값을 이용한 제스처 시작 조건 검사
 */
package com.ggumtle.mission.ar.gesture

import android.view.MotionEvent
import com.ggumtle.mission.ar.util.V3
import com.ggumtle.mission.ar.util.eq
import com.ggumtle.mission.ar.util.magnitude
import com.ggumtle.mission.ar.util.sub
import com.ggumtle.mission.ar.util.v3Origin

class DragGesture(gesturePointersUtility: GesturePointersUtility, motionEvent: MotionEvent) :
    BaseGesture<DragGesture>(gesturePointersUtility) {
    interface OnGestureEventListener : BaseGesture.OnGestureEventListener<DragGesture>

    companion object {
        private const val SLOP_INCHES = 0.1f
    }

    private val pointerId: Int = motionEvent.getPointerId(motionEvent.actionIndex)

    private val startPosition: V3 =
        GesturePointersUtility.Companion.motionEventToPosition(motionEvent, pointerId)

    var position: V3 = startPosition
    private var delta: V3 = v3Origin

    override fun canStart(motionEvent: MotionEvent): Boolean {
        val actionId = motionEvent.getPointerId(motionEvent.actionIndex)
        val action = motionEvent.actionMasked

        if (gesturePointersUtility.isPointerIdRetained(pointerId)) {
            cancel()
            return false
        }

        if (actionId == pointerId
            && (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP)
        ) {
            cancel()
            return false
        } else if (action == MotionEvent.ACTION_CANCEL) {
            cancel()
            return false
        }

        if (action != MotionEvent.ACTION_MOVE) {
            return false
        }

        if (motionEvent.pointerCount > 1) {
            for (i in 0 until motionEvent.pointerCount) {
                val id = motionEvent.getPointerId(i)

                if (id != pointerId && !gesturePointersUtility.isPointerIdRetained(id)) {
                    return false
                }
            }
        }

        val newPosition: V3 =
            GesturePointersUtility.Companion.motionEventToPosition(motionEvent, pointerId)

        val diff: Float = newPosition.sub(startPosition).magnitude()
        val slopPixels = gesturePointersUtility.inchesToPixels(SLOP_INCHES)

        return diff >= slopPixels
    }

    override fun onStart(motionEvent: MotionEvent) {
        position = GesturePointersUtility.Companion.motionEventToPosition(motionEvent, pointerId)
        gesturePointersUtility.retainPointerId(pointerId)
    }

    override fun updateGesture(motionEvent: MotionEvent): Boolean {
        val actionId = motionEvent.getPointerId(motionEvent.actionIndex)
        val action = motionEvent.actionMasked
        if (action == MotionEvent.ACTION_MOVE) {
            val newPosition: V3 =
                GesturePointersUtility.Companion.motionEventToPosition(motionEvent, pointerId)

            if (newPosition.eq(position).not()) {
                delta = newPosition.sub(position)
                position = newPosition
                return true
            }
        } else if (actionId == pointerId
            && (action == MotionEvent.ACTION_UP || action == MotionEvent.ACTION_POINTER_UP)
        ) {
            complete()
        } else if (action == MotionEvent.ACTION_CANCEL) {
            cancel()
        }
        return false
    }

    override fun onCancel() {
    }

    override fun onFinish() {
        gesturePointersUtility.releasePointerId(pointerId)
    }

    override val self: DragGesture get() = this
}
