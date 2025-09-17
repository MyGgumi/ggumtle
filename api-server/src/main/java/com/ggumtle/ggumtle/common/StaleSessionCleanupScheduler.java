package com.ggumtle.ggumtle.common;

import com.ggumtle.ggumtle.common.event.UserDisconnectedEvent;
import com.ggumtle.ggumtle.dream.application.DreamPartyService;
import com.ggumtle.ggumtle.dream.application.command.LeavePartyCommand;
import com.ggumtle.ggumtle.dream.application.result.LeavePartyResult;
import com.ggumtle.ggumtle.dream.presentation.response.LeavePartyResponse;
import com.ggumtle.ggumtle.friend.application.MemberStateService;
import com.ggumtle.ggumtle.presentation.SendSocketEvent;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Set;

@Slf4j
@Component
@RequiredArgsConstructor
public class StaleSessionCleanupScheduler {
    private final DreamPartyService dreamPartyService;
    private final MemberStateService memberStateService;
    private final ApplicationEventPublisher applicationEventPublisher;

    @Scheduled(fixedRate = 60000)
    public void cleanupStaleSessions(){
        Set<Long> memberIdsInParties = dreamPartyService.getAllActivePartyMemberIds();

        for (Long memberId : memberIdsInParties) {
            if (memberStateService.getState(memberId) == MemberStateService.State.OFFLINE) {
                applicationEventPublisher.publishEvent(new UserDisconnectedEvent(memberId));
            }
        }

    }
}
