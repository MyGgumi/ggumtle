/**
 * 수학 유틸리티 및 3D 그래픽스 연산
 * - 4x4 행렬 연산 (scale, rotate, translate)
 * - 2D/3D/4D 벡터 연산 및 유틸리티
 * - AR 평면 처리 및 UV 매핑 함수
 * - 각도 변환 및 단위 변환 함수
 * - 버퍼 생성 및 메모리 관리 유틸리티
 */
package com.ggumtle.mission.ar.util

import android.app.Activity
import android.content.Context
import android.opengl.Matrix
import android.os.Build
import android.view.Surface
import com.ggumtle.mission.ar.arcore.ArCore
import com.google.ar.core.Frame
import com.google.ar.core.Pose
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer
import java.nio.ShortBuffer
import java.nio.channels.Channels
import kotlin.math.PI
import kotlin.math.ceil
import kotlin.math.floor
import kotlin.math.sqrt

// Float 타입의 바이트 크기 (4바이트) - 메모리 할당 및 버퍼 사이즈 계산에 사용
inline val Float.Companion.size get() = java.lang.Float.BYTES

// 각도 및 원주율 상수 정의 - 3D 그래픽스 회전 계산에 사용
inline val Float.Companion.degreesInTau: Float get() = 360f  // 한 바퀴(전체 원)의 도수 (360도)
inline val Float.Companion.tau: Float get() = PI.toFloat() * 2f  // 한 바퀴의 라디안 (2π)

// 라디안에서 도로 변환하는 확장 프로퍼티
inline val Float.toDegrees: Float get() = this * (Float.degreesInTau / Float.tau)
// 도에서 라디안으로 변환하는 확장 프로퍼티
inline val Float.toRadians: Float get() = this * (Float.tau / Float.degreesInTau)

// 각도를 0~2π 범위로 정규화하는 확장 프로퍼티
// 회전 각도가 음수이거나 2π를 초과할 때 사용
inline val Float.clampToTau: Float
    get() =
        when {
            this < 0f ->  // 음수 각도를 양수로 변환
                this + ceil(-this / Float.tau) * Float.tau

            this >= Float.tau ->  // 2π 이상의 각도를 0~2π 범위로 제한
                this - floor(this / Float.tau) * Float.tau

            else ->  // 이미 0~2π 범위 내에 있음
                this
        }

/**
 * 2차원 벡터 배열 데이터 클래스
 * UV 좌표, 2D 점 데이터 등을 저장하기 위해 사용
 * @param floatArray 2개의 float 값을 담는 배열 (x, y 또는 u, v)
 */
data class V2A(val floatArray: FloatArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as V2A

        // 배열 내용 비교 - 참조가 아닌 실제 데이터 비교
        return floatArray.contentEquals(other.floatArray)
    }

    override fun hashCode(): Int {
        // 배열 내용 기반 해시 코드 생성
        return floatArray.contentHashCode()
    }
}

/**
 * 3차원 벡터 데이터 클래스
 * 3D 좌표, 벡터 연산, 위치 데이터 등에 사용
 * @param floatArray 3개의 float 값을 담는 배열 (x, y, z)
 */
data class V3(val floatArray: FloatArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as V3

        return floatArray.contentEquals(other.floatArray)
    }

    override fun hashCode(): Int {
        return floatArray.contentHashCode()
    }
}

data class V3A(val floatArray: FloatArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as V3A

        return floatArray.contentEquals(other.floatArray)
    }

    override fun hashCode(): Int {
        return floatArray.contentHashCode()
    }
}

data class V4A(val floatArray: FloatArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as V4A

        return floatArray.contentEquals(other.floatArray)
    }

    override fun hashCode(): Int {
        return floatArray.contentHashCode()
    }
}

data class M4(val floatArray: FloatArray) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as M4

        return floatArray.contentEquals(other.floatArray)
    }

    override fun hashCode(): Int {
        return floatArray.contentHashCode()
    }
}

@JvmInline
value class TriangleIndexArray(val shortArray: ShortArray)

inline fun triangleIndexArrayCreate(
    count: Int,
    i1: (Int) -> Short,
    i2: (Int) -> Short,
    i3: (Int) -> Short
): TriangleIndexArray {
    val triangleIndexArray = TriangleIndexArray(ShortArray(count * 3))

    for (i in 0 until count) {
        val k = i * 3
        triangleIndexArray.shortArray[k + 0] = i1(i)
        triangleIndexArray.shortArray[k + 1] = i2(i)
        triangleIndexArray.shortArray[k + 2] = i3(i)
    }

    return triangleIndexArray
}

fun m4Identity(): M4 = FloatArray(16)
    .also { Matrix.setIdentityM(it, 0) }
    .let { M4(it) }

