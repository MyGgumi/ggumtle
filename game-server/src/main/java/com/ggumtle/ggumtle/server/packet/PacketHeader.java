package com.ggumtle.ggumtle.server.packet;

import java.nio.ByteBuffer;

/**
 * @param packetType 패킷 타입
 * @param dataLength 데이터 길이
 * @param timestamp  타임스탬프
 */
public record PacketHeader(
        short packetType,
        int dataLength,
        long timestamp
) {
    public static final int HEADER_SIZE = 14; // short + int + long = 2 + 4 + 8 = 14

    public static PacketHeader fromBytes(byte[] bytes) {
        if (bytes.length != HEADER_SIZE) {
            throw new IllegalArgumentException("Packet Header 길이가 맞지 않습니다.");
        }

        ByteBuffer buffer = ByteBuffer.wrap(bytes);

        // Java는 기본 Big Endian
        // 명시해두기 위해 작성해두었지만, 혹시 모를 성능을 대비해 주석 처리
        // buffer.order(ByteOrder.BIG_ENDIAN);

        short packetType = buffer.getShort();
        int dataLength = buffer.getInt();
        long timestamp = buffer.getLong();

        return new PacketHeader(packetType, dataLength, timestamp);
    }

    public byte[] toBytes() {
        ByteBuffer buffer = ByteBuffer.allocate(HEADER_SIZE);

        // Java는 기본 Big Endian
        // 명시해두기 위해 작성해두었지만, 혹시 모를 성능을 대비해 주석 처리
        // buffer.order(ByteOrder.BIG_ENDIAN);

        buffer.putShort(packetType);
        buffer.putInt(dataLength);
        buffer.putLong(timestamp);

        return buffer.array();
    }
}
