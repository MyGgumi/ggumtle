package com.ggumtle.ggumtle.common.property;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "metric-server")
@RequiredArgsConstructor
@Getter
public class MetricServerProperty {
    private final int port;
}
