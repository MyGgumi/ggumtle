package com.ggumtle.ggumtle.dream.presentation;

import com.ggumtle.ggumtle.common.SocketCommandHandler;
import com.ggumtle.ggumtle.common.SocketType;
import com.ggumtle.ggumtle.dream.application.DreamPartyService;
import com.ggumtle.ggumtle.dream.application.DreamService;
import com.ggumtle.ggumtle.dream.application.command.AcceptPartyInvitationCommand;
import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.InvitePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.ReadyDreamCommand;
import com.ggumtle.ggumtle.dream.application.command.StartDreamCommand;
import com.ggumtle.ggumtle.dream.application.result.AcceptPartyInvitationResult;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.application.result.InvitePartyResult;
import com.ggumtle.ggumtle.dream.application.result.ReadyDreamResult;
import com.ggumtle.ggumtle.dream.application.result.UnreadyDreamResult;
import com.ggumtle.ggumtle.dream.presentation.request.AcceptPartyInvitationRequest;
import com.ggumtle.ggumtle.dream.presentation.request.InvitePartyRequest;
import com.ggumtle.ggumtle.dream.presentation.response.AcceptPartyInvitationResponse;
import com.ggumtle.ggumtle.dream.presentation.response.CreatePartyResponse;
import com.ggumtle.ggumtle.dream.presentation.response.InvitePartyResponse;
import com.ggumtle.ggumtle.dream.presentation.response.ReadyDreamResponse;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketSession;

import java.util.List;

@Slf4j
@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamController {
    private final ApplicationEventPublisher applicationEventPublisher;
    private final DreamService dreamService;
    private final DreamPartyService dreamPartyService;

    @SocketCommandHandler(type = SocketType.CREATE_PARTY)
    public void createParty(WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        CreatePartyResult result = dreamPartyService.createParty(new CreatePartyCommand(requesterId));

        SendSocketEvent event = new SendSocketEvent(SocketType.CREATE_PARTY, result.clientMemberIds(), new CreatePartyResponse(result.partyId()));
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketType.INVITE_PARTY)
    public void inviteParty(InvitePartyRequest request, WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        InvitePartyResult result = dreamPartyService.inviteParty(new InvitePartyCommand(requesterId, request.inviteeId()));

        SendSocketEvent event = new SendSocketEvent(SocketType.INVITE_PARTY, result.memberIds(), new InvitePartyResponse(result.invitationId()));
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketType.ACCEPT_PARTY_INVITATION)
    public void acceptPartyInvitation(AcceptPartyInvitationRequest request, WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        AcceptPartyInvitationCommand command = new AcceptPartyInvitationCommand(requesterId, request.invitationId());
        AcceptPartyInvitationResult result = dreamPartyService.acceptPartyInvitation(command);

        AcceptPartyInvitationResponse response = new AcceptPartyInvitationResponse(result.joinedMemberId(), result.joinedMemberNickname());
        SendSocketEvent event = new SendSocketEvent(SocketType.ACCEPT_PARTY_INVITATION, result.memberIds(), response);
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketType.START_DREAM)
    public void startDream(WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        StartDreamCommand command = new StartDreamCommand(requesterId);
        dreamService.startDream(command);
    }

    @SocketCommandHandler(type = SocketType.READY_DREAM)
    public void readyDream(WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        ReadyDreamCommand command = new ReadyDreamCommand(requesterId);
        ReadyDreamResult result = dreamPartyService.readyDream(command);

        ReadyDreamResponse response = new ReadyDreamResponse(requesterId,true);

        SendSocketEvent event = new SendSocketEvent(SocketType.READY_DREAM, result.participantMemberIds(), response);
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketType.UNREADY_DREAM)
    public void unreadyDream(WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        ReadyDreamCommand command = new ReadyDreamCommand(requesterId);
        UnreadyDreamResult result = dreamPartyService.unreadyDream(command);

        ReadyDreamResponse response = new ReadyDreamResponse(requesterId,false);

        SendSocketEvent event = new SendSocketEvent(SocketType.UNREADY_DREAM, result.participantMemberIds(), response);
        applicationEventPublisher.publishEvent(event);
    }
}
