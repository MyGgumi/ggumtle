/**
 * 3D 모델 렌더링 및 조작 처리
 * - GLB 모델 로드 및 애니메이션 재생
 * - 터치/제스처에 따른 모델 이동, 회전, 크기 조절
 * - AR 평면에 모델 배치 및 hit test 처리
 * - 프레임별 렌더링 업데이트
 */
package com.ggumtle.mission.ar.renderer

import android.content.Context
import com.ggumtle.mission.ar.util.V3
import com.ggumtle.mission.ar.util.ScreenPosition
import com.ggumtle.mission.ar.arcore.ArCore
import com.ggumtle.mission.ar.util.clampToTau
import com.ggumtle.mission.ar.filament.Filament
import com.ggumtle.mission.ar.util.m4Identity
import com.ggumtle.mission.ar.util.rotate
import com.ggumtle.mission.ar.util.scale
import com.ggumtle.mission.ar.util.toDegrees
import com.ggumtle.mission.ar.util.translate
import com.ggumtle.mission.ar.util.v3Origin
import com.ggumtle.mission.ar.util.x
import com.ggumtle.mission.ar.util.y
import com.ggumtle.mission.ar.util.z
import com.google.ar.core.Frame
import com.google.ar.core.Point
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.cancel
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.filterNotNull
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.mapNotNull
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.nio.ByteBuffer
import java.util.concurrent.TimeUnit

/**
 * 3D 모델 렌더링 및 조작을 담당하는 메인 클래스
 * @param context 안드로이드 애플리케이션 컨텍스트 - assets에서 GLB 파일 로드에 필요
 * @param arCore AR 세션 관리 객체 - 평면 감지 및 hit test 수행
 * @param filament 3D 렌더링 엔진 - 실제 모델 렌더링 담당
 */
class ModelRenderer(context: Context, private val arCore: ArCore, private val filament: Filament) {
    /**
     * 모델 조작 이벤트를 정의하는 sealed class
     * UI에서 발생하는 터치/제스처 이벤트를 모델 변환으로 전달하는 역할
     */
    sealed class ModelEvent {
        /**
         * 모델 위치 이동 이벤트
         * @param screenPosition 화면 좌표계에서의 터치 위치 (0.0~1.0 정규화된 좌표)
         */
        data class Move(val screenPosition: ScreenPosition) : ModelEvent()

        /**
         * 모델 회전 및 크기 조절 이벤트
         * @param rotate 회전 각도 변화량 (라디안 단위)
         * @param scale 크기 변화 배율 (1.0 = 동일, 2.0 = 2배 확대)
         */
        data class Update(val rotate: Float, val scale: Float) : ModelEvent()
    }

    // 모델 조작 이벤트를 전달하는 SharedFlow - UI 컴포넌트에서 이벤트를 방출
    // extraBufferCapacity = 1: 1개의 이벤트를 버퍼에 저장 가능
    // DROP_OLDEST: 버퍼가 가득 찰 때 가장 오래된 이벤트를 삭제하고 새 이벤트 추가
    val modelEvents: MutableSharedFlow<ModelEvent> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    // AR 프레임 업데이트 이벤트를 전달하는 SharedFlow - 매 프레임마다 호출됨
    // ArActivity에서 doFrame() 호출 시 이 Flow로 Frame 객체가 전달됨
    private val doFrameEvents: MutableSharedFlow<Frame> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    // 모델이 화면에 그려질 수 있는 상태인지를 나타내는 StateFlow
    // Unit? 타입을 사용하여 null = 그리기 불가, Unit = 그리기 가능 상태를 표현
    // 모델이 AR 평면에 배치된 후에만 렌더링이 시작됨
    private val canDrawBehavior: MutableStateFlow<Unit?> =
        MutableStateFlow(null)

    // 모델의 3D 변환 상태를 저장하는 변수들
    private var translation: V3 = v3Origin  // 모델의 3D 세계 좌표 (x, y, z) - AR 평면 상의 위치
    private var rotate: Float = 0f          // Y축 기준 회전 각도 (라디안) - 모델의 방향
    private var scale: Float = 1f           // 크기 배율 - 원본 모델 대비 확대/축소 비율

