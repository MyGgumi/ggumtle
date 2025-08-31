package com.ggumtle.ggumtle.server.pipeline;

import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.PacketHeader;
import io.netty.buffer.ByteBuf;
import io.netty.channel.ChannelHandlerContext;
import io.netty.handler.codec.ByteToMessageDecoder;
import lombok.extern.slf4j.Slf4j;

import java.util.List;

@Slf4j
public class PacketDecoder extends ByteToMessageDecoder {

    @Override
    protected void decode(ChannelHandlerContext ctx, ByteBuf in, List<Object> out) throws Exception {
        // 헤더 크기만큼 읽을 수 있는지 확인
        if (in.readableBytes() < PacketHeader.HEADER_SIZE) {
            return; // 더 많은 데이터를 기다림
        }

        // 현재 읽기 위치를 마킹
        in.markReaderIndex();

        // 헤더 읽기
        byte[] headerBytes = new byte[PacketHeader.HEADER_SIZE];
        in.readBytes(headerBytes);

        PacketHeader header = PacketHeader.fromBytes(headerBytes);

        // 데이터 길이 확인
        if (in.readableBytes() < header.dataLength()) {
            in.resetReaderIndex(); // 읽기 위치를 되돌림
            return; // 더 많은 데이터를 기다림
        }

        // 데이터 읽기 (있는 경우)
        byte[] data = null;
        if (header.dataLength() > 0) {
            data = new byte[(int) header.dataLength()];
            in.readBytes(data);
        }

        // 패킷 객체 생성
        Packet packet = new Packet(header, data);
        out.add(packet);

        log.debug("패킷 디코딩 완료 - Type: {}, Length: {}", header.packetType(), header.dataLength());
    }
}
