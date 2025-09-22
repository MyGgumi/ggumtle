package com.ggumtle.growth

import androidx.lifecycle.ViewModel
import com.ggumtle.domain.unity.UnitySendManager
import com.ggumtle.designsystem.dialog.DialogState
import com.ggumtle.domain.rest.model.Resource
import com.ggumtle.domain.rest.usecase.growth.EnhanceMonggingUseCase
import com.ggumtle.domain.rest.usecase.growth.GetMonggingDetailUseCase
import com.ggumtle.domain.rest.usecase.growth.GetMonggingListUseCase
import com.ggumtle.domain.rest.usecase.member.GetMemberCoinUseCase
import com.ggumtle.domain.rest.usecase.mission.GetMissionListUseCase
import com.ggumtle.growth.model.toCharacterInfo
import com.ggumtle.growth.model.toDailyMission
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import org.orbitmvi.orbit.Container
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject
import kotlin.let

@HiltViewModel
class GrowthViewModel @Inject constructor(
    private val unitySendManager: UnitySendManager,
    private val getMemberCoinUseCase: GetMemberCoinUseCase,
    private val getMonggingListUseCase: GetMonggingListUseCase,
    private val getMonggingDetailUseCase: GetMonggingDetailUseCase,
    private val enhanceMonggingUseCase: EnhanceMonggingUseCase,
    private val getMissionListUseCase: GetMissionListUseCase
) : ViewModel(), ContainerHost<GrowthContract.State, GrowthContract.SideEffect> {

    override val container: Container<GrowthContract.State, GrowthContract.SideEffect> =
        container(GrowthContract.State())

    init {
        loadCoin()
        loadMyMonggingData()
        loadMission()
    }

    private fun loadMission() = intent {
        getMissionListUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    reduce {
                        state.copy(
                            dailyMission = resource.data.toDailyMission()
                        )
                    }
                }
                is Resource.Failure -> reduce { state.copy(isLoading = false) }
            }
        }
    }

    // 내 몽깅이 정보 조회
    fun loadMyMonggingData() = intent {
        getMonggingListUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> {
                    resource.data.monggings.forEach {
                        getMonggingDetailUseCase.invoke(it.id).collect { resource ->
                            when (resource) {
                                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                                is Resource.Success -> {
                                    val monggingDetail = resource.data.toCharacterInfo()
                                    reduce { state.copy(myCharacters = state.myCharacters + monggingDetail) }
                                }

                                is Resource.Failure -> reduce { state.copy(isLoading = false) }
                            }
                        }
                    }
                }

                is Resource.Failure -> reduce { state.copy(isLoading = false) }
            }
        }
    }

    // 코인 정보 조회
    fun loadCoin() = intent {
        getMemberCoinUseCase.invoke().collect { resource ->
            when (resource) {
                is Resource.Loading -> reduce { state.copy(isLoading = true) }
                is Resource.Success -> reduce { state.copy(dreamCoin = resource.data.coin) }
                is Resource.Failure -> reduce { state.copy(isLoading = false) }
            }
        }
    }

    // 대기방으로 돌아가기
    fun onBackClick() = intent {
        reduce { state.copy(isNavigating = true) }
        unitySendManager.goToHomeFromGrowth()
        delay(1100)
        postSideEffect(GrowthContract.SideEffect.NavigateToHome)
    }

    // AR 화면으로 이동
    fun onARClick() = intent { postSideEffect(GrowthContract.SideEffect.NavigateToAR) }


    // 육성 캐릭터 변경
    fun onCharacterSwipe(index: Int, isNext: Boolean) = intent {
        if (isNext) unitySendManager.changeGrowthCharacterTypeNext()
        else unitySendManager.changeGrowthCharacterTypePrevious()
        reduce { state.copy(selectedCharacterIndex = index) }
    }

    // 강화
    fun onEnhanceClick() = intent {
        val currentCharacter = state.myCharacters.getOrNull(state.selectedCharacterIndex)

        currentCharacter?.let {
            enhanceMonggingUseCase.invoke(it.id).collect { resource ->
                when (resource) {
                    is Resource.Loading -> reduce { state.copy(isLoading = true) }
                    is Resource.Success -> {
                        when (resource.data.isSuccess) {
                            true -> {
                                unitySendManager.playEnhanceSuccessEffect()
                                delay(6600)
                            }

                            false -> {
                                unitySendManager.playEnhanceFailEffect()
                                delay(3600)
                                reduce { state.copy(isShowingEnhanceFailure = true) }
                                delay(2000)
                                reduce { state.copy(isShowingEnhanceFailure = false) }
                            }
                        }
                    }

                    is Resource.Failure -> reduce { state.copy(isLoading = false) }
                }
            }
        }
    }

    //다이얼로그 숨기기
    fun hideDialog() = intent {
        reduce { state.copy(dialogState = DialogState.Hidden) }
    }

    //강화 성공 다이얼로그 숨기기
    fun hideEnhanceSuccessDialog() = intent {
        reduce { state.copy(isShowingEnhanceSuccess = false) }
    }

    //일일 미션 다이얼로그 표시
    fun showDailyMissionDialog() = intent {
        reduce { state.copy(isShowingDailyMission = true) }
    }

    //일일 미션 다이얼로그 숨기기
    fun hideDailyMissionDialog() = intent {
        reduce { state.copy(isShowingDailyMission = false) }
    }

    //미션 보상 받기
    fun claimMissionReward(missionId: Long) = intent {

    }
}