/**
 * ARCore 세션 관리 및 카메라 스트림 처리
 * - AR 세션 설정 (평면 감지, 조명, 포커스)
 * - 카메라 이미지 스트림 및 깊이 이미지 처리
 * - 디스플레이 회전 및 화면 비율 처리
 * - Filament 렌더러와 연동되는 렌더링 텍스처 생성
 */
package com.ggumtle.mission.ar.arcore

import android.annotation.SuppressLint
import android.app.Activity
import android.graphics.SurfaceTexture
import android.hardware.camera2.CameraCharacteristics
import android.hardware.camera2.CameraManager
import android.media.Image
import android.os.Build
import android.os.Handler
import android.util.DisplayMetrics
import android.view.Surface
import android.view.View
import android.view.ViewGroup
import androidx.core.content.ContextCompat
import com.ggumtle.mission.ar.util.M4
import com.ggumtle.mission.ar.util.V2A
import com.ggumtle.mission.ar.util.count
import com.ggumtle.mission.ar.util.createExternalTextureId
import com.ggumtle.mission.ar.util.dimenV2A
import com.ggumtle.mission.ar.filament.Filament
import com.ggumtle.mission.ar.util.m4Identity
import com.ggumtle.mission.ar.util.matrix
import com.ggumtle.mission.ar.util.projectionMatrix
import com.ggumtle.mission.ar.util.readUncompressedAsset
import com.ggumtle.mission.ar.util.rotate
import com.ggumtle.mission.ar.util.set
import com.ggumtle.mission.ar.util.toDoubleArray
import com.ggumtle.mission.ar.util.toFloatBuffer
import com.ggumtle.mission.ar.util.toShortBuffer
import com.ggumtle.mission.ar.util.translate
import com.google.android.filament.Entity
import com.google.android.filament.EntityManager
import com.google.android.filament.IndexBuffer
import com.google.android.filament.Material
import com.google.android.filament.MaterialInstance
import com.google.android.filament.RenderableManager
import com.google.android.filament.RenderableManager.PrimitiveType
import com.google.android.filament.Stream
import com.google.android.filament.Texture
import com.google.android.filament.TextureSampler
import com.google.android.filament.VertexBuffer
import com.google.android.filament.VertexBuffer.AttributeType
import com.google.android.filament.VertexBuffer.VertexAttribute
import com.google.ar.core.Config
import com.google.ar.core.Frame
import com.google.ar.core.Session
import kotlin.math.roundToInt

/**
 * 모델 버퍼 데이터 클래스
 * @param clipPosition 클립 좌표계의 정점 위치 배열
 * @param uvs 텍스처 좌표 배열 (UV 매핑용)
 * @param triangleIndices 삼각형 인덱스 배열
 */
class ModelBuffers(val clipPosition: V2A, val uvs: V2A, val triangleIndices: ShortArray)

/**
 * ARCore 세션 관리 메인 클래스
 * @param activity 현재 액티비티 컨텍스트
 * @param filament Filament 렌더링 엔진 인스턴스
 * @param view 렌더링이 표시될 뷰
 */
@SuppressLint("MissingPermission")
class ArCore(private val activity: Activity, val filament: Filament, private val view: View) {
    companion object {
        const val NEAR: Float = .1f // 카메라 near clipping plane (0.1미터)
        const val FAR: Float = 30f // 카메라 far clipping plane (30미터)
        private const val POSITION_BUFFER_INDEX: Int = 0 // 정점 위치 버퍼 인덱스
        private const val UV_BUFFER_INDEX: Int = 1 // UV 좌표 버퍼 인덱스
    }

    private val cameraStreamTextureId: Int = createExternalTextureId() // ARCore 카메라 스트림용 외부 텍스처 ID
    private lateinit var stream: Stream // Filament 카메라 스트림 객체
    private lateinit var depthMaterialInstance: MaterialInstance // 깊이 이미지 렌더링용 재질 인스턴스
    private lateinit var flatMaterialInstance: MaterialInstance // 평면 카메라 이미지 렌더링용 재질 인스턴스

