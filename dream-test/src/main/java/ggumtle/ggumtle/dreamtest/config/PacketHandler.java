package ggumtle.ggumtle.dreamtest.config;

import ggumtle.ggumtle.dreamtest.event.PlayerInfoEvent;
import ggumtle.ggumtle.dreamtest.event.StartGameEvent;
import ggumtle.ggumtle.dreamtest.packet.SendPacketType;
import io.netty.channel.ChannelHandler;
import io.netty.channel.ChannelHandlerContext;
import io.netty.channel.SimpleChannelInboundHandler;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import ggumtle.ggumtle.dreamtest.packet.Packet;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;

import java.util.concurrent.Executors;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.TimeUnit;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@ChannelHandler.Sharable
@Slf4j
public class PacketHandler extends SimpleChannelInboundHandler<Packet> {
    private final ScheduledExecutorService scheduler = Executors.newSingleThreadScheduledExecutor();
    private final ApplicationEventPublisher applicationEventPublisher;

    @Override
    protected void channelRead0(ChannelHandlerContext ctx, Packet packet) {
        log.info("패킷 수신: {} - {}", packet.header(), packet.data());

        handleMessage(packet);
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

    private void handleMessage(Packet packet) {
        if (packet.header().receivePacketType() == SendPacketType.INITIALIZE_PLAYER.getValue()) {
            PlayerInfoEvent event = PlayerInfoEvent.of(packet.data());
            applicationEventPublisher.publishEvent(event);
            return;
        }

        if (packet.header().receivePacketType() == SendPacketType.GAME_START.getValue()) {
            scheduler.schedule(() -> {
                StartGameEvent event = new StartGameEvent();
                applicationEventPublisher.publishEvent(event);
            }, 1, TimeUnit.SECONDS);
            return;
        }
    }
}
