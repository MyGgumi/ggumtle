/**
 * 기본 제스처 추상 클래스
 * - 모든 제스처의 공통 동작 및 상태 관리
 * - 제스처 시작, 업데이트, 종료 상태 처리
 * - 제스처 이벤트 리스너 인터페이스 제공
 */
package com.ggumtle.mission.ar.gesture

import android.view.MotionEvent

/**
 * 모든 제스처 클래스의 기본이 되는 추상 클래스
 * 제스처는 특정 탄 동작(드래그, 핀치, 트위스트 등)을 나타내는 터치 이벤트 시퀀스
 * @param T 자기 자신의 타입 (은 전성을 위한 제네릭 패턴)
 * @param gesturePointersUtility 터치 포인터 좌표 변환 및 관리를 위한 유틸리티
 */
abstract class BaseGesture<T : BaseGesture<T>>(val gesturePointersUtility: GesturePointersUtility) {
    /**
     * 제스처 이벤트 리스너 인터페이스
     * 제스처의 상태 변화를 외부에서 감지하고 대응할 수 있도록 콜백 제공
     */
    interface OnGestureEventListener<T : BaseGesture<T>> {
        /**
         * 제스처가 진행 중일 때 호출 (예: 드래그 중 손가락 이동)
         * @param gesture 업데이트된 제스처 인스턴스
         */
        fun onUpdated(gesture: T)

        /**
         * 제스처가 완료되거나 취소되었을 때 호출
         * @param gesture 종료된 제스처 인스턴스
         */
        fun onFinished(gesture: T)
    }

    // 제스처 상태 추적을 위한 내부 플래그들
    private var hasStarted = false     // 제스처가 시작되었는지 여부
    private var justStarted = false    // 방금 시작된 제스처인지 여부 (현재 프레임에서만 true)
    private var hasFinished = false    // 제스처가 종료되었는지 여부
    private var wasCancelled = false   // 제스처가 취소되었는지 여부

    // 제스처 이벤트를 수신할 리스너 - 외부 코드에서 제스처 상태 변화를 감지하기 위해 사용
    private var eventListener: OnGestureEventListener<T>? = null

    /**
     * 제스처가 시작되었는지 확인
     * @return 제스처 시작 여부
     */
    fun hasStarted(): Boolean {
        return hasStarted
    }

    /**
     * 제스처가 방금 시작되었는지 확인 (현재 프레임에서만)
     * @return 방금 시작된 제스처 여부
     */
    fun justStarted(): Boolean {
        return justStarted
    }

    /**
     * 제스처가 종료되었는지 확인
     * @return 제스처 종료 여부
     */
    fun hasFinished(): Boolean {
        return hasFinished
    }

    /**
     * 제스처가 취소되었는지 확인
     * @return 제스처 취소 여부
     */
    fun wasCancelled(): Boolean {
        return wasCancelled
    }

    /**
     * 인치 단위를 픽셀 단위로 변환
     * 화면 밀도에 따른 물리적 거리를 디지털 픽셀로 변환
     * @param inches 변환할 인치 값
     * @return 픽셀 단위로 변환된 값
     */
    fun inchesToPixels(inches: Float): Float {
        return gesturePointersUtility.inchesToPixels(inches)
    }

    /**
     * 픽셀 단위를 인치 단위로 변환
     * 디지털 픽셀을 물리적 거리(인치)로 변환하여 다양한 화면 크기에서 일관된 제스처 처리
     * @param pixels 변환할 픽셀 값
     * @return 인치 단위로 변환된 값
     */
    fun pixelsToInches(pixels: Float): Float {
        return gesturePointersUtility.pixelsToInches(pixels)
    }

    /**
     * 제스처 이벤트 리스너 등록
     * 제스처 상태 변화(업데이트, 종료)를 외부에서 감지하기 위해 사용
     * @param listener 등록할 이벤트 리스너
     */
    fun setGestureEventListener(listener: OnGestureEventListener<T>) {
        eventListener = listener
    }

