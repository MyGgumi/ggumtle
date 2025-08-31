package com.ggumtle.ggumtle.server.pipeline;

import com.ggumtle.ggumtle.server.packet.Packet;
import io.netty.buffer.ByteBuf;
import io.netty.channel.ChannelHandlerContext;
import io.netty.handler.codec.MessageToByteEncoder;
import lombok.extern.slf4j.Slf4j;

@Slf4j
public class PacketEncoder extends MessageToByteEncoder<Packet> {

    @Override
    protected void encode(ChannelHandlerContext ctx, Packet packet, ByteBuf out) throws Exception {
        // 헤더를 바이트로 변환
        byte[] headerBytes = packet.header().toBytes();
        out.writeBytes(headerBytes);

        // 데이터가 있으면 추가
        if (packet.hasData()) {
            out.writeBytes(packet.data());
        }

        log.debug("패킷 인코딩 완료 - Type: {}, Length: {}", packet.header().packetType(), packet.getDataLength());
    }
}
