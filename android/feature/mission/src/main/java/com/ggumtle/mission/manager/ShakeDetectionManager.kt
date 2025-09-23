package com.ggumtle.mission.manager

import android.content.Context
import android.hardware.Sensor
import android.hardware.SensorEvent
import android.hardware.SensorEventListener
import android.hardware.SensorManager
import android.util.Log
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.math.abs
import kotlin.math.sqrt

@Singleton
class ShakeDetectionManager @Inject constructor() : SensorEventListener {

    private var sensorManager: SensorManager? = null
    private var accelerometer: Sensor? = null

    private val _shakeDetected = MutableStateFlow(false)
    val shakeDetected: StateFlow<Boolean> = _shakeDetected

    // 흔들기 감지 설정
    private var lastUpdate: Long = 0
    private var lastX: Float = 0f
    private var lastY: Float = 0f
    private var lastZ: Float = 0f

    // 간단한 흔들기 감지 설정
    private val shakeThreshold = 16f // 전체 가속도 변화량 임계값
    private val shakeTimeGap = 500L // 연속 흔들기 방지 간격 (ms)
    private var lastShakeTime = 0L

    fun initialize(context: Context) {
        sensorManager = context.getSystemService(Context.SENSOR_SERVICE) as SensorManager
        accelerometer = sensorManager?.getDefaultSensor(Sensor.TYPE_ACCELEROMETER)
    }

    fun startDetection() {
        accelerometer?.let { sensor ->
            sensorManager?.registerListener(this, sensor, SensorManager.SENSOR_DELAY_UI)
            Log.d("ShakeDetectionManager", "흔들기 감지 시작")
        }
    }

    fun stopDetection() {
        sensorManager?.unregisterListener(this)
        Log.d("ShakeDetectionManager", "흔들기 감지 중지")
    }

    override fun onSensorChanged(event: SensorEvent?) {
        event?.let {
            val currentTime = System.currentTimeMillis()

            // 100ms마다 체크
            if (currentTime - lastUpdate > 100) {
                lastUpdate = currentTime

                val x = event.values[0]
                val y = event.values[1]
                val z = event.values[2]

                // 전체 가속도 변화량 계산 (벡터 크기)
                val acceleration = sqrt(
                    (x - lastX) * (x - lastX) +
                            (y - lastY) * (y - lastY) +
                            (z - lastZ) * (z - lastZ)
                )

                // 임계값 초과 시 흔들기로 감지
                if (acceleration > shakeThreshold) {
                    // 연속 감지 방지
                    if (currentTime - lastShakeTime > shakeTimeGap) {
                        lastShakeTime = currentTime
                        _shakeDetected.value = true
                        Log.d("ShakeDetectionManager", "흔들기 감지! 가속도: $acceleration")
                    }
                }

                lastX = x
                lastY = y
                lastZ = z
            }
        }
    }

    override fun onAccuracyChanged(sensor: Sensor?, accuracy: Int) {
        // 필요시 구현
    }

    fun resetShakeDetected() {
        _shakeDetected.value = false
    }
}