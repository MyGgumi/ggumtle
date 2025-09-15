//package com.ggumtle.ggumtle.common;
//
//import com.ggumtle.ggumtle.dream.application.DreamPartyService;
//import com.ggumtle.ggumtle.friend.application.MemberStateService;
//import lombok.RequiredArgsConstructor;
//import org.springframework.scheduling.annotation.Scheduled;
//import org.springframework.stereotype.Component;
//
//import java.util.Set;
//
//@Component
//@RequiredArgsConstructor
//public class StaleSessionCleanupScheduler {
//    private final DreamPartyService dreamPartyService;
//    private final MemberStateService memberStateService;
//
//    @Scheduled(fixedRate = 60000)
//    public void cleanupStalSessions(){
//        Set<Long> memberIdsInParties = dreamPartyService.
//    }
//}
