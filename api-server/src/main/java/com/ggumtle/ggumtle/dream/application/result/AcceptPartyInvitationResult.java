package com.ggumtle.ggumtle.dream.application.result;

import com.ggumtle.ggumtle.dream.domain.PartyParticipant;
import com.ggumtle.ggumtle.mongging.domain.Mongging;

import java.util.List;

public record AcceptPartyInvitationResult(
        List<Long> memberIds,
        Long joinedMemberId,
        String joinedMemberNickname,
        Long monggingClassId,
        Integer monggingLevel
) {
    public static AcceptPartyInvitationResult of(List<PartyParticipant> participants, Mongging mongging) {
        return new AcceptPartyInvitationResult(
                participants.stream().map(PartyParticipant::getMemberId).toList(),
                mongging.getOwner().getId(),
                mongging.getOwner().getNickname(),
                mongging.getMonggingClass().getId(),
                mongging.getLevel()
        );
    }
}
