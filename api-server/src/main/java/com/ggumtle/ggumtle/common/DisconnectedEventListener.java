package com.ggumtle.ggumtle.common;

import com.ggumtle.ggumtle.common.event.UserDisconnectedEvent;
import com.ggumtle.ggumtle.dream.application.DreamPartyService;
import com.ggumtle.ggumtle.dream.application.DreamService;
import com.ggumtle.ggumtle.dream.application.command.CancelMatchingCommand;
import com.ggumtle.ggumtle.dream.application.command.LeavePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.CancelMatchingResult;
import com.ggumtle.ggumtle.dream.application.result.LeavePartyResult;
import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.dream.persistence.PartyParticipantRepository;
import com.ggumtle.ggumtle.dream.presentation.response.CancelMatchingResponse;
import com.ggumtle.ggumtle.dream.presentation.response.LeavePartyResponse;
import com.ggumtle.ggumtle.exception.GgumtleException;
import com.ggumtle.ggumtle.exception.code.DreamErrorCode;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;
import java.util.List;
import java.util.Optional;

@Component
@RequiredArgsConstructor
public class DisconnectedEventListener {

    private final DreamPartyService dreamPartyService;
    private final DreamService dreamService;
    private final ApplicationEventPublisher applicationEventPublisher;

    @EventListener
    public void handleUserDisconnect(UserDisconnectedEvent event) {
        processUserDisconnect(event.memberId());
    }

    public void processUserDisconnect(Long memberId) {
        try {
            CancelMatchingCommand cancelCommand = new CancelMatchingCommand(memberId);
            CancelMatchingResult cancelResult = dreamService.cancelMatching(cancelCommand);

            List<Long> cancelRecipients = cancelResult.participantIds().stream()
                    .filter(id -> !id.equals(memberId))
                    .toList();

            CancelMatchingResponse cancelResponse = new CancelMatchingResponse(cancelResult.message());
            SendSocketEvent sendCancelEvent = new SendSocketEvent(SocketType.CANCELLED_MATCHING, cancelRecipients, cancelResponse);
            applicationEventPublisher.publishEvent(sendCancelEvent);

        } catch (GgumtleException e) {
            DreamErrorCode.NOT_FOUND_MATCHING.getCode();
        }

        LeavePartyCommand command = new LeavePartyCommand(memberId);
        LeavePartyResult result = dreamPartyService.leaveParty(command);

        List<Long> recipients = result.participantMembers().stream()
                .filter(id -> !id.equals(memberId))
                .toList();

        if (!recipients.isEmpty()) {
            LeavePartyResponse response = new LeavePartyResponse(memberId, result.newLeaderId());
            SendSocketEvent sendLeaveEvent = new SendSocketEvent(SocketType.LEAVE_PARTY, recipients, response);
            applicationEventPublisher.publishEvent(sendLeaveEvent);
        }
    }
}