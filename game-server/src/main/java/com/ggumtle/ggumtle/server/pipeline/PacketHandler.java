package com.ggumtle.ggumtle.server.pipeline;

import com.ggumtle.ggumtle.server.PacketDispatcher;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.session.SessionManager;
import io.netty.channel.ChannelHandler;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@ChannelHandler.Sharable
@RequiredArgsConstructor
public class PacketHandler extends SimpleChannelInboundHandler<Packet> {

    private final SessionManager sessionManager;
    private final PacketDispatcher packetDispatcher;

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, Packet packet) {
        packetDispatcher.dispatch(ctx, packet);
    }

    @Override
    public void channelActive(ChannelHandlerContext ctx) {
        sessionManager.createSession(ctx.channel());

        log.info("클라이언트 연결됨: {}, 전체 서버 인원 수: {}", ctx.channel().remoteAddress(), sessionManager.getSessions().size());
    }

    @Override
    public void channelInactive(ChannelHandlerContext ctx) {
        sessionManager.removeSession(ctx.channel());

        log.info("클라이언트 연결 해제됨: {}, 전체 서버 인원 수: {}", ctx.channel().remoteAddress(), sessionManager.getSessions().size());
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        log.error("채널 예외 발생", cause);
        ctx.close();
    }
}
