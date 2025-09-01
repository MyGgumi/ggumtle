package com.ggumtle.ggumtle.server.packet;

import com.ggumtle.ggumtle.common.dto.Result;

import java.nio.charset.StandardCharsets;

/**
 * @param header 패킷 헤더
 * @param data   패킷 바디 데이터
 */
public record Packet(
        PacketHeader header,
        byte[] data
) {
    public static Packet of(SendPacketType sendPacketType, long timestamp, Result result) {
        byte[] data = result.toBytes(StandardCharsets.UTF_8);
        PacketHeader packetHeader = new PacketHeader(sendPacketType.getValue(), data.length, timestamp);

        return new Packet(packetHeader, data);
    }

    public boolean hasData() {
        return data != null && data.length > 0;
    }

    public int getDataLength() {
        return data != null ? data.length : 0;
    }
}
