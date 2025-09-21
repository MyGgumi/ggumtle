/**
 * 프레임별 렌더링 콜백 및 FPS 제어
 * - Choreographer를 사용한 렌더링 루프 관리
 * - 최대 FPS 제한 및 프레임 레이트 제어
 * - ARCore 프레임과 Filament 렌더링 동기화
 * - 화면 갱신 생명주기 관리
 */
package com.ggumtle.mission.ar.renderer

import android.view.Choreographer
import com.ggumtle.mission.ar.arcore.ArCore
import com.google.ar.core.Frame
import java.util.concurrent.TimeUnit

class FrameCallback(
    private val arCore: ArCore,
    private val doFrame: (frame: Frame) -> Unit,
) : Choreographer.FrameCallback {
    companion object {
        private const val MAX_FRAMES_PER_SECOND: Long = 60
    }

    @Suppress("unused")
    enum class FrameRate(val factor: Long) {
        Full(1),
        Half(2),
        Third(3),
    }

    private val choreographer: Choreographer = Choreographer.getInstance()
    private var lastTick: Long = 0
    private var frameRate: FrameRate = FrameRate.Full

    override fun doFrame(frameTimeNanos: Long) {
        choreographer.postFrameCallback(this)

        // 최대 FPS로 제한
        val nanoTime = System.nanoTime()
        val tick = nanoTime / (TimeUnit.SECONDS.toNanos(1) / MAX_FRAMES_PER_SECOND)

        if (lastTick / frameRate.factor == tick / frameRate.factor) {
            return
        }

        lastTick = tick

        // 지터 가능성을 줄이기 위해 지난 틱의 프레임 사용 (레이턴시 증가)
        if (// AR 프레임이 있을 때만 렌더링
            arCore.timestamp != 0L &&
            arCore.filament.uiHelper.isReadyToRender &&
            // 너무 빠르게 GPU에 프레임을 전송하고 있음을 의미
            arCore.filament.renderer.beginFrame(arCore.filament.swapChain!!, frameTimeNanos)
        ) {
            arCore.filament.timestamp = arCore.timestamp
            arCore.filament.renderer.render(arCore.filament.view)
            arCore.filament.renderer.endFrame()
        }

        val frame = arCore.session.update()

        // 시작 시 카메라 시스템이 즉시 실제 이미지를 생성하지 않을 수 있음
        // 이러한 일반적인 경우 timestamp = 0인 프레임이 반환됨
        if (frame.timestamp != 0L &&
            frame.timestamp != arCore.timestamp
        ) {
            arCore.timestamp = frame.timestamp
            arCore.update(frame, arCore.filament)
            doFrame(frame)
        }
    }

    fun start() {
        choreographer.postFrameCallback(this)
    }

    fun stop() {
        choreographer.removeFrameCallback(this)
    }
}
