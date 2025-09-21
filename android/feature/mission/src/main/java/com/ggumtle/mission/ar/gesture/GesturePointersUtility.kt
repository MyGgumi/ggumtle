/**
 * GesturePointersUtility.kt
 * 제스처 포인터 관리 및 터치 좌표 변환 유틸리티 클래스
 *
 * 이 클래스는 다음과 같은 기능을 제공합니다:
 * - 포인터 ID를 유지/해제하여 각 포인터가 한 번에 하나의 제스처에서만 사용되도록 관리
 * - 터치 좌표를 픽셀과 인치 단위 간에 변환하는 헬퍼 함수 제공
 */
package com.ggumtle.mission.ar.gesture

import android.util.DisplayMetrics
import android.util.TypedValue
import android.view.MotionEvent
import com.ggumtle.mission.ar.util.V3
import com.ggumtle.mission.ar.util.v3
class GesturePointersUtility(private val displayMetrics: DisplayMetrics) {
    companion object {
        /**
         * MotionEvent에서 특정 포인터 ID의 위치를 3D 벡터로 변환합니다.
         * @param me MotionEvent 객체
         * @param pointerId 포인터 ID
         * @return 3D 위치 벡터 (z는 0으로 설정)
         */
        fun motionEventToPosition(me: MotionEvent, pointerId: Int): V3 {
            val index = me.findPointerIndex(pointerId)
            return v3(me.getX(index), me.getY(index), 0f)
        }
    }

    // 현재 사용 중인 포인터 ID들을 저장하는 집합
    private val retainedPointerIds: HashSet<Int> = HashSet()

    /**
     * 포인터 ID를 유지합니다 (다른 제스처에서 사용되지 않도록 예약)
     * @param pointerId 유지할 포인터 ID
     */
    fun retainPointerId(pointerId: Int) {
        if (!isPointerIdRetained(pointerId)) {
            retainedPointerIds.add(pointerId)
        }
    }

    /**
     * 포인터 ID를 해제합니다 (다른 제스처에서 사용 가능하도록 함)
     * @param pointerId 해제할 포인터 ID
     */
    fun releasePointerId(pointerId: Int) {
        retainedPointerIds.remove(Integer.valueOf(pointerId))
    }

    /**
     * 포인터 ID가 현재 유지(사용 중)되고 있는지 확인합니다.
     * @param pointerId 확인할 포인터 ID
     * @return 유지되고 있으면 true, 아니면 false
     */
    fun isPointerIdRetained(pointerId: Int): Boolean {
        return retainedPointerIds.contains(pointerId)
    }

    /**
     * 인치 단위를 픽셀 단위로 변환합니다.
     * @param inches 인치 값
     * @return 픽셀 값
     */
    fun inchesToPixels(inches: Float): Float {
        return TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_IN, inches, displayMetrics)
    }

    /**
     * 픽셀 단위를 인치 단위로 변환합니다.
     * @param pixels 픽셀 값
     * @return 인치 값
     */
    fun pixelsToInches(pixels: Float): Float {
        val inchOfPixels = TypedValue.applyDimension(TypedValue.COMPLEX_UNIT_IN, 1f, displayMetrics)
        return pixels / inchOfPixels
    }
}
