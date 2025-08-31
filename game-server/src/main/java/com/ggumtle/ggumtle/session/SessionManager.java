package com.ggumtle.ggumtle.session;

import com.ggumtle.ggumtle.server.packet.Packet;
import io.netty.channel.Channel;
import io.netty.channel.group.ChannelGroup;
import io.netty.channel.group.DefaultChannelGroup;
import io.netty.util.concurrent.GlobalEventExecutor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicLong;

@Slf4j
@Component
public class SessionManager {

    // 세션 ID 생성을 위한 시퀀스
    private final AtomicLong sessionIdGenerator = new AtomicLong(0);

    // Session 관리를 위한 맵
    private final ConcurrentHashMap<Channel, Session> sessionMap = new ConcurrentHashMap<>();

    // 모든 채널을 관리하기 위한 채널 그룹
    private final ChannelGroup allChannels = new DefaultChannelGroup(GlobalEventExecutor.INSTANCE);

    /**
     * 모든 세션에 패킷을 브로드캐스트합니다.
     *
     * @param packet 브로드캐스트할 패킷
     */
    public void broadcast(Packet packet) {
        log.debug("모든 세션에 패킷 브로드캐스트: {}", packet.header().packetType());
        allChannels.writeAndFlush(packet);
    }

    /**
     * 새로운 세션을 생성합니다.
     *
     * @param channel 세션에 연결된 채널
     */
    public void createSession(Channel channel) {
        long sessionId = sessionIdGenerator.incrementAndGet();

        log.debug("새로운 세션 생성: {}", sessionId);

        Session session = Session.builder()
                .sessionId(sessionId)
                .channel(channel)
                .build();

        log.debug("세션 맵에 추가: {}", sessionId);
        sessionMap.put(channel, session);
        allChannels.add(session.getChannel());
    }

    /**
     * 채널에 연결된 세션을 조회합니다.
     *
     * @param channel 조회할 채널
     * @return 조회된 세션
     */
    public Session getSession(Channel channel) {
        if (!sessionMap.containsKey(channel)) {
            throw new IllegalArgumentException("해당 세션이 존재하지 않습니다.");
        }

        return sessionMap.get(channel);
    }

    /**
     * 세션을 제거합니다.
     *
     * @param channel 제거할 채널
     */
    public void removeSession(Channel channel) {
        Session session = sessionMap.get(channel);

        if (session != null) {
            log.debug("세션 제거: {}", session.getSessionId());
            session.disconnect();
            sessionMap.remove(channel);
        }

        if (allChannels.contains(channel)) {
            log.debug("모든 채널에서 제거: {}", channel.remoteAddress());
            allChannels.remove(channel);
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
