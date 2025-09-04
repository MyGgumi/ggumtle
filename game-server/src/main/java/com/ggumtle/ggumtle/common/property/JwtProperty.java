package com.ggumtle.ggumtle.common.property;

import lombok.Getter;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.context.properties.ConfigurationProperties;

@ConfigurationProperties(prefix = "jwt.access")
@RequiredArgsConstructor
@Getter
public class JwtProperty {
    private final String secret;
}
