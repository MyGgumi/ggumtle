package com.ggumtle.ggumtle.common;

import com.ggumtle.ggumtle.common.event.UserDisconnectedEvent;
import com.ggumtle.ggumtle.dream.application.DreamPartyService;
import com.ggumtle.ggumtle.dream.application.command.LeavePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.LeavePartyResult;
import com.ggumtle.ggumtle.dream.presentation.response.LeavePartyResponse;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;
import java.util.List;

@Component
@RequiredArgsConstructor
public class DisconnectedEventListener {

    private final DreamPartyService dreamPartyService;
    private final ApplicationEventPublisher applicationEventPublisher;

    @EventListener
    public void handleUserDisconnect(UserDisconnectedEvent event) {
        processUserDisconnect(event.memberId());
    }
    public void processUserDisconnect(Long memberId) {

        LeavePartyCommand command = new LeavePartyCommand(memberId);
        LeavePartyResult result = dreamPartyService.leaveParty(command);

        List<Long> recipients = result.participantMembers().stream()
                .filter(id -> !id.equals(memberId))
                .toList();

        if (!recipients.isEmpty()) {
            LeavePartyResponse response = new LeavePartyResponse(memberId, result.newLeaderId());
            SendSocketEvent sendEvent = new SendSocketEvent(SocketType.LEAVE_PARTY, recipients, response);
            applicationEventPublisher.publishEvent(sendEvent);
        }
    }
}