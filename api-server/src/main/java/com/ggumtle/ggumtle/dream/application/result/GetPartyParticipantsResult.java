package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.mongging.domain.Mongging;

import java.util.List;
import java.util.Map;

public record GetPartyParticipantsResult(
        List<Participant> participants
) {
    public static GetPartyParticipantsResult of(List<PartyParticipant> partyParticipants, Map<Long, Mongging> monggings){
        if (partyParticipants == null) {
            return new GetPartyParticipantsResult(List.of());
        }

        List<Participant> participants = partyParticipants.stream()
                .map(participant ->
                        new Participant(
                                participant.getMemberId(),
                                monggings.get(participant.getMemberId()).getOwner().getNickname(),
                                participant.isLeader(),
                                participant.isReady(),
                                monggings.get(participant.getMemberId()).getMonggingClass().getId(),
                                monggings.get(participant.getMemberId()).getLevel()
                        )
                )
                .toList();
        return new GetPartyParticipantsResult(participants);
    }

    public record Participant(
            Long memberId,
            String nickname,
            boolean isLeader,
            boolean isReady,
            Long monggingClassId,
            Integer monggingLevel
    ) {
    }
}