fun M4.scale(x: Float, y: Float, z: Float): M4 = FloatArray(16)
    .also { Matrix.scaleM(it, 0, floatArray, 0, x, y, z) }
    .let { M4(it) }

fun M4.rotate(angle: Float, x: Float, y: Float, z: Float): M4 = FloatArray(16)
    .also { Matrix.rotateM(it, 0, floatArray, 0, angle, x, y, z) }
    .let { M4(it) }

fun M4.translate(x: Float, y: Float, z: Float): M4 = FloatArray(16)
    .also { Matrix.translateM(it, 0, floatArray, 0, x, y, z) }
    .let { M4(it) }

@Suppress("unused")
fun M4.multiply(m: M4): M4 = FloatArray(16)
    .also { Matrix.multiplyMM(it, 0, floatArray, 0, m.floatArray, 0) }
    .let { M4(it) }

@Suppress("unused")
fun M4.invert(): M4 = FloatArray(16)
    .also { Matrix.invertM(it, 0, floatArray, 0) }
    .let { M4(it) }

@Suppress("unused")
fun m4Rotate(angle: Float, x: Float, y: Float, z: Float): M4 = FloatArray(16)
    .also { Matrix.setRotateM(it, 0, angle, x, y, z) }
    .let { M4(it) }

fun FloatArray.toDoubleArray(): DoubleArray = DoubleArray(size)
    .also { doubleArray ->
        for (i in indices) {
            doubleArray[i] = this[i].toDouble()
        }
    }

fun Frame.projectionMatrix(): M4 = FloatArray(16)
    .apply { camera.getProjectionMatrix(this, 0, ArCore.NEAR, ArCore.FAR) }
    .let { M4(it) }

@Suppress("DEPRECATION")
fun Activity.displayRotation(): Int =
    (if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) display
    else windowManager.defaultDisplay)!!.rotation

@Suppress("unused")
fun Activity.displayRotationDegrees(): Int =
    when (displayRotation()) {
        Surface.ROTATION_0 -> 0
        Surface.ROTATION_90 -> 90
        Surface.ROTATION_180 -> 180
        Surface.ROTATION_270 -> 270
        else -> throw Exception("Invalid Display Rotation")
    }

fun Pose.matrix(): M4 = FloatArray(16)
    .also { toMatrix(it, 0) }
    .let { M4(it) }

inline fun v2aCreate(count: Int, x: (Int) -> Float, y: (Int) -> Float): V2A =
    V2A(FloatArray(count * dimenV2A))
        .also {
            for (i in it.indices) {
                it.set(i, x(i), y(i))
            }
        }

const val dimenV2A: Int = 2

@Suppress("UnusedReceiverParameter")
inline val V2A.dimen: Int get() = dimenV2A
fun V2A.count(): Int = floatArray.size / dimen
inline val V2A.indices: IntRange get() = IntRange(0, count() - 1)

fun V2A.set(i: Int, x: Float, y: Float) {
    floatArray[(i * dimen) + 0] = x
    floatArray[(i * dimen) + 1] = y
}

const val dimenV3A: Int = 3

@Suppress("UnusedReceiverParameter")
inline val V3A.dimen: Int get() = dimenV3A

fun V3A.set(i: Int, x: Float, y: Float, z: Float) {
    floatArray[(i * dimen) + 0] = x
    floatArray[(i * dimen) + 1] = y
    floatArray[(i * dimen) + 2] = z
}

fun mulV3(r: FloatArray, ri: Int, v: FloatArray, vi: Int, s: Float) {
    r[ri + 0] = v[vi + 0] * s
    r[ri + 1] = v[vi + 1] * s
    r[ri + 2] = v[vi + 2] * s
}

const val dimenV4A: Int = 4

@Suppress("UnusedReceiverParameter")
inline val V4A.dimen: Int get() = dimenV4A
inline val V4A.count: Int get() = floatArray.size / dimen

fun V4A.getX(i: Int): Float = floatArray[(i * dimen) + 0]

@Suppress("unused")
fun V4A.getY(i: Int): Float = floatArray[(i * dimen) + 1]
fun V4A.getZ(i: Int): Float = floatArray[(i * dimen) + 2]

@Suppress("unused")
fun V4A.getW(i: Int): Float = floatArray[(i * dimen) + 3]

fun V4A.set(i: Int, x: Float, y: Float, z: Float, w: Float) {
    floatArray[(i * dimen) + 0] = x
    floatArray[(i * dimen) + 1] = y
    floatArray[(i * dimen) + 2] = z
    floatArray[(i * dimen) + 3] = w
}

/**
 * 3D 벡터 내적(도트 곱) 연산
 * 두 벡터 간의 각도 계산이나 직교성 검사에 사용
 * @param v 내적을 계산할 다른 벡터
 * @return 내적 결과값 (cos(각도) * |벡터들의 크기의 곱|)
 */