    @Entity
    var depthRenderable: Int = 0 // 깊이 이미지 렌더링 가능한 엔티티 ID

    @Entity
    var flatRenderable: Int = 0 // 평면 카메라 이미지 렌더링 가능한 엔티티 ID

    val session: Session = Session(activity) // ARCore 세션 생성
        .also { session ->
            session.config // AR 세션 설정 객체
                .apply {
                    planeFindingMode = Config.PlaneFindingMode.HORIZONTAL_AND_VERTICAL // 수평/수직 평면 모두 감지
                    focusMode = Config.FocusMode.AUTO // 자동 포커스 모드

                    // 최신 Filament에서 깊이 정보 읽기 기능 문제
                    depthMode = // 깊이 감지 모드 설정
                        if (session.isDepthModeSupported(Config.DepthMode.AUTOMATIC)) Config.DepthMode.AUTOMATIC // 자동 깊이 감지 지원 시
                        else Config.DepthMode.DISABLED // 미지원 시 깊이 감지 비활성화

                    lightEstimationMode = Config.LightEstimationMode.ENVIRONMENTAL_HDR // HDR 환경 조명 추정 모드
                    // AR 프레임 획득은 블록되지 않고 마지막 프레임 제공
                    updateMode = Config.UpdateMode.LATEST_CAMERA_IMAGE // 최신 카메라 이미지로 업데이트
                }
                .let(session::configure) // 설정을 세션에 적용

            session.setCameraTextureName(cameraStreamTextureId) // 카메라 텍스처 이름 설정
        }

    private val cameraId: String = session.cameraConfig.cameraId // 현재 사용 중인 카메라 ID

    private val cameraManager: CameraManager = // 카메라 관리자 서비스
        ContextCompat.getSystemService(activity, CameraManager::class.java)!!

    var timestamp: Long = 0L // 현재 프레임 타임스탬프

    lateinit var frame: Frame // 현재 AR 프레임 데이터

    private lateinit var depthTexture: Texture // 깊이 이미지를 저장하는 텍스처

    /**
     * ARCore 세션 종료 및 리소스 정리
     */
    fun destroy() {
        session.close() // AR 세션 종료
    }

    private var displayRotationDegrees: Int = 0 // 현재 디스플레이 회전 각도 (도 단위)

    /**
     * 디스플레이 회전 및 화면 크기 변경 시 호출
     * 카메라와 디스플레이 간의 비율을 맞춰 뷰 크기 조정
     */
    fun configurationChange() {
        if (this::frame.isInitialized.not()) return // 프레임이 아직 초기화되지 않았다면 리턴

        val intrinsics = frame.camera.textureIntrinsics // 카메라 텍스처 내재 매개변수
        val dimensions = intrinsics.imageDimensions // 카메라 이미지 크기 (폭, 높이)

        val displayWidth: Int // 디스플레이 폭
        val displayHeight: Int // 디스플레이 높이
        val displayRotation: Int // 디스플레이 회전 상태

        DisplayMetrics() // 디스플레이 메트릭 객체 생성
            .also { displayMetrics ->
                @Suppress("DEPRECATION") // 하위 호환성을 위한 Deprecated API 사용
                (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) activity.display // API 30 이상
                else activity.windowManager.defaultDisplay)!! // API 30 미만
                    .also { display ->
                        display.getRealMetrics(displayMetrics) // 실제 디스플레이 크기 획득
                        displayRotation = display.rotation // 디스플레이 회전 상태 획득
                    }

                displayWidth = displayMetrics.widthPixels // 디스플레이 폭 (픽셀)
                displayHeight = displayMetrics.heightPixels // 디스플레이 높이 (픽셀)
            }

        displayRotationDegrees = // 회전 상수를 도 단위로 변환
            when (displayRotation) {
                Surface.ROTATION_0 -> 0 // 세로 모드 (정상)
                Surface.ROTATION_90 -> 90 // 90도 회전 (가로 모드)
                Surface.ROTATION_180 -> 180 // 180도 회전 (역세로)
                Surface.ROTATION_270 -> 270 // 270도 회전 (역가로)
                else -> throw Exception("Invalid Display Rotation") // 잘못된 회전 값
            }

