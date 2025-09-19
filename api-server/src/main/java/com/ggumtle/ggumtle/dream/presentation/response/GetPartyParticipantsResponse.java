package com.ggumtle.ggumtle.dream.presentation.response;

import com.ggumtle.ggumtle.dream.application.result.GetPartyParticipantsResult;

import java.util.List;

public record GetPartyParticipantsResponse(
        List<Participant> participants
) {
    public static GetPartyParticipantsResponse from(GetPartyParticipantsResult result){
        return new GetPartyParticipantsResponse(result.participants().stream()
                .map(p -> new Participant(
                        p.memberId(),
                        p.nickname(),
                        p.isLeader(),
                        p.isReady(),
                        p.monggingClassId(),
                        p.monggingLevel()
                ))
                .toList()
        );
    }

    private record Participant(
            Long memberId,
            String nickname,
            boolean isLeader,
            boolean isReady,
            Long monggingClassId,
            Integer monggingLevel
    ){}
}