fun V3.dot(v: V3): Float =
    x * v.x + y * v.y + z * v.z

/**
 * 3D 벡터의 방향을 반대로 바꾸는 연산 (네게이션)
 * 벡터의 방향을 180도 뒤집어 돌리는 효과
 * @return 반대 방향의 벡터
 */
fun V3.neg(): V3 =
    v3(
        -x,  // X 성분 음수 변환
        -y,  // Y 성분 음수 변환
        -z   // Z 성분 음수 변환
    )

// 3D 원점 벡터 (0, 0, 0) - 전역 상수로 자주 사용되는 기준점
val v3Origin: V3 = v3(0f, 0f, 0f)

/**
 * 3D 벡터 생성자 함수
 * 주어진 x, y, z 좌표로 새로운 3D 벡터를 생성
 * @param x X축 좌표
 * @param y Y축 좌표
 * @param z Z축 좌표
 * @return 생성된 V3 벡터 인스턴스
 */
fun v3(x: Float, y: Float, z: Float): V3 = V3(FloatArray(3))
    .also {
        it.x = x  // X 좌표 설정
        it.y = y  // Y 좌표 설정
        it.z = z  // Z 좌표 설정
    }

inline var V3.x: Float
    get() = floatArray[0]
    set(x) {
        floatArray[0] = x
    }

inline var V3.y: Float
    get() = floatArray[1]
    set(y) {
        floatArray[1] = y
    }

inline var V3.z: Float
    get() = floatArray[2]
    set(z) {
        floatArray[2] = z
    }

fun V3.normalize(): V3 =
    scale(1f / magnitude())

fun V3.magnitude(): Float =
    sqrt(dot(this))

fun V3.scale(s: Float): V3 =
    v3(
        x * s,
        y * s,
        z * s
    )

fun V3.div(d: Float): V3 =
    v3(
        x / d,
        y / d,
        z / d
    )

fun FloatArray.toFloatBuffer(): FloatBuffer = ByteBuffer
    .allocateDirect(size * Float.size)
    .order(ByteOrder.nativeOrder())
    .asFloatBuffer()
    .also { floatBuffer ->
        floatBuffer.put(this)
        floatBuffer.rewind()
    }

fun Context.readUncompressedAsset(@Suppress("SameParameterValue") assetName: String): ByteBuffer {
    assets.openFd(assetName)
        .use { fd ->
            val input = fd.createInputStream()
            val dst = ByteBuffer.allocate(fd.length.toInt())

            val src = Channels.newChannel(input)
            src.read(dst)
            src.close()

            return dst.apply { rewind() }
        }
}

fun ShortArray.toShortBuffer(): ShortBuffer = ShortBuffer
    .allocate(size)
    .also { shortBuffer ->
        shortBuffer.put(this)
        shortBuffer.rewind()
    }

// 이 계수들은 복사 조도에 대한 Filament IndirectLight Java 문서에서 추출
private val environmentalHdrToFilamentShCoefficients =
    floatArrayOf(
        0.282095f, -0.325735f, 0.325735f,
        -0.325735f, 0.273137f, -0.273137f,
        0.078848f, -0.273137f, 0.136569f
    )

fun getEnvironmentalHdrSphericalHarmonics(sphericalHarmonics: FloatArray): FloatArray =
    FloatArray(27)
        .also { irradianceData ->
            for (index in 0 until 27 step 3) {
                mulV3(
                    irradianceData,
                    index,
                    sphericalHarmonics,
                    index,
                    environmentalHdrToFilamentShCoefficients[index / 3]
                )
            }
        }

fun FloatBuffer.polygonToVertices(m: M4): V4A {
    val f = FloatArray((capacity() / 2) * 4)
    val v = FloatArray(4)
    v[1] = 0f
    v[3] = 1f
    rewind()

    for (i in f.indices step 4) {
        v[0] = get()
        v[2] = get()
        Matrix.multiplyMV(f, i, m.floatArray, 0, v, 0)
    }

    return V4A(f)
}

fun FloatBuffer.polygonToUV(): V2A {
    val f = V2A(FloatArray(capacity()))
    rewind()

    for (i in f.indices) {
        f.set(i, get() * 10f, get() * 5f)
    }

    return f
}

// 더 나은 안정성을 위해 월드 좌표계를 사용하여 UV 좌표 결정
fun V4A.horizontalToUV(): V2A = v2aCreate(count, { i -> getX(i) * 10f }, { i -> getZ(i) * 5f })

fun V3.sub(v: V3): V3 =
    v3(
        x - v.x,
        y - v.y,
        z - v.z
    )

fun V3.eq(v: V3): Boolean = x == v.x &&
        y == v.y &&
        z == v.z
