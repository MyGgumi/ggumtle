/**
 * ArModels.kt
 * AR 관련 데이터 모델 및 유틸리티 함수들을 정의하는 파일
 *
 * 이 파일은 다음과 같은 요소들을 포함합니다:
 * - 화면 좌표 및 뷰 영역을 나타내는 데이터 클래스
 * - 터치 이벤트를 처리하기 위한 sealed class
 * - View 확장 함수
 */
package com.ggumtle.mission.ar.util

import android.view.View

/**
 * 화면상의 위치를 나타내는 데이터 클래스
 * @param x X 좌표
 * @param y Y 좌표
 */
data class ScreenPosition(val x: Float, val y: Float)

/**
 * 뷰의 영역 정보를 나타내는 데이터 클래스
 * @param left 왼쪽 위치
 * @param top 위쪽 위치
 * @param width 너비
 * @param height 높이
 */
data class ViewRect(val left: Float, val top: Float, val width: Float, val height: Float)

/**
 * 터치 이벤트를 나타내는 sealed class
 * @param x X 좌표
 * @param y Y 좌표
 */
sealed class TouchEvent(val x: Float, val y: Float) {
    /**
     * 터치 이동 이벤트
     */
    class Move(x: Float, y: Float) : TouchEvent(x, y)

    /**
     * 터치 종료 이벤트
     */
    class Stop(x: Float, y: Float) : TouchEvent(x, y)
}

/**
 * View를 ViewRect로 변환하는 확장 함수
 * @return View의 위치와 크기 정보를 담은 ViewRect 객체
 */
fun View.toViewRect(): ViewRect =
    ViewRect(
        left.toFloat(),
        top.toFloat(),
        width.toFloat(),
        height.toFloat(),
    )
