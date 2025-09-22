package com.ggumtle.mission.ar

import android.app.Activity
import android.content.Context
import android.content.res.Configuration
import android.util.Log
import android.view.MotionEvent
import android.view.SurfaceView
import com.ggumtle.mission.R
import com.ggumtle.mission.ar.arcore.ArCore
import com.ggumtle.mission.ar.filament.Filament
import com.ggumtle.mission.ar.gesture.DragGesture
import com.ggumtle.mission.ar.gesture.DragGestureRecognizer
import com.ggumtle.mission.ar.gesture.PinchGesture
import com.ggumtle.mission.ar.gesture.PinchGestureRecognizer
import com.ggumtle.mission.ar.gesture.TransformationSystem
import com.ggumtle.mission.ar.gesture.TwistGesture
import com.ggumtle.mission.ar.gesture.TwistGestureRecognizer
import com.ggumtle.mission.ar.renderer.FrameCallback
import com.ggumtle.mission.ar.renderer.LightRenderer
import com.ggumtle.mission.ar.renderer.ModelRenderer
import com.ggumtle.mission.ar.renderer.PlaneRenderer
import com.ggumtle.mission.ar.util.OpenGLVersionNotSupported
import com.ggumtle.mission.ar.util.ScreenPosition
import com.ggumtle.mission.ar.util.TouchEvent
import com.ggumtle.mission.ar.util.UserCanceled
import com.ggumtle.mission.ar.util.ViewRect
import com.ggumtle.mission.ar.util.checkIfOpenGlVersionSupported
import com.ggumtle.mission.ar.util.minOpenGlVersion
import com.ggumtle.mission.ar.util.showOpenGlNotSupportedDialog
import com.ggumtle.mission.ar.util.toRadians
import com.ggumtle.mission.ar.util.toViewRect
import com.ggumtle.mission.ar.util.x
import com.ggumtle.mission.ar.util.y
import com.google.ar.core.ArCoreApk
import com.google.ar.core.Plane
import com.google.ar.core.TrackingState
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.cancel
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.dropWhile
import kotlinx.coroutines.flow.filterNotNull
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.launch
import java.util.concurrent.TimeUnit
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.coroutines.coroutineContext
import kotlin.coroutines.resume
import kotlin.div
import kotlin.unaryMinus

@Singleton
class ArManager @Inject constructor() {

    // 액티비티의 resume/pause 상태를 추적하는 StateFlow
    private val resumeBehavior: MutableStateFlow<Unit?> =
        MutableStateFlow(null)

    // 화면 회전 및 구성 변경 확인 플로우
    private val configurationChangedEvents: MutableSharedFlow<Configuration> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    // 드래그 제스처
    private val dragEvents: MutableSharedFlow<Pair<ViewRect, TouchEvent>> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    // 핀치 제스처
    private val scaleEvents: MutableSharedFlow<Float> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    // 회전 제스처
    private val rotateEvents: MutableSharedFlow<Float> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    // ARCore,FrameCallback 관리 StateFlow
    private val arCoreBehavior: MutableStateFlow<Pair<ArCore, FrameCallback>?> =
        MutableStateFlow(null)

    // 터치 제스처를 처리하는 변환 시스템
    private lateinit var transformationSystem: TransformationSystem

    // 생성시 사용
    private lateinit var createScope: CoroutineScope

    // 시작시 사용
    private lateinit var startScope: CoroutineScope

    // Filament 렌더링을 위한 Surface View
    private lateinit var surfaceView: SurfaceView

    fun initialize(context: Context) {
        createScope = CoroutineScope(Dispatchers.Main + SupervisorJob())

        setupGestureHandlers(context as Activity, surfaceView)

        createScope.launch {
            try {
                createUx(context)
            } catch (error: Throwable) {
                Log.d("ArManager", "initialize: $error")
                if (error !is UserCanceled) {
                    error.printStackTrace()
                }
            }
        }
    }

