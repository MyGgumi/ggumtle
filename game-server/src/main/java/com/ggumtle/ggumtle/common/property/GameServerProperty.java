package com.ggumtle.ggumtle.common.property;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "game-server")
@RequiredArgsConstructor
@Getter
public class GameServerProperty {
    private final int port;
    private final String id;
}
