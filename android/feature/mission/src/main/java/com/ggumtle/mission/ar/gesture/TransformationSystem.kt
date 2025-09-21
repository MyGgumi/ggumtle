/**
 * 제스처 인식 시스템 통합 관리
 * - 드래그, 핀치, 회전 제스처 인식기 통합
 * - 터치 이벤트를 각 인식기에 배포
 * - 3D 모델 조작을 위한 제스처 처리
 */
package com.ggumtle.mission.ar.gesture

import android.util.DisplayMetrics
import android.view.MotionEvent

/**
 * 3D 모델 조작을 위한 제스처 인식 시스템 통합 관리 클래스
 * 여러 제스처 인식기를 통합하여 단일 인터페이스로 제공
 * @param displayMetrics 안드로이드 디스플레이 메트릭 - 화면 밀도 및 크기 정보
 */
class TransformationSystem(displayMetrics: DisplayMetrics) {
    // 제스처 포인터 관리 유틸리티 - 다중 터치 좌표 변환 및 관리
    // 화면 좌표를 3D 좌표로 변환, 터치 포인터 상태 추적 등의 기능 제공
    private val gesturePointersUtility: GesturePointersUtility =
        GesturePointersUtility(displayMetrics)

    // 등록된 모든 제스처 인식기들을 관리하는 리스트
    // 터치 이벤트 발생 시 모든 인식기에 동시에 전달되어 병렬 처리
    private val recognizers: MutableList<BaseGestureRecognizer<*>> = mutableListOf()

    // 드래그 제스처 인식기 - 단일 손가락 드래그 동작 감지
    // 모델의 위치 이동(이동 변환)에 사용되는 주요 제스처
    val dragRecognizer: DragGestureRecognizer = DragGestureRecognizer(gesturePointersUtility)
        .also { addGestureRecognizer(it) }  // 생성 즉시 인식기 리스트에 추가

    // 핀치 제스처 인식기 - 두 손가락 간의 거리 변화 감지
    // 모델의 크기 조절(스케일 변환)에 사용되는 제스처
    val pinchRecognizer: PinchGestureRecognizer = PinchGestureRecognizer(gesturePointersUtility)
        .also { addGestureRecognizer(it) }  // 생성 즉시 인식기 리스트에 추가

    // 트위스트 제스처 인식기 - 두 손가락 간의 회전 각도 변화 감지
    // 모델의 회전 조절(회전 변환)에 사용되는 제스처
    val twistRecognizer: TwistGestureRecognizer = TwistGestureRecognizer(gesturePointersUtility)
        .also { addGestureRecognizer(it) }  // 생성 즉시 인식기 리스트에 추가

    /**
     * 제스처 인식기를 시스템에 등록하는 내부 함수
     * 등록된 인식기는 onTouch() 호출 시 자동으로 터치 이벤트를 수신
     * @param gestureRecognizer 등록할 제스처 인식기 인스턴스
     */
    private fun addGestureRecognizer(gestureRecognizer: BaseGestureRecognizer<*>) {
        recognizers.add(gestureRecognizer)  // 인식기 리스트에 추가
    }

    /**
     * 터치 이벤트를 모든 등록된 제스처 인식기에 전달
     * 각 인식기는 독립적으로 자신의 제스처 패턴을 검사하고 처리
     * @param motionEvent 안드로이드 터치 이벤트 데이터
     */
    fun onTouch(motionEvent: MotionEvent) {
        // 등록된 모든 인식기에 동시에 이벤트 전달 - 병렬 처리
        for (recognizer in recognizers) {
            recognizer.onTouch(motionEvent)  // 각 인식기의 터치 이벤트 처리 메소드 호출
        }
    }
}