    private fun setupGestureHandlers(context: Activity, surfaceView: SurfaceView) {
        transformationSystem = TransformationSystem(context.resources.displayMetrics)

        // 핀치 제스처 (크기 조절)
        transformationSystem.pinchRecognizer.addOnGestureStartedListener(
            object : PinchGestureRecognizer.OnGestureStartedListener {
                override fun onGestureStarted(gesture: PinchGesture) {
                    update(gesture)
                    gesture.setGestureEventListener(
                        object : PinchGesture.OnGestureEventListener {
                            override fun onFinished(gesture: PinchGesture) {
                                update(gesture)
                            }

                            override fun onUpdated(gesture: PinchGesture) {
                                update(gesture)
                            }
                        },
                    )
                }

                private fun update(gesture: PinchGesture) {
                    scaleEvents.tryEmit(1f + gesture.gapDeltaInches())
                }
            }
        )

        // 회전 제스처
        transformationSystem.twistRecognizer.addOnGestureStartedListener(
            object : TwistGestureRecognizer.OnGestureStartedListener {

                override fun onGestureStarted(gesture: TwistGesture) {
                    update(gesture)
                    gesture.setGestureEventListener(
                        object : TwistGesture.OnGestureEventListener {
                            override fun onFinished(gesture: TwistGesture) {
                                update(gesture)
                            }

                            override fun onUpdated(gesture: TwistGesture) {
                                update(gesture)
                            }
                        },
                    )
                }

                private fun update(gesture: TwistGesture) {
                    rotateEvents.tryEmit(-gesture.deltaRotationDegrees.toRadians)
                }
            }
        )

        // 드래그 제스처 (모델 이동)
        transformationSystem.dragRecognizer.addOnGestureStartedListener(
            object : DragGestureRecognizer.OnGestureStartedListener {
                override fun onGestureStarted(gesture: DragGesture) {
                    Pair(
                        surfaceView.toViewRect(),
                        TouchEvent.Move(gesture.position.x, gesture.position.y),
                    )
                        .let { dragEvents.tryEmit(it) }

                    gesture.setGestureEventListener(
                        object : DragGesture.OnGestureEventListener {
                            override fun onFinished(gesture: DragGesture) {
                                Pair(
                                    surfaceView.toViewRect(),
                                    TouchEvent.Stop(gesture.position.x, gesture.position.y),
                                ).let { dragEvents.tryEmit(it) }
                            }

                            override fun onUpdated(gesture: DragGesture) {
                                Pair(
                                    surfaceView.toViewRect(),
                                    TouchEvent.Move(gesture.position.x, gesture.position.y),
                                ).let { dragEvents.tryEmit(it) }
                            }
                        },
                    )
                }
            },
        )
    }

