// components/ProfileDialog.kt
package com.ggumtle.home.component

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.example.designsystem.component.button.GameIconButton
import com.example.designsystem.theme.GameColors
import com.ggumtle.home.HomeContract

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProfileDialog(
    userProfile: HomeContract.UserProfile,
    isNicknameEditMode: Boolean,
    tempNickname: String,
    onDismiss: () -> Unit,
    onEditNicknameClick: () -> Unit,
    onNicknameTextChange: (String) -> Unit,
    onSaveNickname: () -> Unit,
    onCancelNicknameEdit: () -> Unit
) {
    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            colors = CardDefaults.cardColors(
                containerColor = Color(0xFF1A1D2E)
            ),
            shape = RoundedCornerShape(20.dp)
        ) {
            Column(
                modifier = Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                // 헤더
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "프로필",
                        color = Color(0xFF85C1E9),
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold
                    )

                    IconButton(onClick = onDismiss) {
                        Icon(
                            Icons.Default.Close,
                            contentDescription = "닫기",
                            tint = GameColors.textSecondary
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))

                // 프로필 이미지
                Box(
                    modifier = Modifier
                        .size(80.dp)
                        .clip(CircleShape)
                        .background(
                            brush = Brush.radialGradient(
                                colors = listOf(
                                    GameColors.primary,
                                    GameColors.primaryLight
                                )
                            )
                        ),
                    contentAlignment = Alignment.Center
                ) {
                    if (userProfile.profileImageUrl != null) {
                        // TODO: 프로필 이미지 로드
                        Icon(
                            Icons.Default.Person,
                            contentDescription = "프로필",
                            tint = Color.White,
                            modifier = Modifier.size(40.dp)
                        )
                    } else {
                        Text(
                            text = userProfile.nickname.first().toString(),
                            color = Color.White,
                            fontSize = 32.sp,
                            fontWeight = FontWeight.Bold
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))

                // 닉네임 섹션
                if (isNicknameEditMode) {
                    // 닉네임 편집 모드
                    Column {
                        OutlinedTextField(
                            value = tempNickname,
                            onValueChange = onNicknameTextChange,
                            label = { Text("닉네임", color = GameColors.textSecondary) },
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedBorderColor = GameColors.primary,
                                unfocusedBorderColor = GameColors.textSecondary,
                                focusedTextColor = Color.White,
                                unfocusedTextColor = Color.White
                            ),
                            shape = RoundedCornerShape(12.dp),
                            modifier = Modifier.fillMaxWidth()
                        )

                        Spacer(modifier = Modifier.height(16.dp))

                        Row(
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            GameIconButton(
                                onClick = onCancelNicknameEdit,
                                icon = Icons.Default.Close,
                                contentDescription = "취소",
                                backgroundColor = GameColors.warning,
                                size = 40
                            )

                            GameIconButton(
                                onClick = onSaveNickname,
                                icon = Icons.Default.Check,
                                contentDescription = "저장",
                                backgroundColor = GameColors.success,
                                size = 40
                            )
                        }
                    }
                } else {
                    // 닉네임 표시 모드
                    Row(
                        modifier = Modifier.fillMaxWidth(),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Column {
                            Text(
                                text = "닉네임",
                                color = GameColors.textSecondary,
                                fontSize = 12.sp
                            )
                            Text(
                                text = userProfile.nickname,
                                color = Color.White,
                                fontSize = 18.sp,
                                fontWeight = FontWeight.Medium
                            )
                        }

                        GameIconButton(
                            onClick = onEditNicknameClick,
                            icon = Icons.Default.Edit,
                            contentDescription = "편집",
                            backgroundColor = GameColors.primary,
                            size = 40
                        )
                    }
                }

                Spacer(modifier = Modifier.height(24.dp))

                // 사용자 ID (읽기 전용)
                Column(
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(
                        text = "사용자 ID",
                        color = GameColors.textSecondary,
                        fontSize = 12.sp
                    )
                    Text(
                        text = userProfile.id.ifEmpty { "user_${System.currentTimeMillis() % 10000}" },
                        color = GameColors.textSecondary,
                        fontSize = 14.sp,
                        fontWeight = FontWeight.Medium
                    )
                }
            }
        }
    }
}