        // 디스플레이 기준 카메라 폭과 높이
        val cameraWidth: Int // 카메라 이미지 폭
        val cameraHeight: Int // 카메라 이미지 높이

        when (cameraManager // 카메라 센서 방향에 따른 크기 계산
            .getCameraCharacteristics(cameraId) // 카메라 특성 정보 획득
            .get(CameraCharacteristics.SENSOR_ORIENTATION)!!) { // 센서 방향값 획득
            0, 180 -> when (displayRotation) { // 센서가 0도 또는 180도인 경우
                Surface.ROTATION_0, Surface.ROTATION_180 -> { // 디스플레이가 세로 모드인 경우
                    cameraWidth = dimensions[0] // 원본 폭 사용
                    cameraHeight = dimensions[1] // 원본 높이 사용
                }

                else -> { // 디스플레이가 가로 모드인 경우
                    cameraWidth = dimensions[1] // 폭과 높이 바꿈
                    cameraHeight = dimensions[0]
                }
            }

            else -> when (displayRotation) { // 센서가 90도 또는 270도인 경우
                Surface.ROTATION_0, Surface.ROTATION_180 -> { // 디스플레이가 세로 모드인 경우
                    cameraWidth = dimensions[1] // 폭과 높이 바꿴
                    cameraHeight = dimensions[0]
                }

                else -> { // 디스플레이가 가로 모드인 경우
                    cameraWidth = dimensions[0] // 원본 폭 사용
                    cameraHeight = dimensions[1] // 원본 높이 사용
                }
            }
        }

        val cameraRatio: Float = cameraWidth.toFloat() / cameraHeight.toFloat() // 카메라 이미지 비율 계산
        val displayRatio: Float = displayWidth.toFloat() / displayHeight.toFloat() // 디스플레이 비율 계산

        val viewWidth: Int // 최종 뷰 폭
        val viewHeight: Int // 최종 뷰 높이

        if (displayRatio < cameraRatio) { // 디스플레이가 카메라보다 세로로 긴 경우
            // 폭 제약 - 디스플레이 폭에 맞춰 조정
            viewWidth = displayWidth // 디스플레이 전체 폭 사용
            viewHeight = (displayWidth.toFloat() / cameraRatio).roundToInt() // 비율에 맞춰 높이 계산
        } else { // 디스플레이가 카메라보다 가로로 긴 경우
            // 높이 제약 - 디스플레이 높이에 맞춰 조정
            viewWidth = (displayHeight.toFloat() * cameraRatio).roundToInt() // 비율에 맞춰 폭 계산
            viewHeight = displayHeight // 디스플레이 전체 높이 사용
        }

        view.layoutParams = view.layoutParams?.apply { // 기존 레이아웃 파라미터가 있다면 수정
            width = viewWidth // 계산된 폭 적용
            height = viewHeight // 계산된 높이 적용
        } ?: ViewGroup.LayoutParams(viewWidth, viewHeight) // 없다면 새로 생성

