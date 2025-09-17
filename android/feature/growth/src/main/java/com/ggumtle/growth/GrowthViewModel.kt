package com.ggumtle.growth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.ggumtle.designsystem.dialog.DialogState
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import org.orbitmvi.orbit.Container
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject
import kotlin.random.Random

@HiltViewModel
class GrowthViewModel @Inject constructor(
) : ViewModel(), ContainerHost<GrowthContract.State, GrowthContract.SideEffect> {

    override val container: Container<GrowthContract.State, GrowthContract.SideEffect> =
        container(GrowthContract.State())

    fun onBackClick() = intent { 
        postSideEffect(GrowthContract.SideEffect.NavigateToHome) 
    }
    
    fun onARClick() = intent {
        postSideEffect(GrowthContract.SideEffect.NavigateToAR)
    }
    
    
    fun onCharacterSwipe(index: Int) = intent {
        //캐릭터 선택
        reduce { state.copy(selectedCharacterIndex = index) }
    }
    
    fun onEnhanceClick() = intent {
        val currentCharacter = state.characters.getOrNull(state.selectedCharacterIndex)
        currentCharacter?.let {
            if (state.dreamCoin >= it.enhancementCost) {
                // 코인 차감
                reduce {
                    state.copy(
                        dreamCoin = state.dreamCoin - it.enhancementCost,
                        isEnhanceButtonEnabled = false
                    )
                }

                // TODO: 서버에서 강화 결과를 받아올 때 이 로직을 서버 응답으로 대체
                // 현재는 로컬에서 랜덤으로 성공/실패 결정
                performEnhancement(it.successRate)
            } else {
                postSideEffect(GrowthContract.SideEffect.ShowToast("꿈코인이 부족합니다"))
            }
        }
    }

    private fun performEnhancement(successRate: Int) {
        viewModelScope.launch {
            // 3초 대기 (유니티 애니메이션 대기)
            delay(3000)

            // TODO: 서버 API 호출하여 강화 결과 받기
            // val result = enhancementRepository.enhance(characterId, ...)

            // 현재는 로컬 랜덤으로 처리
            val isSuccess = Random.nextInt(100) < successRate

            if (isSuccess) {
                // 캐릭터 정보 업데이트 (레벨업, 스탯 증가, 성공확률 감소)
                intent {
                    val currentCharacter = state.characters.getOrNull(state.selectedCharacterIndex)
                    currentCharacter?.let { character ->
                        // 강화 전 정보 저장
                        // TODO: 서버에서 받을 업데이트된 캐릭터 정보
                        // val updatedCharacter = enhancementRepository.getUpdatedCharacter(characterId)

                        // 현재는 로컬에서 계산
                        val updatedCharacter = character.copy(
                            level = character.level + 1,
                            currentStat = character.nextLevelStat,
                            nextLevelStat = character.nextLevelStat + 0.05f, // 다음 레벨 스탯
                            successRate = maxOf(10, character.successRate - 5), // 성공확률 5% 감소 (최소 10%)
                            enhancementCost = (character.enhancementCost * 1.5).toInt() // 강화비용 1.5배 증가
                        )

                        val updatedCharacters = state.characters.toMutableList()
                        updatedCharacters[state.selectedCharacterIndex] = updatedCharacter

                        //강화 성공
                        reduce {
                            state.copy(
                                characters = updatedCharacters,
                                isShowingEnhanceSuccess = true,
                                previousCharacterInfo = character // 강화 전 정보 저장
                            )
                        }
                    }
                }
            } else {
                // 강화 실패 오버레이 표시
                intent {
                    reduce { state.copy(isShowingEnhanceFailure = true) }
                }

                // 강화 실패 오버레이 자동 사라짐
                delay(2000)
                intent {
                    reduce { state.copy(isShowingEnhanceFailure = false) }
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
    fun claimMissionReward(missionId: String) = intent {
        val updatedMissions = state.dailyMission.missions.map { mission ->
            if (mission.id == missionId && mission.isCompleted && !mission.isRewarded) {
                mission.copy(isRewarded = true)
            } else {
                mission
            }
        }

        val rewardedMission = state.dailyMission.missions.find { it.id == missionId }
        rewardedMission?.let {
            // 완료된 미션 수 계산 (보상까지 받은 미션)
            val newCompletedCount = updatedMissions.count { mission -> mission.isCompleted && mission.isRewarded }

            // 보상 지급
            reduce {
                state.copy(
                    dreamCoin = state.dreamCoin + it.reward,
                    dailyMission = state.dailyMission.copy(
                        missions = updatedMissions,
                        completedMissions = newCompletedCount
                    )
                )
            }
            postSideEffect(GrowthContract.SideEffect.ShowToast("${it.reward} 꿈코인을 받았습니다!"))
        }
    }
}