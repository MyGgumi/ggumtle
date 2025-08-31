package com.ggumtle.ggumtle.dream.domain;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.ToString;
import org.springframework.data.annotation.Id;
import org.springframework.data.redis.core.RedisHash;

@RedisHash("waiting_party")
@NoArgsConstructor
@AllArgsConstructor
@Getter
@ToString
public class WaitingParty {
    @Id
    private String partyId;

    private Integer participantSize;
}
