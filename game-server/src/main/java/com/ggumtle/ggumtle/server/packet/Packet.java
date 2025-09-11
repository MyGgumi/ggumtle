package com.ggumtle.ggumtle.server.packet;

import com.ggumtle.ggumtle.common.dto.Body;

import java.nio.charset.StandardCharsets;

/**
 * @param header 패킷 헤더
 * @param data   패킷 바디 데이터
 */
public record Packet(
        PacketHeader header,
        byte[] data
) {
    public static Packet of(SendPacketType sendPacketType, long timestamp, Body body) {
        if (body != null) {
            byte[] data = body.toBytes(StandardCharsets.UTF_8);

            PacketHeader packetHeader = new PacketHeader(sendPacketType.getValue(), data.length, timestamp);
            return new Packet(packetHeader, data);
        }

        PacketHeader packetHeader = new PacketHeader(sendPacketType.getValue(), 0, timestamp);
        return new Packet(packetHeader, null);
    }

    public boolean hasData() {
        return data != null && data.length > 0;
    }

    public int getDataLength() {
        return data != null ? data.length : 0;
    }
}