    // 비동기 작업을 위한 코루틴 스코프 - Main 디스패처 사용으로 UI 스레드에서 실행
    // 모델 로딩, 이벤트 처리, 렌더링 업데이트 등의 비동기 작업을 관리
    private val coroutineScope: CoroutineScope =
        CoroutineScope(Dispatchers.Main)

    /**
     * ModelRenderer 초기화 블록
     * - GLB 모델 파일 로드 및 Filament Asset 생성
     * - 모델 조작 이벤트 처리 코루틴 시작
     * - 애니메이션 및 렌더링 루프 설정
     */
    init {
        // 메인 초기화 코루틴 시작 - 모든 비동기 작업들을 병렬로 실행
        coroutineScope.launch {
            // GLB 3D 모델 파일을 비동기로 로드하여 Filament Asset으로 변환
            // IO 디스패처 사용으로 파일 I/O가 메인 스레드를 블록하지 않도록 함
            val filamentAsset =
                withContext(Dispatchers.IO) {
                    context.assets
                        .open("mongging-ar.glb")  // assets 폴더에서 GLB 파일 열기
                        .use { input ->  // use 구문으로 자동 리소스 해제 보장
                            val bytes = ByteArray(input.available())  // 파일 크기만큼 바이트 배열 생성
                            input.read(bytes)  // 파일 내용을 바이트 배열로 읽기
                            // AssetLoader를 통해 GLB 바이너리를 Filament Asset으로 변환
                            filament.assetLoader.createAsset(ByteBuffer.wrap(bytes))!!
                        }
                }
                    .also { asset ->
                        // 텍스처, 머티리얼 등 추가 리소스를 GPU 메모리로 로드
                        // GLB 파일에 포함된 모든 리소스를 Filament 엔진에서 사용 가능하도록 준비
                        filament.resourceLoader.loadResources(asset)

                        // 재질 설정 시도 (API 호환성 문제로 주석 처리)
                        // TODO: 재질 색상 문제는 GLB 파일 자체를 수정하여 해결
                    }

            // 모델 위치 이동 처리 코루틴 - 터치 이벤트를 3D 좌표로 변환
            launch {
                // Move 이벤트만 필터링하여 AR 평면과의 교차점 계산
                modelEvents
                    .mapNotNull { modelEvent ->
                        // Move 이벤트인 경우에만 처리
                        (modelEvent as? ModelEvent.Move)
                            ?.let {
                                // AR 프레임에서 화면 좌표를 3D 공간 좌표로 변환 (Ray Casting)
                                arCore.frame
                                    .hitTest(
                                        // 정규화된 화면 좌표를 픽셀 좌표로 변환
                                        filament.surfaceView.width.toFloat() * modelEvent.screenPosition.x,
                                        filament.surfaceView.height.toFloat() * modelEvent.screenPosition.y,
                                    )
                                    // Hit Test 결과 중 Point(특징점) 기반 결과를 우선 선택
                                    // Point는 평면보다 더 정확한 위치 정보를 제공
                                    .maxByOrNull { it.trackable is Point }
                            }
                            // Hit Pose의 translation을 V3 좌표로 변환
                            ?.let { V3(it.hitPose.translation) }
                    }
                    .collect {
                        // 모델이 배치되었으므로 렌더링 시작 신호 발송
                        canDrawBehavior.tryEmit(Unit)
                        // 모델의 3D 위치 업데이트
                        translation = it
                    }
            }

            // 모델 회전 및 크기 조절 처리 코루틴 - 제스처 입력을 모델 변환에 적용
            launch {
                // 모든 모델 이벤트를 수집하여 변환 상태 업데이트
                modelEvents.collect { modelEvent ->
                    // 이벤트 타입에 따라 회전/크기 값 계산
                    when (modelEvent) {
                        is ModelEvent.Update ->
                            // Update 이벤트: 기존 값에 델타를 누적 적용
                            Pair(
                                (rotate + modelEvent.rotate).clampToTau,  // 회전값을 0~2π 범위로 제한
                                scale * modelEvent.scale  // 크기는 곱셈으로 누적 (상대적 변화)
                            )
                        else ->
                            // 다른 이벤트: 현재 값 유지
                            Pair(rotate, scale)
                    }
                        .let { (r, s) ->
                            // 계산된 변환 값을 멤버 변수에 저장
                            rotate = r
                            scale = s
                        }
                }
            }

            // 메인 렌더링 루프 코루틴 - 매 프레임마다 모델 업데이트 및 렌더링 수행
            launch {
                // 모델이 배치될 때까지 대기 - canDrawBehavior에서 Unit 값이 방출되면 시작
                canDrawBehavior.filterNotNull().first()

                // AR 프레임 마다 모델 업데이트 및 렌더링 수행
                doFrameEvents.collect { frame ->
                    // 모델 애니메이션 업데이트 - GLB 파일에 포함된 애니메이션 재생
                    val animator = filamentAsset.instance.animator

                    // 애니메이션이 있는 경우에만 업데이트 수행
                    if (animator.animationCount > 0) {
                        // 시간 기반 애니메이션 진행 - 프레임 타임스탬프를 사용
                        animator.applyAnimation(
                            0,  // 첫 번째 애니메이션 인덱스
                            // 타임스탬프를 초 단위로 변환하고 애니메이션 길이로 나눈 나머지로 루프
                            (frame.timestamp /
                                    TimeUnit.SECONDS.toNanos(1).toDouble())  // 나노초를 초로 변환
                                .toFloat() %
                                    animator.getAnimationDuration(0),  // 애니메이션 길이로 나눉 나머지로 루프 효과
                        )

                        // 애니메이션 적용 후 본 매트릭스 업데이트 - 스켈레탈 애니메이션 처리
                        animator.updateBoneMatrices()
                    }

                    // 모델의 모든 엔티티를 씨에 추가 - 현재 프레임에서 렌더링될 수 있도록 등록
                    filament.scene.addEntities(filamentAsset.entities)

                    // 모델 루트 노드에 변환 매트릭스 적용 - 이동, 회전, 크기 조절 반영
                    filament.engine.transformManager.setTransform(
                        // 모델 루트 엔티티에 대한 Transform 인스턴스 획득
                        filament.engine.transformManager.getInstance(filamentAsset.root),
                        // 4x4 변환 매트릭스 생성 및 순차적 변환 적용 (TRS 순서: Translation → Rotation → Scale)
                        m4Identity()  // 단위 매트릭스에서 시작
                            .translate(translation.x, translation.y, translation.z)  // 3D 위치 이동 변환
                            .rotate(rotate.toDegrees, 0f, 1f, 0f)  // Y축 기준 회전 (라디안을 도로 변환)
                            .scale(scale * 0.1f, scale * 0.1f, scale * 0.1f)  // 균등 크기 조절 (0.1배로 축소하여 적절한 크기 유지)
                            .floatArray,  // Filament에서 요구하는 float 배열 형식으로 변환
                    )
                }
            }
        }
    }

    /**
     * ModelRenderer 리소스 정리 및 종료 처리
     * 메모리 누수 방지를 위해 반드시 호출되어야 하는 함수
     */
    fun destroy() {
        // 모든 코루틴 작업 취소 및 스코프 종료
        // 비동기 작업 중단으로 시스템 리소스 해제
        coroutineScope.cancel()
    }

    /**
     * AR 프레임 업데이트 함수 - ArActivity에서 매 프레임마다 호출
     * @param frame ARCore에서 제공하는 현재 프레임 데이터 (카메라 이미지, 자세, 추적 정보 포함)
     */
    fun doFrame(frame: Frame) {
        // 프레임 데이터를 렌더링 루프에 전달
        // tryEmit 사용으로 비블로킹 방식으로 이벤트 발송 (성능 최적화)
        doFrameEvents.tryEmit(frame)
    }
}
