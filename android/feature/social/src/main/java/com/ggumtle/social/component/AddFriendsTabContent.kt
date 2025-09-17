package com.ggumtle.social.component

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Search
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.ggumtle.designsystem.component.GameCard
import com.ggumtle.designsystem.component.GameSearchBar
import com.ggumtle.designsystem.component.GameSearchBarClickable
import com.ggumtle.designsystem.theme.GameColors
import com.ggumtle.domain.websocket.model.FriendRequest
import com.example.domain.websocket.model.SentRequest

@Composable
fun AddFriendsTabContent(
    receivedRequests: List<FriendRequest>,
    sentRequests: List<SentRequest>,
    isLoading: Boolean,
    onAcceptFriendRequest: (Long) -> Unit,
    onRejectFriendRequest: (Long) -> Unit,
    onCancelSentRequest: (Long) -> Unit,
    onOpenProfile: (Long) -> Unit,
    onShowSearchDialog: () -> Unit,
) {
    var receivedRequestsExpanded by remember { mutableStateOf(true) }
    var sentRequestsExpanded by remember { mutableStateOf(true) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(horizontal = 20.dp)
    ) {

        GameSearchBarClickable(
            placeholder = "플레이어 이름으로 검색...",
            onClick = { onShowSearchDialog() }
        )

        Spacer(modifier = Modifier.height(24.dp))

        LazyColumn(
            verticalArrangement = Arrangement.spacedBy(12.dp),
            contentPadding = PaddingValues(bottom = 20.dp)
        ) {
            if (receivedRequests.isNotEmpty()) {
                item {
                    GameSectionHeader(
                        title = "받은 요청",
                        count = receivedRequests.size,
                        isExpanded = receivedRequestsExpanded,
                        onToggle = { receivedRequestsExpanded = !receivedRequestsExpanded },
                        color = GameColors.primary
                    )
                }

                if (receivedRequestsExpanded) {
                    items(receivedRequests) { request ->
                        GameFriendRequestItem(
                            request = request,
                            onAccept = { onAcceptFriendRequest(request.friendRequestId) },
                            onReject = { onRejectFriendRequest(request.friendRequestId) }
                        )
                    }
                }
            }

            if (sentRequests.isNotEmpty()) {
                item {
                    GameSectionHeader(
                        title = "보낸 요청",
                        count = sentRequests.size,
                        isExpanded = sentRequestsExpanded,
                        onToggle = { sentRequestsExpanded = !sentRequestsExpanded },
                        color = GameColors.primary
                    )
                }

                if (sentRequestsExpanded) {
                    items(sentRequests) { request ->
                        GameSentRequestItem(
                            request = request,
                            onCancel = { onCancelSentRequest(request.id) },
                            onOpenProfile = { onOpenProfile(request.toUserId) }
                        )
                    }
                }
            }
        }
    }
}