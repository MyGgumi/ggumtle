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

    private boolean isReady;

    public PartyParticipant(Long memberId, String partyId, boolean isLeader) {
        this.memberId = memberId;
        this.partyId = partyId;
        this.isLeader = isLeader;
        this.isReady = false;
    }

    public void ready() {
        this.isReady = true;
    }

    public void unready() {
        this.isReady = false;
    }

    public void setAsLeader(){
        this.isLeader = true;
    }
}
