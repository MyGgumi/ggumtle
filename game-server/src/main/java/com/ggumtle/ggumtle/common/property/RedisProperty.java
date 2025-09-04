package com.ggumtle.ggumtle.common.property;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "spring.data.redis")
@RequiredArgsConstructor
@Getter
public class RedisProperty {
    private final String host;
    private final int port;
    private final String password;
}
