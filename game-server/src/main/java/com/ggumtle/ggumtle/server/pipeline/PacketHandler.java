package com.ggumtle.ggumtle.server.pipeline;

import com.ggumtle.ggumtle.room.application.RoomManager;
import com.ggumtle.ggumtle.server.PacketDispatcher;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.session.Session;
import io.netty.channel.ChannelHandler;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.Optional;

@Slf4j
@Component
@ChannelHandler.Sharable
@RequiredArgsConstructor
public class PacketHandler extends SimpleChannelInboundHandler<Packet> {

    private final ChannelManager channelManager;
    private final RoomManager roomManager;
    private final PacketDispatcher packetDispatcher;

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, Packet packet) {
        packetDispatcher.dispatch(ctx, packet);
    }

    @Override
    public void channelActive(ChannelHandlerContext ctx) {
        channelManager.addChannel(ctx.channel());

        log.info("클라이언트 연결됨: {}, 전체 채널 수: {}", ctx.channel().remoteAddress(), channelManager.getChannelCount());
    }

    @Override
    public void channelInactive(ChannelHandlerContext ctx) {
        Optional<Session> optionalSession = channelManager.getSession(ctx.channel());
        optionalSession.ifPresent(session -> roomManager.removeSession(session));

        channelManager.removeChannel(ctx.channel());

        log.info("클라이언트 연결 해제됨: {}", ctx.channel().remoteAddress());
    }

    @Override
    public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) {
        log.error("채널 예외 발생", cause);
        ctx.close();
    }
}
