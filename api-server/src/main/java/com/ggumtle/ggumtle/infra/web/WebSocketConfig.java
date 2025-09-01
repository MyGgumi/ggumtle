package com.ggumtle.ggumtle.infra.web;

import com.ggumtle.ggumtle.infra.websocket.JwtHandshakeHandler;
import com.ggumtle.ggumtle.infra.websocket.JwtHandshakeInterceptor;
import com.ggumtle.ggumtle.presentation.SocketHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Configuration;
import org.springframework.web.socket.config.annotation.EnableWebSocket;
import org.springframework.web.socket.config.annotation.WebSocketConfigurer;
import org.springframework.web.socket.config.annotation.WebSocketHandlerRegistry;

@Configuration
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@EnableWebSocket
public class WebSocketConfig implements WebSocketConfigurer {
    private final SocketHandler socketHandler;
    private final JwtHandshakeInterceptor jwtHandshakeInterceptor;
    private final JwtHandshakeHandler jwtHandshakeHandler;

    @Override
    public void registerWebSocketHandlers(WebSocketHandlerRegistry registry) {
        registry.addHandler(socketHandler, "ws")
                .addInterceptors(jwtHandshakeInterceptor)
                .setHandshakeHandler(jwtHandshakeHandler)
                .setAllowedOrigins("*");
    }
}
