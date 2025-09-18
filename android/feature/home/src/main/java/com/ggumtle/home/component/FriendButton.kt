package com.ggumtle.home.component

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.TrendingUp
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.unit.dp
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.theme.GameColors

@Composable
fun FriendButton(
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    IconButton(
        onClick = onClick,
        modifier = Modifier
            .padding(start = 16.dp)
            .size(60.dp)
    ) {
        Icon(
            painter = painterResource(id = R.drawable.btn_friend),
            contentDescription = "친구",
            tint = Color.Unspecified,
            modifier = Modifier.size(60.dp)
        )
    }
}