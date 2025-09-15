package com.ggumtle.ggumtle.server.applicatoin;

import com.ggumtle.ggumtle.auth.JwtService;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.session.Session;
import com.ggumtle.ggumtle.session.SessionManager;
import com.ggumtle.ggumtle.session.result.SessionBody;
import io.netty.channel.Channel;
import io.netty.channel.group.ChannelGroup;
import io.netty.channel.group.DefaultChannelGroup;
import io.netty.util.concurrent.GlobalEventExecutor;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.Optional;

@Slf4j
@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class ChannelManager {

    private final ChannelGroup channels = new DefaultChannelGroup(GlobalEventExecutor.INSTANCE);
    private final SessionManager sessionManager;
    private final JwtService jwtService;

    public void addChannel(Channel channel) {
        channels.add(channel);
    }

    public void removeChannel(Channel channel) {
        sessionManager.removeSession(channel);
        channels.remove(channel);
    }

    public int getChannelCount() {
        return channels.size();
    }

    public void authorizeChannel(Channel channel, String accessToken) {
        Optional<Session> optionalSession = sessionManager.getSession(channel);
        if (optionalSession.isPresent()) {
            Session existingSession = optionalSession.get();
            log.warn("[{}] 이미 {}번 사용자의 {}번 세션이 존재합니다", channel.id(), existingSession.getMemberId(), existingSession.getSessionId());

            SessionBody sessionResult = new SessionBody(false, -1L);
            Packet packet = Packet.of(SendPacketType.VERIFY_TOKEN, System.currentTimeMillis(), sessionResult);
            channel.writeAndFlush(packet);

            return;
        }

        if (!jwtService.verifyToken(accessToken)) {
            log.info("[{}] 토큰이 유효하지 않습니다", channel.id());

            SessionBody sessionResult = new SessionBody(false, -1L);
            Packet packet = Packet.of(SendPacketType.VERIFY_TOKEN, System.currentTimeMillis(), sessionResult);
            channel.writeAndFlush(packet);

            return;
        }

        log.info("[{}] 토큰 인증 성공", channel.id());
        long memberId = jwtService.parseId(accessToken);

        sessionManager.createSession(channel, memberId);
    }

    public boolean isAuthorized(Channel channel) {
        return sessionManager.existSession(channel);
    }

    public Optional<Session> getSession(Channel channel) {
        return sessionManager.getSession(channel);
    }

    public long getPendingWriteCount() {
        long totalPending = 0;
        for (Channel ch : channels) {
            if (!ch.isWritable()) {
                totalPending += 1;
            }
        }
        return totalPending;
    }
}
