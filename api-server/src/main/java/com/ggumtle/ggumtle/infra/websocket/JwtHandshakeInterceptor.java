package com.ggumtle.ggumtle.infra.websocket;

import com.ggumtle.ggumtle.auth.jwt.JwtProvider;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.http.server.ServerHttpRequest;
import org.springframework.http.server.ServerHttpResponse;
import org.springframework.http.server.ServletServerHttpRequest;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.WebSocketHandler;
import org.springframework.web.socket.server.HandshakeInterceptor;

import java.util.List;
import java.util.Map;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class JwtHandshakeInterceptor implements HandshakeInterceptor {
    private final JwtProvider jwtProvider;

    @Override
    public boolean beforeHandshake(ServerHttpRequest request,
                                   ServerHttpResponse response,
                                   WebSocketHandler webSocketHandler,
                                   Map<String, Object> attributes) throws Exception {

        String token = extractToken(request);
        if (token == null || token.isBlank()) {
            return false;
        }

        try {
            Long memberId = jwtProvider.parseMemberId(token);
            if (memberId == null) return false;

            attributes.put("memberId", memberId);
            return true;
        } catch (Exception e) {
            return false;
        }
    }

    @Override
    public void afterHandshake(ServerHttpRequest request,
                               ServerHttpResponse response,
                               WebSocketHandler webSocketHandler,
                               Exception exception) {
    }

    private String extractToken(ServerHttpRequest request) {
        List<String> authorizationHeaders = request.getHeaders().get("Authorization");
        if (authorizationHeaders != null && !authorizationHeaders.isEmpty()) {
            String authorizationHeader = authorizationHeaders.get(0);
            if (authorizationHeader != null && authorizationHeader.startsWith("Bearer ")) {
                return authorizationHeader.substring(7);
            }
        }

        if (request instanceof ServletServerHttpRequest servletRequest) {
            String queryToken = servletRequest.getServletRequest().getParameter("token");
            if (queryToken != null && !queryToken.isBlank()) {
                return queryToken;
            }
        }
        return null;
    }
}
