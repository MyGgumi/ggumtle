package com.ggumtle.ggumtle.server.packet;

/**
 * @param header 패킷 헤더
 * @param data   패킷 바디 데이터
 */
public record Packet(
        PacketHeader header,
        byte[] data
) {
    public boolean hasData() {
        return data != null && data.length > 0;
    }

    public int getDataLength() {
        return data != null ? data.length : 0;
    }
}
