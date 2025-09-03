package com.ggumtle.ggumtle.friend.presentation;

import com.ggumtle.ggumtle.common.SocketCommandHandler;
import com.ggumtle.ggumtle.common.SocketRequestType;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.errorCode.FriendErrorCode;
import com.ggumtle.ggumtle.friend.application.FriendService;
import com.ggumtle.ggumtle.friend.application.command.FriendRequestCommand;
import com.ggumtle.ggumtle.friend.application.result.FriendRequestResult;
import com.ggumtle.ggumtle.friend.presentation.request.FriendRequestRequest;
import com.ggumtle.ggumtle.friend.presentation.response.FriendRequestResponse;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketSession;

import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class FriendController {
    private final FriendService friendService;
    private final ApplicationEventPublisher applicationEventPublisher;

    @SocketCommandHandler(type = SocketRequestType.FRIEND_REQUEST)
    public void requestFriend(FriendRequestRequest request, WebSocketSession session) {
        if (request.targetMemberId() == null) {
            throw new GgumtleException(FriendErrorCode.TARGET_REQUIRED);
        }

        Long requesterId = Long.parseLong(session.getPrincipal().getName());
        FriendRequestCommand command = request.toCommand(requesterId);

        FriendRequestResult result = friendService.requestFriend(command);
        FriendRequestResponse response = FriendRequestResponse.from(result);

        SendSocketEvent event = new SendSocketEvent(List.of(requesterId,result.targetMemberId()),response);
        applicationEventPublisher.publishEvent(event);
    }
}
