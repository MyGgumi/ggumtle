package com.ggumtle.ggumtle.dream.domain;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Getter;
import org.springframework.data.annotation.Id;
import org.springframework.data.redis.core.RedisHash;
import org.springframework.data.redis.core.index.Indexed;

@RedisHash(value = "party_invitation", timeToLive = 60 * 5)
@AllArgsConstructor
@Builder
@Getter
public class PartyInvitation {
    @Id
    private String id;

    private String partyId;

    private Long inviterId;

    @Indexed
    private Long inviteeId;
}