        session.setDisplayGeometry(displayRotation, viewWidth, viewHeight) // ARCore에 디스플레이 정보 전달
    }

    private var hasDepthImage: Boolean = false // 깊이 이미지 사용 가능 여부 플래그

    /**
     * 매 프레임마다 호출되는 AR 데이터 업데이트 함수
     * @param frame ARCore에서 제공하는 현재 프레임 데이터
     * @param filament Filament 렌더링 엔진 인스턴스
     */
    fun update(frame: Frame, filament: Filament) {
        val firstFrame = this::frame.isInitialized.not() // 첫 번째 프레임인지 확인
        this.frame = frame // 현재 프레임 업데이트

        if (firstFrame) { // 첫 번째 프레임일 때 초기화 작업 수행
            configurationChange() // 디스플레이 설정 적용
            val camera = frame.camera // AR 카메라 객체
            val intrinsics = camera.textureIntrinsics // 카메라 내재 매개변수
            val dimensions = intrinsics.imageDimensions // 이미지 크기 배열
            val width = dimensions[0] // 이미지 폭
            val height = dimensions[1] // 이미지 높이

            stream = Stream // Filament 스트림 생성
                .Builder()
                .stream(SurfaceTexture(cameraStreamTextureId)) // ARCore 카메라 텍스처와 연결
                .width(width) // 스트림 폭 설정
                .height(height) // 스트림 높이 설정
                .build(filament.engine) // Filament 엔진으로 빌드

            flatMaterialInstance = activity // 평면 재질 인스턴스 생성
                .readUncompressedAsset("materials/flat.filamat") // flat 재질 파일 읽기
                .let { byteBuffer ->
                    Material // Filament 재질 빌더
                        .Builder()
                        .payload(byteBuffer, byteBuffer.remaining()) // 재질 데이터 설정
                }
                .build(filament.engine) // 엔진으로 재질 빌드
                .createInstance() // 재질 인스턴스 생성
                .also { materialInstance ->
                    materialInstance.setParameter( // 카메라 텍스처 파라미터 설정
                        "cameraTexture", // 파라미터 이름
                        Texture // 카메라 텍스처 생성
                            .Builder()
                            .sampler(Texture.Sampler.SAMPLER_EXTERNAL) // 외부 텍스처 샘플러
                            .importTexture(cameraStreamTextureId.toLong()) // ARCore 텍스처 ID 임포트
                            .build(filament.engine) // 텍스처 빌드
                            .apply { setExternalStream(filament.engine, stream) }, // 스트림 연결
                        TextureSampler( // 텍스처 샘플러 설정
                            TextureSampler.MinFilter.LINEAR, // 축소 시 선형 필터
                            TextureSampler.MagFilter.LINEAR, // 확대 시 선형 필터
                            TextureSampler.WrapMode.CLAMP_TO_EDGE, // 가장자리 클램프
                        )
                    )

                    materialInstance.setParameter( // UV 변환 행렬 파라미터 설정
                        "uvTransform", // 파라미터 이름
                        MaterialInstance.FloatElement.FLOAT4, // 4개 float 배열
                        m4Identity().floatArray, // 단위 행렬
                        0, // 오프셋
                        4, // 개수
                    )
                }

            initFlat() // 평면 렌더링 객체 초기화
        }

        // 최신 Filament에서 깊이 정보 읽기 기능 문제
        (if (session.isDepthModeSupported(Config.DepthMode.AUTOMATIC)) Unit else null) // 깊이 모드 지원 확인
            ?.let {
                if (hasDepthImage.not()) { // 깊이 이미지가 아직 처리되지 않은 경우
                    try {
                        val depthImage = frame.acquireDepthImage16Bits() // 16비트 깊이 이미지 획득

                        if (depthImage.planes[0].buffer[0] != 0.toByte()) { // 유효한 깊이 데이터가 있는지 확인
                            hasDepthImage = true // 깊이 이미지 사용 가능 플래그 설정

                            if (this::depthTexture.isInitialized.not()) { // 깊이 텍스처가 초기화되지 않은 경우
                                initDepthTextures(depthImage) // 깊이 텍스처 초기화
                            }

                            depthTexture.setImage( // 깊이 텍스처에 이미지 데이터 설정
                                filament.engine, // Filament 엔진
                                0, // 밉맵 레벨
                                Texture.PixelBufferDescriptor( // 픽셀 버퍼 설명자
                                    depthImage.planes[0].buffer, // 깊이 이미지 버퍼
                                    Texture.Format.RG, // RG 포맷 (2채널)
                                    Texture.Type.UBYTE, // 부호 없는 바이트
                                    1, // 바이트당 픽셀 수
                                    0, 0, 0, // 오프셋
                                    @Suppress("DEPRECATION")
                                    Handler(), // 완료 핸들러
                                ) {
                                    depthImage.close() // 깊이 이미지 리소스 해제
                                    hasDepthImage = false // 플래그 리셋
                                }
                            )

                            depthMaterialInstance.setParameter( // 깊이 재질 UV 변환 설정
                                "uvTransform", // 파라미터 이름
                                MaterialInstance.FloatElement.FLOAT4, // 타입
                                uvTransform().floatArray, // 변환 행렬
                                0, 4, // 오프셋, 개수
                            )

                            filament.scene.removeEntity(flatRenderable) // 평면 렌더링 제거
                            filament.scene.addEntity(depthRenderable) // 깊이 렌더링 추가
                        } else {
                            null // 유효하지 않은 깊이 데이터
                        }
                    } catch (error: Throwable) {
                        null // 깊이 이미지 처리 실패
                    }
                } else Unit
            }
            ?: run { // 깊이 모드 미지원 시 평면 카메라 이미지 사용
                flatMaterialInstance.setParameter( // 평면 재질 UV 변환 설정
                    "uvTransform", // 파라미터 이름
                    MaterialInstance.FloatElement.FLOAT4, // 타입
                    uvTransform().floatArray, // 변환 행렬
                    0, 4, // 오프셋, 개수
                )

                filament.scene.removeEntity(depthRenderable) // 깊이 렌더링 제거
                filament.scene.addEntity(flatRenderable) // 평면 렌더링 추가
            }

        // 카메라 투영 행렬 업데이트
        filament.camera.setCustomProjection( // 사용자 정의 투영 행렬 설정
            frame.projectionMatrix().floatArray.toDoubleArray(), // ARCore 투영 행렬을 double 배열로 변환
            NEAR.toDouble(), // near plane
            FAR.toDouble(), // far plane
        )

        val cameraTransform = frame.camera.displayOrientedPose.matrix() // 디스플레이 방향 고려한 카메라 변환 행렬
        filament.camera.setModelMatrix(cameraTransform.floatArray) // 카메라 모델 행렬 설정
        val instance = filament.engine.transformManager.create(depthRenderable) // 깊이 렌더링 객체의 변환 인스턴스 생성
        filament.engine.transformManager.setTransform(instance, cameraTransform.floatArray) // 변환 행렬 적용
    }

    /**
     * 평면 카메라 이미지 렌더링용 객체 초기화
     */
    private fun initFlat() {
        val tes = tessellation() // 테셀레이션 데이터 생성

        RenderableManager // 렌더링 매니저 빌더
            .Builder(1) // 1개의 프리미티브
            .castShadows(false) // 그림자 생성 안함
            .receiveShadows(false) // 그림자 받지 않음
            .culling(false) // 컬링 비활성화
            .geometry( // 지오메트리 설정
                0, // 인덱스
                PrimitiveType.TRIANGLES, // 삼각형 프리미티브
                VertexBuffer // 정점 버퍼 생성
                    .Builder()
                    .vertexCount(tes.clipPosition.count()) // 정점 개수
                    .bufferCount(2) // 2개 버퍼 (위치, UV)
                    .attribute( // 위치 속성
                        VertexAttribute.POSITION, // 위치 속성
                        POSITION_BUFFER_INDEX, // 버퍼 인덱스
                        AttributeType.FLOAT2, // 2D float
                        0, 0, // 오프셋, 스트라이드
                    )
                    .attribute( // UV 속성
                        VertexAttribute.UV0, // UV 속성
                        UV_BUFFER_INDEX, // 버퍼 인덱스
                        AttributeType.FLOAT2, // 2D float
                        0, 0, // 오프셋, 스트라이드
                    )
                    .build(filament.engine) // 정점 버퍼 빌드
                    .also { vertexBuffer ->
                        vertexBuffer.setBufferAt( // 위치 버퍼 데이터 설정
                            filament.engine, // 엔진
                            POSITION_BUFFER_INDEX, // 버퍼 인덱스
                            tes.clipPosition.floatArray.toFloatBuffer(), // 위치 데이터
                        )

                        vertexBuffer.setBufferAt( // UV 버퍼 데이터 설정
                            filament.engine, // 엔진
                            UV_BUFFER_INDEX, // 버퍼 인덱스
                            tes.uvs.floatArray.toFloatBuffer(), // UV 데이터
                        )
                    },
                IndexBuffer // 인덱스 버퍼 생성
                    .Builder()
                    .indexCount(tes.triangleIndices.size) // 인덱스 개수
                    .bufferType(IndexBuffer.Builder.IndexType.USHORT) // unsigned short 타입
                    .build(filament.engine) // 인덱스 버퍼 빌드
                    .apply { setBuffer(filament.engine, tes.triangleIndices.toShortBuffer()) }, // 인덱스 데이터 설정
            )
            .material(0, flatMaterialInstance) // 재질 설정
            .build(filament.engine, EntityManager.get().create().also { flatRenderable = it }) // 렌더링 객체 빌드
    }

    /**
     * 깊이 이미지 렌더링용 객체 초기화
     * @param depthImage 깊이 이미지 데이터
     */
    private fun initDepthTextures(depthImage: Image) {
        val tes = tessellation() // 테셀레이션 데이터 생성

        depthMaterialInstance = activity // 깊이 재질 인스턴스 생성
            .readUncompressedAsset("materials/depth.filamat") // 깊이 재질 파일 읽기
            .let { byteBuffer ->
                Material // 재질 빌더
                    .Builder()
                    .payload(byteBuffer, byteBuffer.remaining()) // 재질 데이터
            }
            .build(filament.engine) // 재질 빌드
            .createInstance() // 인스턴스 생성
            .also { materialInstance ->
                materialInstance.setParameter( // 깊이 텍스처 파라미터 설정
                    "depthTexture", // 파라미터 이름
                    Texture // 깊이 텍스처 생성
                        .Builder()
                        .width(depthImage.width) // 깊이 이미지 폭
                        .height(depthImage.height) // 깊이 이미지 높이
                        .sampler(Texture.Sampler.SAMPLER_2D) // 2D 샘플러
                        .format(Texture.InternalFormat.RG8) // RG8 포맷
                        .levels(1) // 밉맵 레벨 1개
                        .build(filament.engine) // 텍스처 빌드
                        .also { depthTexture = it }, // 깊이 텍스처 저장
                    TextureSampler(), // 기본 텍스처 샘플러
                )

                materialInstance.setParameter( // UV 변환 파라미터 설정
                    "uvTransform", // 파라미터 이름
                    MaterialInstance.FloatElement.FLOAT4, // 타입
                    m4Identity().floatArray, // 단위 행렬
                    0, 4, // 오프셋, 개수
                )
            }

        RenderableManager // 깊이 렌더링 매니저 빌더
            .Builder(1) // 1개 프리미티브
            .castShadows(false) // 그림자 생성 안함
            .receiveShadows(false) // 그림자 받지 않음
            .culling(false) // 컬링 비활성화
            .geometry( // 지오메트리 설정 (initFlat과 동일한 구조)
                0, PrimitiveType.TRIANGLES,
                VertexBuffer.Builder()
                    .vertexCount(tes.clipPosition.count())
                    .bufferCount(2)
                    .attribute(VertexAttribute.POSITION, POSITION_BUFFER_INDEX, AttributeType.FLOAT2, 0, 0)
                    .attribute(VertexAttribute.UV0, UV_BUFFER_INDEX, AttributeType.FLOAT2, 0, 0)
                    .build(filament.engine)
                    .also { vertexBuffer ->
                        vertexBuffer.setBufferAt(filament.engine, POSITION_BUFFER_INDEX, tes.clipPosition.floatArray.toFloatBuffer())
                        vertexBuffer.setBufferAt(filament.engine, UV_BUFFER_INDEX, tes.uvs.floatArray.toFloatBuffer())
                    },
                IndexBuffer.Builder()
                    .indexCount(tes.triangleIndices.size)
                    .bufferType(IndexBuffer.Builder.IndexType.USHORT)
                    .build(filament.engine)
                    .apply { setBuffer(filament.engine, tes.triangleIndices.toShortBuffer()) },
            )
            .material(0, depthMaterialInstance) // 깊이 재질 적용
            .build(filament.engine, EntityManager.get().create().also { depthRenderable = it }) // 깊이 렌더링 객체 빌드
    }

    /**
     * 카메라 이미지를 화면에 맞게 표시하기 위한 간단한 사각형 메시 생성
     * @return 테셀레이션된 모델 버퍼 데이터
     */
    @Suppress("KotlinConstantConditions")
    private fun tessellation(): ModelBuffers {
        val tesWidth = 1 // 테셀레이션 폭 (1x1 사각형)
        val tesHeight = 1 // 테셀레이션 높이

        val clipPosition = // 클립 좌표계 정점 배열 생성
            V2A(FloatArray((((tesWidth * tesHeight) + tesWidth + tesHeight + 1) * dimenV2A)))

        val uvs = // UV 좌표 배열 생성
            V2A(FloatArray((((tesWidth * tesHeight) + tesWidth + tesHeight + 1) * dimenV2A)))

        for (k in 0..tesHeight) { // 높이 방향 반복
            val v = k.toFloat() / tesHeight.toFloat() // V 좌표 (0~1)
            val y = (k.toFloat() / tesHeight.toFloat()) * 2f - 1f // Y 좌표 (-1~1)

            for (i in 0..tesWidth) { // 폭 방향 반복
                val u = i.toFloat() / tesWidth.toFloat() // U 좌표 (0~1)
                val x = (i.toFloat() / tesWidth.toFloat()) * 2f - 1f // X 좌표 (-1~1)
                clipPosition.set(k * (tesWidth + 1) + i, x, y) // 클립 좌표 설정
                uvs.set(k * (tesWidth + 1) + i, u, v) // UV 좌표 설정
            }
        }

        val triangleIndices = ShortArray(tesWidth * tesHeight * 6) // 삼각형 인덱스 배열 (사각형당 2개 삼각형, 삼각형당 3개 정점)

        for (k in 0 until tesHeight) { // 높이 방향 반복
            for (i in 0 until tesWidth) { // 폭 방향 반복
                // 첫 번째 삼각형 (좌상단-우상단-좌하단)
                triangleIndices[((k * tesWidth + i) * 6) + 0] = ((k * (tesWidth + 1)) + i + 0).toShort()
                triangleIndices[((k * tesWidth + i) * 6) + 1] = ((k * (tesWidth + 1)) + i + 1).toShort()
                triangleIndices[((k * tesWidth + i) * 6) + 2] = ((k + 1) * (tesWidth + 1) + i).toShort()

                // 두 번째 삼각형 (좌하단-우상단-우하단)
                triangleIndices[((k * tesWidth + i) * 6) + 3] = ((k + 1) * (tesWidth + 1) + i).toShort()
                triangleIndices[((k * tesWidth + i) * 6) + 4] = ((k * (tesWidth + 1)) + i + 1).toShort()
                triangleIndices[((k * tesWidth + i) * 6) + 5] = ((k + 1) * (tesWidth + 1) + i + 1).toShort()
            }
        }

        return ModelBuffers(clipPosition, uvs, triangleIndices) // 완성된 모델 버퍼 반환
    }

    /**
     * 카메라 이미지 회전을 보정하기 위한 UV 변환 행렬 생성
     * @return 4x4 변환 행렬
     */
    private fun uvTransform(): M4 = m4Identity() // 단위 행렬에서 시작
        .translate(.5f, .5f, 0f) // 중심점을 (0.5, 0.5)로 이동
        .rotate(imageRotation().toFloat(), 0f, 0f, -1f) // Z축 기준 회전
        .translate(-.5f, -.5f, 0f) // 중심점을 다시 (0, 0)으로 이동

    /**
     * 카메라 센서와 디스플레이 회전을 고려한 이미지 회전 각도 계산
     * @return 회전 각도 (도 단위)
     */
    private fun imageRotation(): Int = (cameraManager
        .getCameraCharacteristics(cameraId) // 카메라 특성 정보
        .get(CameraCharacteristics.SENSOR_ORIENTATION)!! + // 센서 방향
            when (displayRotationDegrees) { // 디스플레이 회전에 따른 보정값
                0 -> 90 // 세로 모드
                90 -> 0 // 가로 모드
                180 -> 270 // 역세로 모드
                270 -> 180 // 역가로 모드
                else -> throw Exception() // 잘못된 회전값
            } + 270) % 360 // 최종 보정 후 360도 범위로 정규화
}