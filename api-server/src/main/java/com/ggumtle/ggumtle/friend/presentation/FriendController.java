package com.ggumtle.ggumtle.friend.presentation;

import com.ggumtle.ggumtle.common.SocketCommandHandler;
import com.ggumtle.ggumtle.common.SocketRequestType;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.errorCode.FriendErrorCode;
import com.ggumtle.ggumtle.friend.application.FriendService;
import com.ggumtle.ggumtle.friend.application.command.GetFriendRequestsCommand;
import com.ggumtle.ggumtle.friend.application.command.GetFriendsCommand;
import com.ggumtle.ggumtle.friend.application.command.RequestFriendCommand;
import com.ggumtle.ggumtle.friend.application.result.GetFriendRequestsResult;
import com.ggumtle.ggumtle.friend.application.result.GetFriendsResult;
import com.ggumtle.ggumtle.friend.application.result.RequestFriendsResult;
import com.ggumtle.ggumtle.friend.presentation.request.RequestFriendRequest;
import com.ggumtle.ggumtle.friend.presentation.response.GetFriendRequestsResponse;
import com.ggumtle.ggumtle.friend.presentation.response.GetFriendsResponse;
import com.ggumtle.ggumtle.friend.presentation.response.RequestFriendResponse;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketSession;

import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class FriendController {
    private final FriendService friendService;
    private final ApplicationEventPublisher applicationEventPublisher;

    @SocketCommandHandler(type = SocketRequestType.REQUEST_FRIEND)
    public void requestFriend(RequestFriendRequest request, WebSocketSession session) {
        if (request.targetMemberId() == null) {
            throw new GgumtleException(FriendErrorCode.TARGET_REQUIRED);
        }

        Long requesterId = Long.parseLong(session.getPrincipal().getName());
        RequestFriendCommand command = request.toCommand(requesterId);

        RequestFriendsResult result = friendService.requestFriend(command);
        RequestFriendResponse response = RequestFriendResponse.from(result);

        SendSocketEvent event = new SendSocketEvent(List.of(requesterId,result.targetMemberId()),response);
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketRequestType.GET_FRIENDS)
    public void getFriends(WebSocketSession session) {
        Long memberId = Long.parseLong(session.getPrincipal().getName());
        GetFriendsCommand command = new GetFriendsCommand(memberId);

        GetFriendsResult result = friendService.getFriends(command);

        GetFriendsResponse response = GetFriendsResponse.from(result);
        SendSocketEvent event = new SendSocketEvent(List.of(memberId), response);
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketRequestType.GET_FRIEND_REQUESTS)
    public void getFriendRequests(WebSocketSession session) {
        Long memberId = Long.parseLong(session.getPrincipal().getName());
        GetFriendRequestsCommand command = new GetFriendRequestsCommand(memberId);

        GetFriendRequestsResult result = friendService.getFriendRequests(command);

        GetFriendRequestsResponse response = GetFriendRequestsResponse.from(result);
        SendSocketEvent event = new SendSocketEvent(List.of(memberId), response);
        applicationEventPublisher.publishEvent(event);
    }
}
