package com.ggumtle.mission.ar

import android.app.Activity
import android.app.Application
import android.content.Context
import android.view.MotionEvent
import android.view.SurfaceView
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
import com.ggumtle.mission.ar.util.ScreenPosition
import com.ggumtle.mission.ar.util.TouchEvent
import com.ggumtle.mission.ar.util.ViewRect
import com.ggumtle.mission.ar.util.toRadians
import com.ggumtle.mission.ar.util.toViewRect
import com.ggumtle.mission.ar.util.x
import com.ggumtle.mission.ar.util.y
import com.google.ar.core.Plane
import com.google.ar.core.TrackingState
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.launch
import javax.inject.Inject
import javax.inject.Singleton

@Singleton
class ArManager @Inject constructor() {
    private var filament: Filament? = null
    private var arCore: ArCore? = null
    private var frameCallback: FrameCallback? = null
    private var transformationSystem: TransformationSystem? = null

    // 렌더러들
    private var lightRenderer: LightRenderer? = null
    private var planeRenderer: PlaneRenderer? = null
    private var modelRenderer: ModelRenderer? = null

    // 이벤트 스트림들
    private val dragEvents: MutableSharedFlow<Pair<ViewRect, TouchEvent>> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)
    private val scaleEvents: MutableSharedFlow<Float> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)
    private val rotateEvents: MutableSharedFlow<Float> =
        MutableSharedFlow(extraBufferCapacity = 1, onBufferOverflow = BufferOverflow.DROP_OLDEST)

    private var arScope: CoroutineScope? = null
    suspend fun initialize(context: Context, surfaceView: SurfaceView) {
        try {
            arScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)

            // Filament 렌더링 엔진 초기화
            filament = Filament(context, surfaceView)
            val activity = context as? Activity
                ?: throw IllegalArgumentException("ARCore requires Activity context, but received: ${context::class.java}")

            // ARCore 세션 초기화
            arCore = ArCore(activity, filament!!, surfaceView)

            // 렌더러들 초기화
            lightRenderer = LightRenderer(context, filament!!)
            planeRenderer = PlaneRenderer(context, filament!!)
            modelRenderer = ModelRenderer(context, arCore!!, filament!!)

            // 제스처 시스템 초기화
            transformationSystem = TransformationSystem(context.resources.displayMetrics)
            setupGestureHandlers(surfaceView)

            // 프레임 콜백 설정
            frameCallback = FrameCallback(
                arCore!!,
                doFrame = { frame ->
                    // 각 렌더러 프레임 처리
                    lightRenderer?.doFrame(frame)
                    planeRenderer?.doFrame(frame)
                    modelRenderer?.doFrame(frame)
                }
            )

            setupGestureEventHandlers()

        } catch (e: Exception) {
            destroy()
            throw e
        }
    }

    private fun setupGestureHandlers(surfaceView: SurfaceView) {
        val transformationSystem = this.transformationSystem ?: return

        // 핀치 제스처 (크기 조절)
        transformationSystem.pinchRecognizer.addOnGestureStartedListener(
            object : PinchGestureRecognizer.OnGestureStartedListener {
                override fun onGestureStarted(gesture: PinchGesture) {
                    val updateGesture = { g: PinchGesture ->
                        scaleEvents.tryEmit(1f + g.gapDeltaInches())
                    }

                    updateGesture(gesture)
                    gesture.setGestureEventListener(
                        object : PinchGesture.OnGestureEventListener {
                            override fun onFinished(gesture: PinchGesture) {
                                updateGesture(gesture)
                            }
                            override fun onUpdated(gesture: PinchGesture) {
                                updateGesture(gesture)
                            }
                        }
                    )
                }
            }
        )

        // 회전 제스처
        transformationSystem.twistRecognizer.addOnGestureStartedListener(
            object : TwistGestureRecognizer.OnGestureStartedListener {
                override fun onGestureStarted(gesture: TwistGesture) {
                    val updateGesture = { g: TwistGesture ->
                        rotateEvents.tryEmit(-g.deltaRotationDegrees.toRadians)
                    }

                    updateGesture(gesture)
                    gesture.setGestureEventListener(
                        object : TwistGesture.OnGestureEventListener {
                            override fun onFinished(gesture: TwistGesture) {
                                updateGesture(gesture)
                            }
                            override fun onUpdated(gesture: TwistGesture) {
                                updateGesture(gesture)
                            }
                        }
                    )
                }
            }
        )

        // 드래그 제스처 (모델 이동)
        transformationSystem.dragRecognizer.addOnGestureStartedListener(
            object : DragGestureRecognizer.OnGestureStartedListener {
                override fun onGestureStarted(gesture: DragGesture) {
                    val emitDragEvent = { g: DragGesture, eventType: TouchEvent ->
                        Pair(surfaceView.toViewRect(), eventType).let { dragEvents.tryEmit(it) }
                    }

                    emitDragEvent(gesture, TouchEvent.Move(gesture.position.x, gesture.position.y))

                    gesture.setGestureEventListener(
                        object : DragGesture.OnGestureEventListener {
                            override fun onFinished(gesture: DragGesture) {
                                emitDragEvent(gesture, TouchEvent.Stop(gesture.position.x, gesture.position.y))
                            }
                            override fun onUpdated(gesture: DragGesture) {
                                emitDragEvent(gesture, TouchEvent.Move(gesture.position.x, gesture.position.y))
                            }
                        }
                    )
                }
            }
        )
    }

    private fun setupGestureEventHandlers() {
        val modelRenderer = this.modelRenderer ?: return
        val scope = this.arScope ?: return // 이 줄 추가

        // 드래그 이벤트를 모델 이동으로 변환
        scope.launch {
            dragEvents
                .collect { (viewRect, touchEvent) ->
                    val screenPos = ScreenPosition(
                        x = touchEvent.x / viewRect.width,
                        y = touchEvent.y / viewRect.height
                    )
                    modelRenderer.modelEvents.tryEmit(ModelRenderer.ModelEvent.Move(screenPos))
                }
        }

        // 스케일 이벤트 처리
        scope.launch {
            scaleEvents
                .collect { scale ->
                    modelRenderer.modelEvents.tryEmit(ModelRenderer.ModelEvent.Update(0f, scale))
                }
        }

        // 회전 이벤트 처리
        scope.launch {
            rotateEvents
                .collect { rotation ->
                    modelRenderer.modelEvents.tryEmit(ModelRenderer.ModelEvent.Update(rotation, 1f))
                }
        }
    }

    suspend fun startSession() {
        arCore?.session?.resume()
        frameCallback?.start()
    }

    suspend fun stopSession() {
        frameCallback?.stop()
        arCore?.session?.pause()
    }

    suspend fun handleConfigurationChange() {
        arCore?.configurationChange()
    }

    fun handleTouchEvent(motionEvent: MotionEvent): Boolean {
        // 짧은 탭을 드래그 Stop 이벤트로 변환
        if (motionEvent.action == MotionEvent.ACTION_UP &&
            (motionEvent.eventTime - motionEvent.downTime) < 200 // 200ms 미만은 탭으로 인식
        ) {
            // 탭 이벤트를 Stop 이벤트로 변환하여 모델 배치
            val touchEvent = TouchEvent.Stop(motionEvent.x, motionEvent.y)
            dragEvents.tryEmit(Pair(
                ViewRect(0f, 0f, 1080f, 1920f), // 임시 화면 크기
                touchEvent
            ))
        }

        transformationSystem?.onTouch(motionEvent)
        return true
    }

    fun destroy() {
        arScope?.cancel()
        modelRenderer?.destroy()
        arCore?.destroy()
        filament?.destroy()

        modelRenderer = null
        planeRenderer = null
        lightRenderer = null
        frameCallback = null
        arCore = null
        filament = null
        transformationSystem = null
    }
}