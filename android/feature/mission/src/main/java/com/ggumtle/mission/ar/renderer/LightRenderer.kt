/**
 * AR 조명 시스템 및 HDR 조명 처리
 * - ARCore의 환경 조명 정보를 기반으로 동적 조명 설정
 * - 직진광 및 간접광 처리
 * - HDR 환경 맵 및 구면 반사 텍스처 로드
 * - 실시간 색밄 및 방향 조명 업데이트
 */
package com.ggumtle.mission.ar.renderer

import android.content.Context
import android.content.res.AssetManager
import android.graphics.BitmapFactory
import com.ggumtle.mission.ar.util.V3
import com.ggumtle.mission.ar.util.div
import com.ggumtle.mission.ar.filament.Filament
import com.ggumtle.mission.ar.util.getEnvironmentalHdrSphericalHarmonics
import com.google.android.filament.Engine
import com.google.android.filament.EntityInstance
import com.google.android.filament.EntityManager
import com.google.android.filament.IndirectLight
import com.google.android.filament.LightManager
import com.google.android.filament.Texture
import com.google.ar.core.Frame
import com.google.ar.core.LightEstimate
import java.nio.ByteBuffer
import kotlin.math.log2
import kotlin.math.max

class LightRenderer(context: Context, private val filament: Filament) {
    private val reflections: Texture = loadReflections(context.assets, filament.engine)
    private var irradiance: FloatArray = FloatArray(27)

    @EntityInstance
    private var directionalLightInstance: Int = EntityManager
        .get()
        .create()
        .let { directionalLight ->
            filament.scene.addEntity(directionalLight)

            LightManager
                .Builder(LightManager.Type.DIRECTIONAL)
                .castShadows(true)
                .build(filament.engine, directionalLight)

            filament.engine.lightManager.getInstance(directionalLight)
        }

    fun doFrame(frame: Frame) {
        // 조명 추정값 업데이트
        if (frame.lightEstimate.state != LightEstimate.State.VALID) {
            return
        }

        val irradianceUpdate = frame.lightEstimate.environmentalHdrAmbientSphericalHarmonics
            .let { getEnvironmentalHdrSphericalHarmonics(it) }

        if (irradiance.asSequence().zip(irradianceUpdate.asSequence()).any { (x, y) -> x != y }) {
            irradiance = irradianceUpdate

            filament.scene.indirectLight = IndirectLight
                .Builder()
                .reflections(reflections)
                .irradiance(3, irradiance)
                .build(filament.engine)
        }

        with(frame.lightEstimate.environmentalHdrMainLightDirection) {
            filament.engine.lightManager.setDirection(
                directionalLightInstance,
                -get(0),
                -get(1),
                -get(2),
            )
        }

        with(frame.lightEstimate.environmentalHdrMainLightIntensity) {
            // HDR RGB 값을 [0, 1) 범위에 맞게 스케일링
            // 더 나은 변환 방법이 있을 수 있음
            val rgbMax = max(max(get(0), get(1)), get(2))
            // 0으로 나누기 방지
            val color = V3(this).div(max(0.00001f, rgbMax))

            filament.engine.lightManager.setColor(
                directionalLightInstance,
                color.floatArray[0],
                color.floatArray[1],
                color.floatArray[2],
            )
        }
    }
}

@Suppress("SameParameterValue")
private fun peekSize(assets: AssetManager, name: String): Pair<Int, Int> {
    assets.open(name).use { input ->
        val opts = BitmapFactory.Options().apply { inJustDecodeBounds = true }
        BitmapFactory.decodeStream(input, null, opts)
        return opts.outWidth to opts.outHeight
    }
}

private const val reflectionsName = "reflections"

private fun loadCubemap(
    texture: Texture,
    assets: AssetManager,
    engine: Engine,
    prefix: String = "",
    level: Int = 0,
): Boolean {
    // 중요: 알파 채널은 불투명도가 아니라 HDR 데이터를 나타내기 위한 R11G11B10F 이미지의 일부 비트
    // 안드로이드가 RGB 채널에 알파 채널을 미리 곱하지 않도록 알려야 함
    val opts = BitmapFactory.Options().apply { inPremultiplied = false }

    // R11G11B10F는 항상 픽셀당 4바이트
    val faceSize = texture.getWidth(level) * texture.getHeight(level) * 4
    val offsets = IntArray(6) { it * faceSize }
    // 모든 큐브맵 면에 충분한 메모리 할당
    val storage = ByteBuffer.allocateDirect(faceSize * 6)

    arrayOf("px", "nx", "py", "ny", "pz", "nz").forEach { suffix ->
        try {
            assets.open("$reflectionsName/$prefix$suffix.rgb32f").use {
                val bitmap = BitmapFactory.decodeStream(it, null, opts)
                bitmap?.copyPixelsToBuffer(storage)
            }
        } catch (e: Exception) {
            return false
        }
    }

    // 텍스처 버퍼 리와인드
    storage.flip()

    val buffer = Texture.PixelBufferDescriptor(
        storage,
        Texture.Format.RGB, Texture.Type.UINT_10F_11F_11F_REV
    )

    texture.setImage(engine, level, buffer, offsets)
    return true
}

private fun loadReflections(assets: AssetManager, engine: Engine): Texture {
    val (w, h) = peekSize(assets, "$reflectionsName/m0_nx.rgb32f")
    val texture = Texture.Builder()
        .width(w)
        .height(h)
        .levels(log2(w.toFloat()).toInt() + 1)
        .format(Texture.InternalFormat.R11F_G11F_B10F)
        .sampler(Texture.Sampler.SAMPLER_CUBEMAP)
        .build(engine)

    for (i in 0 until texture.levels) {
        if (!loadCubemap(texture, assets, engine, "m${i}_", i)) break
    }

    return texture
}
