package com.ggumtle.social.component

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.TextFieldValue
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.designsystem.component.GameCard
import com.example.designsystem.component.GameSearchBar
import com.example.designsystem.theme.GameColors
import com.example.domain.websocket.model.Friend

@Composable
fun FriendsTabContent(
    friends: List<Friend>,
    searchQuery: TextFieldValue,
    isLoading: Boolean,
    onUpdateSearchQuery: (TextFieldValue) -> Unit,
    onOpenProfile: (Long) -> Unit,
    onRemoveFriend: (Long) -> Unit,
) {
    var onlineFriendsExpanded by remember { mutableStateOf(true) }
    var offlineFriendsExpanded by remember { mutableStateOf(true) }

    val filteredFriends = remember(friends, searchQuery) {
        friends.filter { friend ->
            if (searchQuery.text.isBlank()) true
            else friend.nickname.contains(searchQuery.text, ignoreCase = true)
        }
    }

    val onlineFriends = remember(filteredFriends) {
        filteredFriends.filter { it.isOnline }
    }
    val offlineFriends = remember(filteredFriends) {
        filteredFriends.filter { !it.isOnline }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 20.dp)
    ) {
        GameSearchBar(
            query = searchQuery,
            onQueryChange = onUpdateSearchQuery,
            placeholder = "친구 이름으로 검색..."
        )

        Spacer(modifier = Modifier.height(24.dp))

        if (isLoading) {
            Box(
                modifier = Modifier.fillMaxWidth().padding(40.dp),
                contentAlignment = Alignment.Center
            ) {
                CircularProgressIndicator(
                    color = GameColors.primary,
                    strokeWidth = 3.dp
                )
            }
        } else {
            LazyColumn(
                verticalArrangement = Arrangement.spacedBy(12.dp),
                contentPadding = PaddingValues(bottom = 20.dp)
            ) {
                if (searchQuery.text.isNotEmpty() && filteredFriends.isEmpty()) {
                    item {
                        GameCard {
                            Box(
                                modifier = Modifier
                                    .fillMaxWidth()
                                    .padding(24.dp),
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text = "검색 결과가 없습니다",
                                    color = GameColors.textSecondary,
                                    fontSize = 16.sp
                                )
                            }
                        }
                    }
                } else {
                    if (onlineFriends.isNotEmpty()) {
                        item {
                            GameSectionHeader(
                                title = "온라인",
                                count = onlineFriends.size,
                                isExpanded = onlineFriendsExpanded,
                                onToggle = { onlineFriendsExpanded = !onlineFriendsExpanded },
                                color = GameColors.online
                            )
                        }

                        if (onlineFriendsExpanded) {
                            items(onlineFriends) { friend ->
                                GameFriendItem(
                                    friend = friend,
                                    onOpenProfile = { onOpenProfile(friend.id) },
                                    onRemoveFriend = { onRemoveFriend(friend.id) }
                                )
                            }
                        }
                    }

                    if (offlineFriends.isNotEmpty()) {
                        item {
                            GameSectionHeader(
                                title = "오프라인",
                                count = offlineFriends.size,
                                isExpanded = offlineFriendsExpanded,
                                onToggle = { offlineFriendsExpanded = !offlineFriendsExpanded },
                                color = GameColors.offline
                            )
                        }

                        if (offlineFriendsExpanded) {
                            items(offlineFriends) { friend ->
                                GameFriendItem(
                                    friend = friend,
                                    onOpenProfile = { onOpenProfile(friend.id) },
                                    onRemoveFriend = { onRemoveFriend(friend.id) }
                                )
                            }
                        }
                    }
                }
            }
        }
    }
}