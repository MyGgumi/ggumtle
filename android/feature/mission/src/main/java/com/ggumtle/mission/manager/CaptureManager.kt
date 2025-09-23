package com.ggumtle.mission.manager

import android.content.ContentValues
import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import android.net.Uri
import android.os.Build
import android.os.Environment
import android.os.Handler
import android.os.Looper
import android.provider.MediaStore
import android.view.PixelCopy
import android.view.SurfaceView
import androidx.core.graphics.createBitmap
import kotlinx.coroutines.suspendCancellableCoroutine
import java.io.File
import java.io.FileOutputStream
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.coroutines.resume
import kotlin.coroutines.resumeWithException

@Singleton
class CaptureManager @Inject constructor() {

    private lateinit var surfaceView: SurfaceView

    fun setSurfaceView(surfaceView: SurfaceView){
        this.surfaceView = surfaceView
    }

    suspend fun captureSurfaceView(): Bitmap =
        suspendCancellableCoroutine { continuation ->
            val bitmap = createBitmap(surfaceView.width, surfaceView.height)

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                PixelCopy.request(
                    surfaceView,
                    bitmap,
                    { result ->
                        if (result == PixelCopy.SUCCESS) {
                            continuation.resume(bitmap)
                        } else {
                            continuation.resumeWithException(Exception("Failed to capture surface view"))
                        }
                    },
                    Handler(Looper.getMainLooper())
                )
            } else {
                val canvas = Canvas(bitmap)
                surfaceView.draw(canvas)
                continuation.resume(bitmap)
            }
        }

    suspend fun saveImageToGallery(context: Context, bitmap: Bitmap): Uri =
        suspendCancellableCoroutine { continuation ->
            val filename = "AR_Mission_${
                SimpleDateFormat(
                    "yyyyMMdd_HHmmss",
                    Locale.getDefault()
                ).format(Date())
            }.jpg"

            try {
                val uri = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
                    val contentValues = ContentValues().apply {
                        put(MediaStore.MediaColumns.DISPLAY_NAME, filename)
                        put(MediaStore.MediaColumns.MIME_TYPE, "image/jpeg")
                        put(
                            MediaStore.MediaColumns.RELATIVE_PATH,
                            Environment.DIRECTORY_PICTURES + "/AR_Mission"
                        )
                    }

                    val resolver = context.contentResolver
                    val imageUri =
                        resolver.insert(MediaStore.Images.Media.EXTERNAL_CONTENT_URI, contentValues)

                    imageUri?.let { uri ->
                        resolver.openOutputStream(uri)?.use { outputStream ->
                            bitmap.compress(Bitmap.CompressFormat.JPEG, 95, outputStream)
                        }
                        continuation.resume(uri)
                    } ?: continuation.resumeWithException(Exception("Failed to create image URI"))
                } else {
                    val imagesDir = File(
                        Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_PICTURES),
                        "AR_Mission"
                    )
                    if (!imagesDir.exists()) {
                        imagesDir.mkdirs()
                    }

                    val imageFile = File(imagesDir, filename)
                    FileOutputStream(imageFile).use { outputStream ->
                        bitmap.compress(Bitmap.CompressFormat.JPEG, 95, outputStream)
                    }

                    MediaStore.Images.Media.insertImage(
                        context.contentResolver,
                        imageFile.absolutePath,
                        filename,
                        "AR Mission Photo"
                    )
                    Uri.fromFile(imageFile)
                }
            } catch (e: Exception) {
                continuation.resumeWithException(e)
            }
        }

    fun analyzeImageForModel(bitmap: Bitmap): Boolean {
        try {
            val width = bitmap.width
            val height = bitmap.height

            // 화면 중앙 영역 (25% ~ 75% 범위) 분석
            val centerStartX = (width * 0.25f).toInt()
            val centerEndX = (width * 0.75f).toInt()
            val centerStartY = (height * 0.25f).toInt()
            val centerEndY = (height * 0.75f).toInt()

            var nonBackgroundPixels = 0
            var totalPixels = 0

            // 중앙 영역의 픽셀들을 샘플링하여 분석 (성능을 위해 10픽셀마다 샘플링)
            for (x in centerStartX until centerEndX step 10) {
                for (y in centerStartY until centerEndY step 10) {
                    val pixel = bitmap.getPixel(x, y)
                    val red = (pixel shr 16) and 0xFF
                    val green = (pixel shr 8) and 0xFF
                    val blue = pixel and 0xFF

                    // 배경이 아닌 것으로 판단되는 픽셀 (너무 어둡거나 단순한 색상이 아닌 경우)
                    if (isNonBackgroundPixel(red, green, blue)) {
                        nonBackgroundPixels++
                    }
                    totalPixels++
                }
            }

            // 배경이 아닌 픽셀이 전체의 5% 이상이면 모델이 포함된 것으로 판단
            val ratio = if (totalPixels > 0) nonBackgroundPixels.toFloat() / totalPixels else 0f
            return ratio > 0.05f

        } catch (e: Exception) {
            return false
        }
    }

    private fun isNonBackgroundPixel(red: Int, green: Int, blue: Int): Boolean {
        // 다양한 조건으로 배경이 아닌 픽셀 판단

        // 1. 너무 어두운 픽셀 제외 (배경이 검은색인 경우)
        if (red < 30 && green < 30 && blue < 30) return false

        // 2. 너무 밝은 픽셀 제외 (배경이 흰색인 경우)
        if (red > 220 && green > 220 && blue > 220) return false

        // 3. 색상의 분산이 적은 단조로운 픽셀 제외
        val max = maxOf(red, green, blue)
        val min = minOf(red, green, blue)
        if (max - min < 20) return false

        // 4. 채도가 있는 색상이거나 중간 밝기의 픽셀은 모델로 판단
        return true
    }

}