    /**
     * 터치 이벤트 처리를 위한 메인 함수
     * 제스처 상태에 따라 시작, 업데이트, 종료 로직을 처리
     * @param motionEvent 안드로이드 터치 이벤트
     */
    fun onTouch(motionEvent: MotionEvent) {
        // 제스처가 아직 시작되지 않았고 시작 조건을 만족하는지 검사
        if (!hasStarted && canStart(motionEvent)) {
            start(motionEvent)  // 제스처 시작
            return
        }

        // 이전 프레임에서 시작된 제스처는 더 이상 "justStarted" 상태가 아님
        justStarted = false

        // 시작된 제스처에 대해 업데이트 처리
        if (hasStarted) {
            if (updateGesture(motionEvent)) {  // 제스처 데이터 업데이트
                dispatchUpdateEvent()  // 리스너에 업데이트 이벤트 전달
            }
        }
    }

    /**
     * 제스처 시작 가능 여부를 판단하는 추상 메소드
     * 각 제스처마다 고유한 시작 조건을 정의해야 함
     * @param motionEvent 현재 터치 이벤트
     * @return 제스처 시작 가능 여부
     */
    abstract fun canStart(motionEvent: MotionEvent): Boolean

    /**
     * 제스처 시작 시 호출되는 추상 메소드
     * 제스처별 초기화 로직을 구현해야 함
     * @param motionEvent 제스처 시작 시점의 터치 이벤트
     */
    abstract fun onStart(motionEvent: MotionEvent)

    /**
     * 제스처 업데이트 처리를 위한 추상 메소드
     * 터치 이벤트에 따라 제스처 데이터를 업데이트하고 진행 여부를 반환
     * @param motionEvent 현재 터치 이벤트
     * @return 제스처가 업데이트되었는지 여부
     */
    abstract fun updateGesture(motionEvent: MotionEvent): Boolean

    /**
     * 제스처 취소 시 호출되는 추상 메소드
     * 제스처별 취소 처리 로직을 구현해야 함
     */
    abstract fun onCancel()

    /**
     * 제스처 종료 시 호출되는 추상 메소드
     * 제스처별 정리 로직을 구현해야 함
     */
    abstract fun onFinish()

    /**
     * 제스처를 취소하는 공개 메소드
     * 외부에서 제스처를 강제로 취소하거나 비정상적인 상황에서 호출
     */
    fun cancel() {
        wasCancelled = true  // 취소 플래그 설정
        onCancel()           // 제스처별 취소 처리 실행
        complete()           // 제스처 종료 처리
    }

    /**
     * 제스처를 완료하는 공개 메소드
     * 제스처가 성공적으로 종료되거나 취소된 경우 호출
     */
    fun complete() {
        hasFinished = true  // 종료 플래그 설정

        // 시작된 제스처만 종료 처리 실행
        if (hasStarted) {
            onFinish()               // 제스처별 종료 처리 실행
            dispatchFinishedEvent()  // 리스너에 종료 이벤트 전달
        }
    }

    /**
     * 제스처 시작을 처리하는 내부 메소드
     * canStart()가 true를 반환할 때 onTouch()에서 호출됨
     * @param motionEvent 제스처 시작 시점의 터치 이벤트
     */
    private fun start(motionEvent: MotionEvent) {
        hasStarted = true       // 시작 플래그 설정
        justStarted = true      // 방금 시작된 플래그 설정 (한 프레임만 유지)
        onStart(motionEvent)    // 제스처별 시작 처리 실행
    }

    /**
     * 제스처 업데이트 이벤트를 리스너에 전달하는 내부 메소드
     * updateGesture()가 true를 반환할 때 호출됨
     */
    private fun dispatchUpdateEvent() {
        if (eventListener != null) {
            eventListener!!.onUpdated(self)  // 리스너에 업데이트 이벤트 전달
        }
    }

    /**
     * 제스처 종료 이벤트를 리스너에 전달하는 내부 메소드
     * complete() 메소드에서 호출됨
     */
    private fun dispatchFinishedEvent() {
        if (eventListener != null) {
            eventListener!!.onFinished(self)  // 리스너에 종료 이벤트 전달
        }
    }

    /**
     * 컴파일 시점 타입 안전성을 위한 자기 참조 프로퍼티
     * 이벤트 전달 시 타입 캐스팅 없이 안전하게 자기 자신을 반환
     * 예: DragGesture에서는 DragGesture 타입으로 자기 자신을 반환
     */
    abstract val self: T
}
