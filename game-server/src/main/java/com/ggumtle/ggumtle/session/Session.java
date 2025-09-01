package com.ggumtle.ggumtle.session;

import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.SendPacketType;
import io.netty.channel.Channel;
import lombok.Builder;
import lombok.Getter;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;

@Slf4j
@RequiredArgsConstructor
@Builder
@Getter
public class Session {

    private final long sessionId;
    private final Channel channel;
    private final long memberId;

    /**
     * Session에게 패킷을 전송합니다.
     *
     * @param packet 전송할 패킷
     */
    public void sendPacket(Packet packet) {
        if (isConnected()) {
            channel.writeAndFlush(packet);
            log.debug("Session {}에게 패킷 전송: {}", sessionId, SendPacketType.fromValue(packet.header().packetType()));
        } else {
            log.warn("Session {}가 연결되지 않아 패킷 전송 실패", sessionId);
        }
    }

    /**
     * Session의 연결을 해제합니다.
     */
    public void disconnect() {
        if (isConnected()) {
            channel.close();
            log.info("Session {} 연결 해제", sessionId);
        }
    }

    /**
     * Session가 연결되어 있는지 확인합니다.
     *
     * @return 연결 상태
     */
    public boolean isConnected() {
        return channel != null && channel.isActive();
    }

    /**
     * Session의 원격 주소를 반환합니다.
     *
     * @return 원격 주소
     */
    public String getRemoteAddress() {
        return channel != null ?
                channel.remoteAddress().toString() : "Unknown";
    }
}
