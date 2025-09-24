package com.ggumtle.ggumtle.dream.domain;

import lombok.Getter;
import lombok.NoArgsConstructor;
import org.springframework.data.annotation.Id;
import org.springframework.data.redis.core.RedisHash;
import org.springframework.data.redis.core.index.Indexed;

import java.util.List;

@RedisHash("dream")
@NoArgsConstructor
@Getter
public class Dream {
    @Id
    private String roomRequestId;

    @Indexed
    private Long roomId;

    private List<Long> playerIds;

    private String host;

    private Integer port;

    public Dream(String roomRequestId, List<Long> playerIds, OptimalServer server) {
        this.roomRequestId = roomRequestId;
        this.playerIds = playerIds;
        this.host = server.getHost();
        this.port = server.getPort();
    }

    public void setId(Long roomId) {
        this.roomId = roomId;
    }
}
