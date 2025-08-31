package com.ggumtle.ggumtle.dream.domain;

import lombok.AllArgsConstructor;
import lombok.Getter;
import org.springframework.data.annotation.Id;
import org.springframework.data.redis.core.RedisHash;

@RedisHash("dream-server")
@AllArgsConstructor
@Getter
public class DreamServer {

    @Id
    private String id;
}
