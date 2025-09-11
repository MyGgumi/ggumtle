package com.ggumtle.ggumtle.common;

import com.ggumtle.ggumtle.friend.application.MemberStateService;
import com.ggumtle.ggumtle.presentation.SocketResponseDispatcher;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;

import java.util.Objects;

@Slf4j
@Component
@RequiredArgsConstructor
public class CheckSessionScheduler {
    private final SocketResponseDispatcher socketResponseDispatcher;
    private final MemberStateService memberStateService;

    @Scheduled(fixedRate = 40000)
    public void checkSession() {
        socketResponseDispatcher.getSession().values().forEach(session -> {
            if (session.isOpen() && session.getPrincipal() != null) {
                try {
                    Long memberId = Long.parseLong(Objects.requireNonNull(session.getPrincipal()).getName());
                    memberStateService.heartBeat(memberId);
                } catch (Exception e) {
                    log.error("Error during heartbeat for session {}: {}", session.getId(), e.getMessage());
                }
            }
        });
    }
}
