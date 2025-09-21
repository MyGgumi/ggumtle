package com.ggumtle.mission

import android.Manifest
import android.app.Activity
import android.app.Application
import android.content.pm.PackageManager
import android.util.Log
import android.view.SurfaceView
import androidx.core.content.ContextCompat
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ggumtle.mission.ar.ArManager
import com.ggumtle.mission.ar.util.*
import com.google.ar.core.ArCoreApk
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject

@HiltViewModel
class MissionViewModel @Inject constructor(
    private val application: Application,
    private val arManager: ArManager
) : ContainerHost<MissionContract.State, MissionContract.SideEffect>, ViewModel() {

    companion object {
        private const val TAG = "MissionViewModel"
    }

    override val container =
        container<MissionContract.State, MissionContract.SideEffect>(
            initialState = MissionContract.State()
        )

    private var surfaceView: SurfaceView? = null
    private var activity: Activity? = null
    private var isActivityResumed = false

    fun initializeAR() = intent {
        Log.d(TAG, "initializeAR: 시작 - isActivityResumed=$isActivityResumed")

        if (!isActivityResumed) {
            Log.w(TAG, "Activity가 resumed 상태가 아님")
            return@intent
        }

        val currentActivity = activity
        if (currentActivity == null) {
            Log.e(TAG, "Activity가 null입니다")
            postSideEffect(MissionContract.SideEffect.ShowToast("Activity 초기화 오류"))
            return@intent
        }

        val currentSurfaceView = surfaceView
        if (currentSurfaceView == null) {
            Log.e(TAG, "SurfaceView가 null입니다")
            postSideEffect(MissionContract.SideEffect.ShowToast("SurfaceView 초기화 오류"))
            return@intent
        }

        reduce { state.copy(isLoading = true, errorMessage = null) }
        Log.d(TAG, "AR 초기화 시작")

        // 메인 스레드에서 AR 초기화 실행 (FrameCallback의 Choreographer 때문에 필요)
        viewModelScope.launch(Dispatchers.Main) {
            try {
                // OpenGL 버전 확인
                Log.d(TAG, "OpenGL 버전 확인 중...")
                if (!currentActivity.checkIfOpenGlVersionSupported(minOpenGlVersion)) {
                    Log.e(TAG, "OpenGL ES 3.0 미지원")
                    postSideEffect(MissionContract.SideEffect.ShowOpenGlNotSupportedDialog("OpenGL ES 3.0이 필요합니다."))
                    throw OpenGLVersionNotSupported
                }
                Log.d(TAG, "OpenGL 버전 확인 완료")

                // ARCore 설치 확인
                Log.d(TAG, "ARCore 설치 확인 중...")
                checkAndInstallArCore(currentActivity)
                Log.d(TAG, "ARCore 설치 확인 완료")

                // 카메라 권한 확인
                Log.d(TAG, "카메라 권한 확인 중...")
                checkCameraPermission()
                Log.d(TAG, "카메라 권한 확인 완료")

                // AR 시스템 초기화 - 메인 스레드에서 실행
                Log.d(TAG, "AR 시스템 초기화 중...")
                arManager.initialize(currentActivity, currentSurfaceView)
                Log.d(TAG, "AR 시스템 초기화 완료")

                reduce { state.copy(isArSessionReady = true, isLoading = false) }
                Log.d(TAG, "AR 초기화 성공")

                // AR 초기화 완료 후 즉시 세션 시작
                startARSession()

            } catch (e: OpenGLVersionNotSupported) {
                Log.e(TAG, "OpenGL 버전 미지원", e)
                reduce { state.copy(errorMessage = "OpenGL ES 3.0을 지원하지 않습니다.", isLoading = false) }
                postSideEffect(MissionContract.SideEffect.NavigateToGrowth)
            } catch (e: UserCanceled) {
                Log.w(TAG, "사용자가 취소함", e)
                postSideEffect(MissionContract.SideEffect.NavigateToGrowth)
            } catch (e: Exception) {
                Log.e(TAG, "AR 초기화 실패", e)
                reduce { state.copy(errorMessage = e.message ?: "AR 초기화 실패", isLoading = false) }
                postSideEffect(MissionContract.SideEffect.ShowToast(e.message ?: "AR 초기화 실패"))
            }
        }
    }

    private suspend fun checkAndInstallArCore(activity: Activity) {
        try {
            val installStatus = ArCoreApk.getInstance().requestInstall(
                activity,
                true,
                ArCoreApk.InstallBehavior.REQUIRED,
                ArCoreApk.UserMessageType.USER_ALREADY_INFORMED
            )

            Log.d(TAG, "ARCore 설치 상태: $installStatus")

            if (installStatus == ArCoreApk.InstallStatus.INSTALL_REQUESTED) {
                Log.w(TAG, "ARCore 설치 요청됨")
                throw UserCanceled
            }
        } catch (e: Exception) {
            Log.e(TAG, "ARCore 설치 확인 실패", e)
            throw e
        }
    }

    private fun checkCameraPermission() {
        val hasPermission = ContextCompat.checkSelfPermission(
            application,
            Manifest.permission.CAMERA
        ) == PackageManager.PERMISSION_GRANTED

        Log.d(TAG, "카메라 권한 상태: $hasPermission")

        if (!hasPermission) {
            Log.w(TAG, "카메라 권한이 없음 - 권한 요청")
            intent {
                postSideEffect(MissionContract.SideEffect.ShowPermissionDialog("AR 기능을 사용하기 위해 카메라 권한이 필요합니다."))
                postSideEffect(MissionContract.SideEffect.RequestCameraPermission)
            }
            throw UserCanceled
        }
    }

    fun startARSession() = intent {
        Log.d(TAG, "startARSession: isArSessionReady=${state.isArSessionReady}")

        if (!state.isArSessionReady) {
            Log.w(TAG, "AR 세션이 준비되지 않음")
            return@intent
        }

        viewModelScope.launch {
            try {
                Log.d(TAG, "AR 세션 시작 중...")
                arManager.startSession()
                Log.d(TAG, "AR 세션 시작 완료")
            } catch (e: Exception) {
                Log.e(TAG, "AR 세션 시작 실패", e)
                postSideEffect(MissionContract.SideEffect.ShowToast("AR 세션 시작 실패: ${e.message}"))
            }
        }
    }

    fun stopARSession() = intent {
        Log.d(TAG, "AR 세션 중지")

        viewModelScope.launch {
            try {
                arManager.stopSession()
                Log.d(TAG, "AR 세션 중지 완료")
            } catch (e: Exception) {
                Log.e(TAG, "AR 세션 중지 실패", e)
                postSideEffect(MissionContract.SideEffect.ShowToast("AR 세션 중지 실패: ${e.message}"))
            }
        }
    }

    fun onActivityResume() {
        Log.d(TAG, "onActivityResume")
        isActivityResumed = true

        // Activity와 SurfaceView가 모두 준비되었을 때만 초기화
        if (activity != null && surfaceView != null) {
            initializeAR()
        }
    }

    fun onActivityPause() {
        Log.d(TAG, "onActivityPause")
        isActivityResumed = false
        stopARSession()
    }

    fun onConfigurationChanged(newConfig: android.content.res.Configuration) {
        Log.d(TAG, "onConfigurationChanged")
        viewModelScope.launch {
            try {
                arManager.handleConfigurationChange()
            } catch (e: Exception) {
                Log.e(TAG, "Configuration 변경 처리 실패", e)
            }
        }
    }

    fun onSurfaceViewReady(activity: Activity, surfaceView: SurfaceView) {
        Log.d(TAG, "onSurfaceViewReady: Activity=${activity::class.simpleName}, SurfaceView=$surfaceView, tag=${surfaceView.tag}")

        if (surfaceView.tag == "UNITY_HIDDEN" || surfaceView.tag != "AR_SURFACE_VIEW") {
            Log.d(TAG, "Unity 또는 비AR SurfaceView - 무시 (tag=${surfaceView.tag})")
            return
        }
        this.activity = activity
        this.surfaceView = surfaceView

        // SurfaceView가 실제로 크기를 가질 때까지 대기
        surfaceView.post {
            if (surfaceView.width > 0 && surfaceView.height > 0) {
                Log.d(TAG, "SurfaceView 크기 확인됨: ${surfaceView.width}x${surfaceView.height}")
                // Activity가 resumed 상태이고 아직 AR이 초기화되지 않았다면 초기화 시작
                if (isActivityResumed && !container.stateFlow.value.isArSessionReady) {
                    Log.d(TAG, "SurfaceView 준비 완료 - AR 초기화 시작")
                    initializeAR()
                }
            } else {
                Log.w(TAG, "SurfaceView 크기가 0: ${surfaceView.width}x${surfaceView.height}")
                // 다시 시도
                surfaceView.postDelayed({
                    onSurfaceViewReady(activity, surfaceView)
                }, 100)
            }
        }
    }

    fun onTouchEvent(motionEvent: android.view.MotionEvent) {
        if (container.stateFlow.value.isArSessionReady) {
            arManager.handleTouchEvent(motionEvent)
        } else {
            Log.d(TAG, "AR 세션이 준비되지 않아 터치 이벤트 무시")
        }
    }

    fun onPermissionResult(requestCode: Int, grantResults: IntArray) = intent {
        Log.d(TAG, "onPermissionResult: requestCode=$requestCode, results=${grantResults.contentToString()}")

        if (requestCode == cameraPermissionRequestCode) {
            if (grantResults.any { it != android.content.pm.PackageManager.PERMISSION_GRANTED }) {
                Log.w(TAG, "카메라 권한 거부됨")
                postSideEffect(MissionContract.SideEffect.ShowToast("카메라 권한이 필요합니다."))
                postSideEffect(MissionContract.SideEffect.NavigateToGrowth)
            } else {
                Log.d(TAG, "카메라 권한 승인됨 - AR 초기화 재시도")
                initializeAR()
            }
        }
    }

    fun onBackClick() = intent {
        postSideEffect(MissionContract.SideEffect.NavigateToGrowth)
    }

    override fun onCleared() {
        super.onCleared()
        Log.d(TAG, "ViewModel onCleared")
        arManager.destroy()
    }
}