package com.ggumtle.ggumtle.infra.websocket;

import jakarta.validation.constraints.NotNull;
import org.springframework.http.server.ServerHttpRequest;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketHandler;
import org.springframework.web.socket.server.support.DefaultHandshakeHandler;

import java.security.Principal;
import java.util.Map;

@Component
public class JwtHandshakeHandler extends DefaultHandshakeHandler {
    @Override
    protected Principal determineUser(@NotNull ServerHttpRequest requst,
                                      @NotNull WebSocketHandler webSocketHandler,
                                      @NotNull Map<String, Object> attributes) {
        Object memberId = attributes.get("memberId");

        if (memberId == null) {
            return null;
        }
        String name = String.valueOf(memberId);
        return () -> name;
    }
}
