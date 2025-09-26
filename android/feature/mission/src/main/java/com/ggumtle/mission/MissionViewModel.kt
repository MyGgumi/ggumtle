package com.ggumtle.mission

import android.content.Context
import android.content.res.Configuration
import android.graphics.Bitmap
import android.util.Log
import android.view.MotionEvent
import android.view.SurfaceView
import androidx.lifecycle.ViewModel
import com.ggumtle.mission.manager.ArManager
import dagger.hilt.android.lifecycle.HiltViewModel
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject
import androidx.compose.ui.geometry.Offset
import com.ggumtle.mission.manager.CaptureManager
import com.ggumtle.mission.manager.FeedingManager
import com.ggumtle.mission.manager.ShakeDetectionManager
import kotlinx.coroutines.flow.launchIn
import kotlinx.coroutines.flow.onEach
import androidx.lifecycle.viewModelScope
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.usecase.mission.GetMissionListUseCase
import com.ggumtle.domain.rest.usecase.mission.ProgressMissionUseCase
import com.ggumtle.mission.model.toBeforeSuccessMission

@HiltViewModel
class MissionViewModel @Inject constructor(
    private val arManager: ArManager,
    private val captureManager: CaptureManager,
    private val feedingManager: FeedingManager,
    private val shakeDetectionManager: ShakeDetectionManager,
    private val getMissionListUseCase: GetMissionListUseCase,
    private val progressMissionUseCase: ProgressMissionUseCase,
) : ContainerHost<MissionContract.State, MissionContract.SideEffect>, ViewModel() {

    companion object {
        private const val TAG = "MissionViewModel"
    }

    override val container =
        container<MissionContract.State, MissionContract.SideEffect>(initialState = MissionContract.State())

    init {
        initShake()
        loadMission()
    }

    fun loadMission() = intent{
        getMissionListUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    reduce {
                        state.copy(
                            beforeSuccessMissions = resource.data.missions
                                .filter { it.state == "BEFORE_SUCCESS" }
                                .map { it.toBeforeSuccessMission() }
                        )
                    }
                }
                is Resource.Failure -> reduce { state.copy(isLoading = false) }
            }
        }
    }

    fun initShake(){
        shakeDetectionManager.shakeDetected
            .onEach { isShakeDetected ->
                if (isShakeDetected) {
                    onShakeDetected()
                }
            }
            .launchIn(viewModelScope)
    }

    fun initAR(context: Context) = intent {
        Log.d(TAG, "initializeAR: AR 초기화 시작")
        reduce { state.copy(isLoading = true) }
        try {
            arManager.initialize(context)
            // 흔들기 감지 초기화 및 시작
            shakeDetectionManager.initialize(context)
            shakeDetectionManager.startDetection()
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

    fun startUx() = intent {
        Log.d(TAG, "initializeAR: AR 시작")
        reduce { state.copy(isLoading = true, isModelPlacementReady = true) }
        try {
            arManager.startUx()
            reduce { state.copy(isLoading = false) }
//            checkModelPlacementReady()
        } catch (e: Exception) {
            reduce {
                state.copy(
                    isLoading = false,
                    error = e.toString()
                )
            }
        }
    }

    private fun checkModelPlacementReady() = intent {
        // 주기적으로 평면 감지 상태를 확인
        kotlinx.coroutines.delay(100) // 1초 후부터 확인 시작

        while (!state.isModelPlacementReady) {
            if (arManager.hasDetectedPlanes()) {
                reduce { state.copy(isModelPlacementReady = true) }
                Log.d(TAG, "평면 감지 완료 - 모델 배치 준비됨")
                break
            }
            kotlinx.coroutines.delay(500) // 0.5초마다 재확인
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

    fun setSurfaceView(surfaceView: SurfaceView) {
        arManager.setSurfaceView(surfaceView)
        captureManager.setSurfaceView(surfaceView)
    }

    fun onBackClick() = intent {
        postSideEffect(MissionContract.SideEffect.NavigateToGrowth)
    }

    fun progressMission(missionId: Long) = intent {
        val successMissionId = state.beforeSuccessMissions.find { it.missionId==missionId }?.memberMissionId
        if(successMissionId==null)return@intent
        progressMissionUseCase.invoke(successMissionId).collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    if(resource.data.requiredCount == resource.data.doneCount){
                        reduce {
                            state.copy(
                                beforeSuccessMissions = state.beforeSuccessMissions.filter { it.missionId != missionId },
                                isLoading = false
                            )
                        }
                    } else {
                        reduce { state.copy(isLoading = false) }
                    }
                }
                is Resource.Failure -> reduce { state.copy(isLoading = false) }
            }
        }
    }

    fun capturePhoto(context: Context) = intent {
        reduce { state.copy(isCapturing = true) }
        try {
            val bitmap = captureManager.captureSurfaceView()
            val imageUri = captureManager.saveImageToGallery(context, bitmap)
            val detectModel = detectModelInImage(bitmap)

            if(detectModel){
                progressMission(1)
                Log.d(TAG, "몽깅이랑 사진찍기 성공!")
            }

            reduce {
                state.copy(
                    isCapturing = false,
                    lastCapturedImageUri = imageUri.toString(),
                    hasModelInLastImage = detectModel
                )
            }
        } catch (e: Exception) {
            reduce { state.copy(isCapturing = false) }
        }
    }

    private fun detectModelInImage(bitmap: Bitmap): Boolean {
        // 1. 먼저 모델이 렌더링되고 있는지 확인
        if (!arManager.hasActiveModels()) return false

        // 2. 비트맵 분석을 통한 모델 감지 AR 화면의 중앙 영역에서 픽셀 변화를 감지
        return captureManager.analyzeImageForModel(bitmap)
    }

    fun showImageDialog() = intent {
        reduce { state.copy(showImageDialog = true) }
    }

    fun hideImageDialog() = intent {
        reduce { state.copy(showImageDialog = false) }
    }

    fun showRemainMissionDialog() = intent {
        reduce { state.copy(showRemainMissionDialog = true) }
    }

    fun hideRemainMissionDialog() = intent {
        reduce { state.copy(showRemainMissionDialog = false) }
    }

    fun showEmoticonSelector() = intent {
        reduce { state.copy(showEmoticonSelector = !state.showEmoticonSelector) }
    }

    fun hideEmoticonSelector() = intent {
        reduce { state.copy(showEmoticonSelector = false) }
    }

    fun playEmoticonAnimation(animationIndex: Int) = intent {
        if (arManager.hasActiveModels()) {
            arManager.playAnimation(animationIndex)
            Log.d(TAG, "이모티콘 애니메이션 실행: $animationIndex")
        }
        reduce { state.copy(showEmoticonSelector = false) }
    }

    fun startFoodDrag() = intent {
        reduce {
            state.copy(
                isDraggingFood = true,
                foodPosition = null
            )
        }
    }

    fun updateFoodPosition(offset: Offset) = intent {
        if (state.isDraggingFood) {
            val currentPosition = state.foodPosition
            val newPosition = feedingManager.updatePosition(currentPosition, offset)
            reduce {
                state.copy(foodPosition = newPosition)
            }
        }
    }

    fun endFoodDrag() = intent {
        if (state.isDraggingFood) {
            val finalPosition = state.foodPosition
            val modelScreenPosition = arManager.getModelScreenPosition()

            val fedCharacter = feedingManager.checkCharacterCollision(
                finalPosition,
                modelScreenPosition
            )

            if (fedCharacter && arManager.hasActiveModels()) {
                arManager.playAnimation(1)
                reduce {
                    state.copy(
                        isDraggingFood = false,
                        foodPosition = null
                    )
                }
                progressMission(2)
                Log.d(TAG, "먹이주기 성공!")
                postSideEffect(MissionContract.SideEffect.ShowToast("몽깅이에게 먹이를 주었습니다! 🎉"))
            } else {
                arManager.playAnimation(10)
                reduce {
                    state.copy(
                        isDraggingFood = false,
                        foodPosition = null
                    )
                }
            }
        }
    }

    private fun onShakeDetected() = intent {
        if(arManager.hasActiveModels()){
            progressMission(3)
            Log.d(TAG, "인사하기 성공!")
            postSideEffect(MissionContract.SideEffect.ShowToast("인사하기 성공! 🎉"))
            arManager.playAnimation(14)
        }
        shakeDetectionManager.resetShakeDetected()
    }

    override fun onCleared() {
        super.onCleared()
        arManager.clearManager()
        shakeDetectionManager.stopDetection()
        Log.d(TAG, "onCleared: 뷰모델 종료")
    }
}