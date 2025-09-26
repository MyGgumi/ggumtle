package com.ggumtle.mission.component.button

import androidx.compose.foundation.*
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.Icon
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
import com.ggumtle.core.designsystem.R

@Composable
fun RemainMissionButton(
    onShowRemainMissionDialog: () -> Unit,
    modifier: Modifier = Modifier
) {
    Box(
        modifier = modifier
            .size(40.dp)
            .background(
                color = Color.Green.copy(alpha = 0.5f),
                shape = CircleShape
            )
            .clickable { onShowRemainMissionDialog() },
        contentAlignment = Alignment.Center
    ) {
        Icon(
            painter = painterResource(R.drawable.btn_remain_mission),
            contentDescription = "남은 미션",
            tint = Color.White,
            modifier = Modifier.size(24.dp)
        )
    }
}