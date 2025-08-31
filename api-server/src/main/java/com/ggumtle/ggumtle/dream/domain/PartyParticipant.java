package com.ggumtle.ggumtle.dream.domain;

import lombok.Getter;
import lombok.ToString;
import org.springframework.data.annotation.Id;
import org.springframework.data.redis.core.RedisHash;
import org.springframework.data.redis.core.index.Indexed;

@RedisHash("party_participant")
@Getter
@ToString
public class PartyParticipant {
    @Id
    private Long memberId;

    @Indexed
    private String partyId;

    private boolean isLeader;

    public PartyParticipant(Long memberId, String partyId, boolean isLeader) {
        this.memberId = memberId;
        this.partyId = partyId;
        this.isLeader = isLeader;
    }
}
