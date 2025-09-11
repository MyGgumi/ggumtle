package com.ggumtle.ggumtle.friend.domain;

import org.springframework.data.annotation.Id;
import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.ToString;
import org.springframework.data.redis.core.RedisHash;
import org.springframework.data.redis.core.TimeToLive;

@Getter
@ToString
@AllArgsConstructor
@RedisHash("member_state")
public class MemberState {
    @Id
    private String memberId;

    private String status;

    private Long updatedAt;

    @TimeToLive
    private Long ttlSeconds;
}
