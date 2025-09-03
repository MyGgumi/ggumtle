package com.ggumtle.ggumtle.dream.domain;

import lombok.Getter;
import org.springframework.data.annotation.Id;
import org.springframework.data.redis.core.RedisHash;
import org.springframework.data.redis.core.index.Indexed;

import java.util.List;

@RedisHash("dream")
@Getter
public class Dream {
    @Id
    private Long roomId;

    @Indexed
    private String roomRequestId;

    private List<Long> playerIds;

    public Dream(String roomRequestId, List<Long> playerIds) {
        this.roomRequestId = roomRequestId;
        this.playerIds = playerIds;
    }
}
