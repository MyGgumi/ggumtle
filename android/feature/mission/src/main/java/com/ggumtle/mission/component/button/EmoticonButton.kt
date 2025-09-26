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
import androidx.compose.foundation.layout.*
import com.ggumtle.mission.component.EmoticonSelector

@Composable
fun EmoticonButton(
    showEmoticonSelector: Boolean,
    onShowEmoticonSelector: () -> Unit,
    onEmoticonSelected: (Int) -> Unit,
    onHideEmoticonSelector: () -> Unit,
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier,
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        // 이모티콘 선택기를 왼쪽에 배치
        if (showEmoticonSelector) {
            EmoticonSelector(
                onEmoticonSelected = onEmoticonSelected,
                onDismiss = onHideEmoticonSelector
            )
        }

        // 이모티콘 버튼
        Box(
            modifier = Modifier
                .size(40.dp)
                .background(
                    color = Color(0xFFD08E00).copy(alpha = 0.9f),
                    shape = CircleShape
                )
                .clickable { onShowEmoticonSelector() },
            contentAlignment = Alignment.Center
        ) {
            Icon(
                painter = painterResource(R.drawable.btn_emoticon),
                contentDescription = "이모티콘",
                tint = Color.White,
                modifier = Modifier.size(24.dp)
            )
        }
    }
}