    suspend fun createUx(context: Activity) {

        // OpenGL ES 버전 지원 여부 확인
        if (context.checkIfOpenGlVersionSupported(minOpenGlVersion).not()) {
            showOpenGlNotSupportedDialog(context)
            throw OpenGLVersionNotSupported
        }

        // ARCore 설치 상태 확인 및 설치 요청
        if (ArCoreApk
                .getInstance()
                .requestInstall(
                    context,
                    true, // 사용자 프롬프트 허용
                    ArCoreApk.InstallBehavior.REQUIRED, // ARCore 설치 필수
                    ArCoreApk.UserMessageType.USER_ALREADY_INFORMED, // 사용자가 이미 정보를 받았음을 표시
                ) == ArCoreApk.InstallStatus.INSTALL_REQUESTED
        ) {

            resumeBehavior.dropWhile { it != null }.filterNotNull().first()

            // 설치 완료 후 다시 설치 상태 확인
            if (ArCoreApk
                    .getInstance()
                    .requestInstall(
                        context,
                        false, // 재확인이므로 사용자 프롬프트 없이 진행
                        ArCoreApk.InstallBehavior.REQUIRED,
                        ArCoreApk.UserMessageType.USER_ALREADY_INFORMED,
                    ) != ArCoreApk.InstallStatus.INSTALLED
            ) {
                // 설치가 완료되지 않은 경우 (사용자가 취소했거나 실패)
                throw UserCanceled
            }
        }

        val filament = Filament(context, surfaceView)

        try {
            val arCore = ArCore(context, filament, surfaceView)
            try {
                val lightRenderer = LightRenderer(context, arCore.filament)
                val planeRenderer = PlaneRenderer(context, arCore.filament)
                val modelRenderer = ModelRenderer(context, arCore, arCore.filament)
                try {
                    val frameCallback =
                        FrameCallback(
                            arCore,
                            doFrame = { frame ->
                                lightRenderer.doFrame(frame)
                                planeRenderer.doFrame(frame)
                                modelRenderer.doFrame(frame)
                            },
                        )

                    arCoreBehavior.emit(Pair(arCore, frameCallback))

                    with(CoroutineScope(coroutineContext)) {
                        launch {
                            configurationChangedEvents.collect {
                                arCore.configurationChange()
                            }
                        }

                        launch {
                            dragEvents
                                .map { (viewRect, touchEvent) ->
                                    ScreenPosition(
                                        x = touchEvent.x / viewRect.width,
                                        y = touchEvent.y / viewRect.height,
                                    )
                                        .let { ModelRenderer.ModelEvent.Move(it) }
                                }
                                .collect { modelRenderer.modelEvents.tryEmit(it) }
                        }

                        launch {
                            scaleEvents
                                .map {
                                    ModelRenderer.ModelEvent.Update(0f, it)
                                }
                                .collect { modelRenderer.modelEvents.tryEmit(it) }
                        }

                        launch {
                            rotateEvents
                                .map {
                                    ModelRenderer.ModelEvent.Update(it, 1f)
                                }
                                .collect { modelRenderer.modelEvents.tryEmit(it) }
                        }
                    }
                    awaitCancellation()
                } finally {
                    modelRenderer.destroy()
                }
            } finally {
                arCore.destroy()
            }
        } finally {
            filament.destroy()
        }
    }

    fun startUx() {
        startScope = CoroutineScope(Dispatchers.Main)
        startScope.launch{
            try {
                val (arCore, frameCallback) = arCoreBehavior.filterNotNull().first()
                try {
                    arCore.session.resume()
                    frameCallback.start()
                    awaitCancellation()
                } finally {
                    frameCallback.stop()
                    arCore.session.pause()
                }
            } catch (error: Throwable) {
                if (error !is UserCanceled) {
                    error.printStackTrace()
                }
            }
        }
    }

    fun onConfigurationChanged(newConfig: Configuration){
        configurationChangedEvents.tryEmit(newConfig)
    }

    fun pauseArSession(){
        resumeBehavior.tryEmit(null)
    }

    fun resumeArSession(){
        resumeBehavior.tryEmit(Unit)
    }

    fun cancelStartScope(){
        startScope.cancel()
    }

    fun cancelCreateScope(){
        createScope.cancel()
    }

    fun handleTouchEvent(context: Context, motionEvent: MotionEvent): Boolean {
        if (motionEvent.action == MotionEvent.ACTION_UP &&
            (motionEvent.eventTime - motionEvent.downTime) <
            context.resources.getInteger(R.integer.tap_event_milliseconds)
        ) {
            Pair(
                surfaceView.toViewRect(),
                TouchEvent.Stop(motionEvent.x, motionEvent.y),
            ).let { dragEvents.tryEmit(it) }
        }
        transformationSystem.onTouch(motionEvent)
        return true
    }

    fun setSurfaceView(surfaceView: SurfaceView){
        this.surfaceView = surfaceView
    }

    @OptIn(ExperimentalCoroutinesApi::class)
    fun clearManager(){
        // 모든 코루틴 취소
        createScope.cancel()
        if (this::startScope.isInitialized) {
            startScope.cancel()
        }

        // 모든 상태 초기화
        arCoreBehavior.value = null
        resumeBehavior.value = null

        // SharedFlow 버퍼 클리어
        dragEvents.resetReplayCache()
        scaleEvents.resetReplayCache()
        rotateEvents.resetReplayCache()
        configurationChangedEvents.resetReplayCache()

        Log.d("ArManager", "clearManager: 매니저 완전 초기화 완료")
    }

}