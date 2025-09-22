package com.ggumtle.mission

import android.Manifest
import android.app.Application
import android.content.Context
import android.content.pm.PackageManager
import android.content.res.Configuration
import android.util.Log
import android.view.MotionEvent
import android.view.SurfaceView
import androidx.core.content.ContextCompat
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ggumtle.mission.ar.ArManager
import com.ggumtle.mission.ar.util.UserCanceled
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
    private val arManager: ArManager
) : ContainerHost<MissionContract.State, MissionContract.SideEffect>, ViewModel() {

    companion object {
        private const val TAG = "MissionViewModel"
    }

    override val container =
        container<MissionContract.State, MissionContract.SideEffect>(initialState = MissionContract.State())

    fun initializeAR(context: Context) = intent {
        Log.d(TAG, "initializeAR: AR 초기화 시작")
        reduce { state.copy(isLoading = true) }
        try {
            arManager.initialize(context)
            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce {
                state.copy(
                    isLoading = false,
                    error = e.toString()
                )
            }
        }
    }

    fun onConfigurationChanged(newConfig: Configuration) {
        arManager.onConfigurationChanged(newConfig)
    }

    fun pauseArSession() {
        arManager.pauseArSession()
    }

    fun resumeArSession() {
        arManager.resumeArSession()
    }

    fun startUx() = intent{
        Log.d(TAG, "initializeAR: AR 시작")
        reduce { state.copy(isLoading = true) }
        try {
            arManager.startUx()
            reduce { state.copy(isLoading = false) }
        } catch (e: Exception) {
            reduce {
                state.copy(
                    isLoading = false,
                    error = e.toString()
                )
            }
        }
    }

    fun cancelCreateScope() {
        arManager.cancelCreateScope()
    }

    fun cancelStartScope() {
        arManager.cancelStartScope()
    }

    fun onTouchEvent(context: Context, motionEvent: MotionEvent) =
        arManager.handleTouchEvent(context, motionEvent)

    fun setSurfaceView(surfaceView: SurfaceView) =
        arManager.setSurfaceView(surfaceView)

    fun onBackClick() = intent {
        postSideEffect(MissionContract.SideEffect.NavigateToGrowth)
    }

    override fun onCleared() {
        super.onCleared()
        arManager.clearManager()
        Log.d(TAG, "onCleared: 뷰모델 종료")
    }
}