/**
 * Filament 3D 렌더링 엔진 초기화 및 관리
 * - OpenGL ES 컨텍스트 생성 및 엔진 설정
 * - 카메라, 씨, 렌더러 초기화
 * - GLB 3D 모델 로더 설정
 * - Surface 생명주기 관리
 */
package com.ggumtle.mission.ar.filament

import android.content.Context
import android.opengl.EGLContext
import android.view.Surface
import android.view.SurfaceView
import com.ggumtle.mission.ar.util.createEglContext
import com.ggumtle.mission.ar.util.destroyEglContext
import com.google.android.filament.Camera
import com.google.android.filament.Engine
import com.google.android.filament.EntityManager
import com.google.android.filament.Renderer
import com.google.android.filament.Scene
import com.google.android.filament.SwapChain
import com.google.android.filament.View
import com.google.android.filament.Viewport
import com.google.android.filament.android.DisplayHelper
import com.google.android.filament.android.UiHelper
import com.google.android.filament.gltfio.AssetLoader
import com.google.android.filament.gltfio.ResourceLoader
import com.google.android.filament.gltfio.UbershaderProvider

/**
 * Filament 3D 렌더링 엔진을 관리하는 메인 클래스
 * @param context 안드로이드 애플리케이션 컨텍스트 - 시스템 리소스 접근에 필요
 * @param surfaceView 3D 렌더링이 이루어질 Surface를 담고 있는 뷰
 */
class Filament(context: Context, val surfaceView: SurfaceView) {
    // 렌더링 프레임 타이밍을 측정하기 위한 타임스탬프 (나노초 단위)
    var timestamp: Long = 0L

    // OpenGL ES 컨텍스트 - GPU와 통신하기 위한 OpenGL 상태 정보를 담고 있음
    // EGL(Embedded-System Graphics Library)을 통해 생성된 그래픽 컨텍스트
    private val eglContext: EGLContext = createEglContext().orNull()!!

    // Filament 렌더링 엔진 인스턴스 - 모든 3D 렌더링의 중심이 되는 객체
    // 씬 그래프, 렌더링 파이프라인, 리소스 관리를 담당
    val engine: Engine = Engine.create(eglContext)

    // 렌더러 객체 - 실제 렌더링 작업을 수행하는 컴포넌트
    // 씬의 객체들을 화면에 그리는 역할을 담당
    val renderer: Renderer = engine.createRenderer()

    // 씬 객체 - 3D 공간에 배치될 모든 객체들(라이트, 메시, 카메라 등)을 관리
    // 렌더링할 3D 세계의 컨테이너 역할
    val scene: Scene = engine.createScene()

    // 카메라 객체 - 3D 씬을 바라보는 시점을 정의하는 가상 카메라
    // EntityManager를 통해 고유한 엔티티 ID를 생성하여 카메라를 생성
    val camera: Camera = engine
        .createCamera(engine.entityManager.create()) // 새로운 엔티티 생성 후 카메라 컴포넌트 추가
        .also { camera ->
            // 카메라 노출 설정 (sunny f/16 규칙 따름)
            // 태양과 같은 강도의 조명을 정의하여 적절한 노출 보장
            // 매개변수: aperture(조리개값), shutterSpeed(셔터속도), sensitivity(ISO 감도)
            camera.setExposure(16f, 1f / 125f, 100f)
        }

    // 뷰 객체 - 렌더링할 씬과 카메라를 연결하는 뷰포트
    // 특정 카메라로 특정 씬을 렌더링하는 설정을 담고 있음
    val view: View = engine
        .createView() // 새로운 뷰 인스턴스 생성
        .also { view ->
            view.camera = camera // 이 뷰에서 사용할 카메라 설정
            view.scene = scene   // 이 뷰에서 렌더링할 씬 설정
        }

    // 3D 모델 자산 로더 - GLB/GLTF 파일을 Filament 엔티티로 변환하는 역할
    // UbershaderProvider: 다양한 머티리얼을 처리할 수 있는 범용 셰이더 제공자
    // EntityManager: 씬의 모든 오브젝트들을 고유 ID로 관리하는 엔티티 시스템
    val assetLoader =
        AssetLoader(engine, UbershaderProvider(engine), EntityManager.get())

    // 리소스 로더 - 텍스처, 머티리얼 등의 추가 리소스를 로드하는 역할
    // AssetLoader와 함께 사용되어 완전한 3D 모델을 구성
    val resourceLoader =
        ResourceLoader(engine)

