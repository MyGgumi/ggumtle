package com.ggumtle.social.component

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.Dialog
import com.ggumtle.designsystem.component.GameSearchBar
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.social.model.User

@Composable
fun UserSearchDialog(
    searchQuery: TextFieldValue,
    searchResults: List<User>,
    isLoading: Boolean,
    onUpdateSearchQuery: (TextFieldValue) -> Unit,
    onSendFriendRequest: (Long) -> Unit,
    onOpenProfile: (Long) -> Unit,
    onDismiss: () -> Unit
) {
    Dialog(onDismissRequest = onDismiss) {
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .height(500.dp),
            colors = CardDefaults.cardColors(containerColor = GameColors.background),
            shape = RoundedCornerShape(20.dp)
        ) {
            Column(
                modifier = Modifier.padding(20.dp)
            ) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text(
                        text = "플레이어 검색",
                        color = GameColors.textPrimary,
                        fontSize = 20.sp,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier.weight(1f)
                    )

                    IconButton(onClick = onDismiss) {
                        Icon(
                            Icons.Default.Close,
                            contentDescription = "닫기",
                            tint = GameColors.textSecondary
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                GameSearchBar(
                    query = searchQuery,
                    onQueryChange = onUpdateSearchQuery,
                    placeholder = "플레이어 이름으로 검색...",
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                if (searchQuery.text.isNotEmpty()) {
                    if (isLoading) {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(200.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            CircularProgressIndicator(
                                color = GameColors.primary,
                                strokeWidth = 3.dp
                            )
                        }
                    } else if (searchResults.isEmpty()) {
                        Box(
                            modifier = Modifier
                                .fillMaxWidth()
                                .height(200.dp),
                            contentAlignment = Alignment.Center
                        ) {
                            Column(
                                horizontalAlignment = Alignment.CenterHorizontally
                            ) {
                                Icon(
                                    Icons.Default.Search,
                                    contentDescription = null,
                                    tint = GameColors.textSecondary,
                                    modifier = Modifier.size(48.dp)
                                )
                                Spacer(modifier = Modifier.height(16.dp))
                                Text(
                                    text = "검색 결과가 없습니다",
                                    color = GameColors.textSecondary,
                                    fontSize = 16.sp
                                )
                            }
                        }
                    } else {
                        LazyColumn(
                            modifier = Modifier.heightIn(max = 400.dp),
                            verticalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            items(searchResults) { user ->
                                GameUserSearchItem(
                                    user = user,
                                    onSendRequest = {
                                        onSendFriendRequest(user.id)
                                    },
                                    onOpenProfile = {
                                        onOpenProfile(user.id)
                                    }
                                )
                            }
                        }
                    }
                } else {
                    Box(
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(200.dp),
                        contentAlignment = Alignment.Center
                    ) {
                        Column(
                            horizontalAlignment = Alignment.CenterHorizontally
                        ) {
                            Icon(
                                Icons.Default.PersonSearch,
                                contentDescription = null,
                                tint = GameColors.textSecondary,
                                modifier = Modifier.size(48.dp)
                            )
                            Spacer(modifier = Modifier.height(16.dp))
                            Text(
                                text = "플레이어 이름을 입력하세요",
                                color = GameColors.textSecondary,
                                fontSize = 16.sp
                            )
                        }
                    }
                }
            }
        }
    }
}