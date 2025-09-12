package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.member.domain.Member;

import java.util.List;
import java.util.Map;

public record GetPartyParticipantsResult(
        List<Participant> participants
) {
    public static GetPartyParticipantsResult of(List<PartyParticipant> participants, Map<Long, Member> memberMap){
        return new GetPartyParticipantsResult(
                participants == null ? List.of() :
                        participants.stream()
                                .map(p -> {
                                    Member member = memberMap.get(p.getMemberId());
                                    String nickname = member.getNickname();
                                    return new Participant(
                                            p.getMemberId(),
                                            nickname,
                                            p.isLeader(),
                                            p.isReady()
                                    );
                                })
                                .toList()
        );
    }

    public record Participant(
            Long memberId,
            String nickname,
            boolean isLeader,
            boolean isReady
    ){}
}