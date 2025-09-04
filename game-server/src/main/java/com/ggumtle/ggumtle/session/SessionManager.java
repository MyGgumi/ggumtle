package com.ggumtle.ggumtle.session;

import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import com.ggumtle.ggumtle.session.result.SessionResult;
import io.netty.channel.Channel;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Optional;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@Slf4j
@Component
public class SessionManager {

    // 세션 ID 생성을 위한 시퀀스
    private final AtomicLong sessionIdGenerator = new AtomicLong(0);

    // Session 관리를 위한 맵
    private final ConcurrentHashMap<Channel, Session> sessionMap = new ConcurrentHashMap<>();

    /**
     * 모든 세션에 패킷을 브로드캐스트합니다.
     *
     * @param packet 브로드캐스트할 패킷
     */
    public void broadcast(Packet packet) {
        log.debug("모든 세션에 패킷 브로드캐스트: {}", SendPacketType.fromValue(packet.header().packetType()));

        sessionMap.values().forEach(session -> session.sendPacket(packet));
    }

    /**
     * 새로운 세션을 생성합니다.
     *
     * @param channel 세션에 연결된 채널
     */
    public void createSession(Channel channel, long memberId) {
        long sessionId = sessionIdGenerator.incrementAndGet();

        Session session = Session.builder()
                .sessionId(sessionId)
                .channel(channel)
                .memberId(memberId)
                .build();

        log.debug("새로운 세션 생성: {}", session);

        sessionMap.put(channel, session);
        log.debug("세션 맵에 추가: {}", sessionId);

        SessionResult sessionResult = new SessionResult(true, sessionId);
        Packet packet = Packet.of(SendPacketType.VERIFY_TOKEN_RESULT, System.currentTimeMillis(), sessionResult);
        channel.writeAndFlush(packet);
    }

    public boolean existSession(Channel channel) {
        return sessionMap.containsKey(channel);
    }

    public Optional<Session> getSession(Channel channel) {
        if (!sessionMap.containsKey(channel)) {
            log.warn("세션이 존재하지 않습니다");
            return Optional.empty();
        }

        return Optional.of(sessionMap.get(channel));
    }

    public void removeSession(Channel channel) {
        Session session = sessionMap.getOrDefault(channel, null);

        if (session != null) {
            log.debug("세션 제거: {}", session.getSessionId());
            session.disconnect();
            sessionMap.remove(channel);
        }
    }

    /**
     * 모든 세션 리스트를 반환합니다.
     *
     * @return 세션 리스트
     */
    public List<Session> getSessions() {
        log.debug("모든 세션 리스트 반환: {}", sessionMap.values().stream().map(Session::getSessionId).toList());

        return sessionMap.values().stream().toList();
    }
}
