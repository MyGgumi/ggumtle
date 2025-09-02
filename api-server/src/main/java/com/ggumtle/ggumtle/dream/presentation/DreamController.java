package com.ggumtle.ggumtle.dream.presentation;

import com.ggumtle.ggumtle.common.SocketRequestType;
import com.ggumtle.ggumtle.common.SocketCommandHandler;
import com.ggumtle.ggumtle.dream.application.DreamPartyService;
import com.ggumtle.ggumtle.dream.application.DreamService;
import com.ggumtle.ggumtle.dream.application.command.AcceptPartyInvitationCommand;
import com.ggumtle.ggumtle.dream.application.command.CreatePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.InvitePartyCommand;
import com.ggumtle.ggumtle.dream.application.command.StartDreamCommand;
import com.ggumtle.ggumtle.dream.application.result.AcceptPartyInvitationResult;
import com.ggumtle.ggumtle.dream.application.result.CreatePartyResult;
import com.ggumtle.ggumtle.dream.application.result.InvitePartyResult;
import com.ggumtle.ggumtle.dream.presentation.request.AcceptPartyInvitationRequest;
import com.ggumtle.ggumtle.dream.presentation.request.InvitePartyRequest;
import com.ggumtle.ggumtle.dream.presentation.request.StartDreamRequest;
import com.ggumtle.ggumtle.dream.presentation.response.AcceptPartyInvitationResponse;
import com.ggumtle.ggumtle.dream.presentation.response.CreatePartyResponse;
import com.ggumtle.ggumtle.dream.presentation.response.InvitePartyResponse;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketSession;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class DreamController {
    private final ApplicationEventPublisher applicationEventPublisher;
    private final DreamService dreamService;
    private final DreamPartyService dreamPartyService;

    @SocketCommandHandler(type = SocketRequestType.CREATE_PARTY)
    public void createParty(WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        CreatePartyResult result = dreamPartyService.createParty(new CreatePartyCommand(requesterId));

        SendSocketEvent event = new SendSocketEvent(result.clientMemberIds(), new CreatePartyResponse(result.partyId()));
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketRequestType.INVITE_PARTY)
    public void inviteParty(InvitePartyRequest request, WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        InvitePartyResult result = dreamPartyService.inviteParty(new InvitePartyCommand(requesterId, request.inviteeId()));

        SendSocketEvent event = new SendSocketEvent(result.memberIds(), new InvitePartyResponse(result.invitationId()));
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketRequestType.ACCEPT_PARTY_INVITATION)
    public void acceptPartyInvitation(AcceptPartyInvitationRequest request, WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        AcceptPartyInvitationCommand command = new AcceptPartyInvitationCommand(requesterId, request.invitationId());
        AcceptPartyInvitationResult result = dreamPartyService.acceptPartyInvitation(command);

        AcceptPartyInvitationResponse response = new AcceptPartyInvitationResponse(result.joinedMemberId(), result.joinedMemberNickname());
        SendSocketEvent event = new SendSocketEvent(result.memberIds(), response);
        applicationEventPublisher.publishEvent(event);
    }

    @SocketCommandHandler(type = SocketRequestType.START_DREAM)
    public void startDream(StartDreamRequest request, WebSocketSession session) {
        Long requesterId = Long.parseLong(session.getPrincipal().getName());

        StartDreamCommand command = new StartDreamCommand(requesterId, request.partyId());
        dreamService.startDream(command);
    }
}
