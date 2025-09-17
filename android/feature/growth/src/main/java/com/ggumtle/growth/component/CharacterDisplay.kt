package com.ggumtle.growth.component

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ColorFilter
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.ggumtle.core.designsystem.R
import com.ggumtle.designsystem.theme.BrandColors

@Composable
fun CharacterDisplay(
    characterName: String,
    selectedIndex: Int,
    totalCharacters: Int,
    onSwipe: (Int) -> Unit,
    modifier: Modifier = Modifier
) {
    Column(
        modifier = modifier,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = characterName,
            color = Color.White,
            style = MaterialTheme.typography.titleLarge,
            textAlign = TextAlign.Center
        )
        
        Spacer(modifier = Modifier.height(16.dp))
        
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceEvenly,
            verticalAlignment = Alignment.CenterVertically
        ) {
            IconButton(
                onClick = {
                    val newIndex = if (selectedIndex > 0) selectedIndex - 1 else totalCharacters - 1
                    onSwipe(newIndex)
                },
                modifier = Modifier.size(80.dp)
            ) {
                Image(
                    painter = painterResource(id = R.drawable.ic_left_arrow),
                    contentDescription = "이전",
                    colorFilter = ColorFilter.tint(Color.White),
                    modifier = Modifier.size(60.dp)
                )
            }
            
            Box(
                modifier = Modifier
                    .size(200.dp),
                //contentAlignment = Alignment.Center
            ) {
//                Text(
//                    text = "😎",
//                    fontSize = 80.sp
//                )
            }
            
            IconButton(
                onClick = {
                    val newIndex = if (selectedIndex < totalCharacters - 1) selectedIndex + 1 else 0
                    onSwipe(newIndex)
                },
                modifier = Modifier.size(80.dp)
            ) {
                Image(
                    painter = painterResource(id = R.drawable.ic_right_arrow),
                    contentDescription = "다음",
                    colorFilter = ColorFilter.tint(Color.White),
                    modifier = Modifier.size(60.dp)
                )
            }
        }
    }
}

@Preview(showBackground = true, backgroundColor = 0xFF1A1A2E)
@Composable
fun CharacterDisplayPreview() {
    CharacterDisplay(
        characterName = "힐러 몽깅이",
        selectedIndex = 0,
        totalCharacters = 3,
        onSwipe = {}
    )
}