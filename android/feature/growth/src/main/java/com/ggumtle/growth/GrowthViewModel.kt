package com.ggumtle.growth

import androidx.lifecycle.ViewModel
import com.ggumtle.designsystem.dialog.DialogState
import dagger.hilt.android.lifecycle.HiltViewModel
import org.orbitmvi.orbit.Container
import org.orbitmvi.orbit.ContainerHost
import org.orbitmvi.orbit.syntax.simple.intent
import org.orbitmvi.orbit.syntax.simple.postSideEffect
import org.orbitmvi.orbit.syntax.simple.reduce
import org.orbitmvi.orbit.viewmodel.container
import javax.inject.Inject

@HiltViewModel
class GrowthViewModel @Inject constructor(
) : ViewModel(), ContainerHost<GrowthContract.State, GrowthContract.SideEffect> {

    override val container: Container<GrowthContract.State, GrowthContract.SideEffect> =
        container(GrowthContract.State())

    fun onBackClick() = intent { 
        postSideEffect(GrowthContract.SideEffect.NavigateToHome) 
    }

    fun hideDialog() = intent { 
        reduce { state.copy(dialogState = DialogState.Hidden) } 
    }
}