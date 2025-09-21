/**
 * 애플리케이션 전역 설정 및 예외 처리
 * - 사용자 취소 및 OpenGL 버전 미지원 예외
 * - 카메라 권한 요청 코드 상수
 * - OpenGL 버전 확인 및 대화상자 표시
 * - 최소 요구 사항 검증 유틸리티
 */
package com.ggumtle.mission.ar.util

import android.app.Activity
import android.app.ActivityManager
import android.content.Context
import androidx.appcompat.app.AlertDialog
import androidx.core.content.ContextCompat
import com.ggumtle.mission.R
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlin.coroutines.resume

object UserCanceled : Exception()
object OpenGLVersionNotSupported : Exception()

const val cameraPermissionRequestCode = 1001
val minOpenGlVersion = Version(3, 0, 0, null, null)

fun Context.checkIfOpenGlVersionSupported(minOpenGlVersion: Version): Boolean =
    versionComparator.compare(
        minOpenGlVersion,
        ContextCompat
            .getSystemService(this, ActivityManager::class.java)!!
            .deviceConfigurationInfo
            .glEsVersion
            .let { parserVersion.parse(it) }
    ) <= 0

suspend fun showOpenGlNotSupportedDialog(
    activity: Activity,
) = suspendCancellableCoroutine<Unit> { continuation ->
    val alertDialog = AlertDialog
        .Builder(activity)
        .setTitle(R.string.opengl_required_title)
        .setMessage(
            activity.getString(R.string.opengl_required_message, minOpenGlVersion.print()),
        )
        .setPositiveButton("OK") { _, _ -> continuation.resume(Unit) }
        .setCancelable(false)
        .show()

    continuation.invokeOnCancellation { alertDialog.dismiss() }
}
