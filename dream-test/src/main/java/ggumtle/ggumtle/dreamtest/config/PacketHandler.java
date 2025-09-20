package ggumtle.ggumtle.dreamtest.config;

import io.netty.channel.ChannelHandler;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.packet.Packet;
import org.springframework.stereotype.Component;

@Component
@ChannelHandler.Sharable
@Slf4j
public class PacketHandler extends SimpleChannelInboundHandler<Packet> {

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, Packet packet) {
        log.info("패킷 수신: {} - {}", packet.header(), packet.data());
    }

    @Override
    public void channelActive(ChannelHandlerContext ctx) {
        log.info("서버 연결됨: {}", ctx.channel().remoteAddress());
    }

    @Override
    public void channelInactive(ChannelHandlerContext ctx) {
        log.info("서버 연결 해제됨: {}", ctx.channel().remoteAddress());
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        log.error("채널 예외 발생", cause);
        ctx.close();
    }
}