    // 스왑 체인 - 더블 버퍼링을 통해 화면 깜박임 없는 렌더링 구현
    // Surface가 변경될 때마다 새로 생성되므로 nullable로 선언
    // 백 버퍼와 프론트 버퍼를 교체하여 부드러운 렌더링 제공
    var swapChain: SwapChain? = null

    // 디스플레이 헬퍼 - 안드로이드 디스플레이와 Filament 렌더러 간의 동기화 관리
    // 화면 주사율, 해상도 변경 등의 디스플레이 이벤트를 처리
    val displayHelper = DisplayHelper(context)

    // UI 헬퍼 - SurfaceView의 생명주기와 Filament 렌더링을 연결하는 헬퍼 클래스
    // ContextErrorPolicy.DONT_CHECK: OpenGL 컨텍스트 에러 체크를 비활성화하여 성능 향상
    val uiHelper = UiHelper(UiHelper.ContextErrorPolicy.DONT_CHECK).apply {
        // 렌더링 콜백 설정 - Surface 변경 사항에 따른 렌더링 파이프라인 재구성
        renderCallback = object : UiHelper.RendererCallback {
            /**
             * 네이티브 윈도우(Surface)가 변경될 때 호출되는 콜백
             * @param surface 새로운 렌더링 대상 Surface
             */
            override fun onNativeWindowChanged(surface: Surface) {
                // 기존 스왑 체인이 있다면 파괴 (리소스 누수 방지)
                swapChain?.let { engine.destroySwapChain(it) }
                // 새로운 Surface로 스왑 체인 생성
                swapChain = engine.createSwapChain(surface)
                // 디스플레이 헬퍼를 렌더러와 디스플레이에 연결 (VSync 동기화)
                displayHelper.attach(renderer, surfaceView.display)
            }

            /**
             * Surface에서 분리될 때 호출되는 콜백 (액티비티 일시정지, 화면 회전 등)
             * 리소스 정리와 메모리 누수 방지를 위한 중요한 콜백
             */
            override fun onDetachedFromSurface() {
                // 디스플레이 헬퍼 분리 (VSync 동기화 해제)
                displayHelper.detach()
                // 스왑 체인 안전하게 정리
                swapChain?.let {
                    // 스왑 체인 파괴 명령 전송
                    engine.destroySwapChain(it)
                    // Filament이 destroySwapChain 명령 실행을 완료하기 전에 리턴하지 않도록 보장
                    // 그렇지 않으면 안드로이드가 Surface를 너무 일찍 파괴할 수 있음
                    // GPU 명령 큐의 모든 작업이 완료될 때까지 대기
                    engine.flushAndWait()
                    swapChain = null // 참조 제거
                }
            }

            /**
             * Surface 크기가 변경될 때 호출되는 콜백 (화면 회전, 윈도우 크기 변경 등)
             * @param width 새로운 Surface 너비 (픽셀 단위)
             * @param height 새로운 Surface 높이 (픽셀 단위)
             */
            override fun onResized(width: Int, height: Int) {
                // 뷰포트 업데이트 - 렌더링 영역을 새로운 크기에 맞게 조정
                // Viewport(x, y, width, height): 화면 좌표계에서 렌더링될 영역 정의
                view.viewport = Viewport(0, 0, width, height)
            }
        }

        // UiHelper를 SurfaceView에 연결하여 Surface 생명주기 이벤트 수신 시작
        attachTo(surfaceView)
    }

    /**
     * Filament 엔진과 관련된 모든 리소스를 정리하는 함수
     * 메모리 누수 방지와 안전한 종료를 위해 반드시 호출되어야 함
     * 리소스 정리 순서가 중요: UI 분리 → 엔진 파괴 → EGL 컨텍스트 파괴
     */
    fun destroy() {
        // 1단계: UI 헬퍼 분리 - Surface와의 연결을 먼저 해제
        // 엔진 파괴 전에 항상 surface 분리하여 안전한 종료 보장
        uiHelper.detach()

        // 2단계: Filament 엔진 파괴 - 모든 렌더링 리소스 정리
        // 씬, 뷰, 렌더러, 카메라 등 모든 Filament 객체들이 자동으로 정리됨
        engine.destroy()

        // 3단계: EGL 컨텍스트 파괴 - OpenGL ES 컨텍스트 해제
        // GPU 리소스 완전 해제, 반드시 마지막에 수행
        destroyEglContext(eglContext)
    }
}
