package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.property.GameServerProperty;
import com.ggumtle.ggumtle.server.pipeline.PacketDecoder;
import com.ggumtle.ggumtle.server.pipeline.PacketEncoder;
import com.ggumtle.ggumtle.server.pipeline.PacketHandler;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.ChannelFuture;
import io.netty.channel.ChannelInitializer;
import io.netty.channel.ChannelOption;
import io.netty.channel.EventLoopGroup;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.SocketChannel;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.PreDestroy;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;


@Slf4j
@Component
public class GameServer {

    private final int port;
    private final PacketHandler packetHandler;

    private EventLoopGroup bossGroup;
    private EventLoopGroup workerGroup;
    private ChannelFuture serverChannelFuture;

    private GameServer(GameServerProperty gameServerProperty, PacketHandler packetHandler) {
        this.port = gameServerProperty.getPort();
        this.packetHandler = packetHandler;
    }

    @PostConstruct
    public void start() throws Exception {
        log.info("게임 서버를 시작합니다. 포트: {}", port);

        bossGroup = new NioEventLoopGroup(1);
        workerGroup = new NioEventLoopGroup();

        try {
            ServerBootstrap bs = new ServerBootstrap();
            bs.group(bossGroup, workerGroup)
                    .channel(NioServerSocketChannel.class)
                    .childHandler(new ChannelInitializer<SocketChannel>() {
                        @Override
                        protected void initChannel(SocketChannel ch) throws Exception {
                            ch.pipeline()
                                    .addLast(new PacketDecoder()) // 바이너리 패킷 디코더
                                    .addLast(new PacketEncoder()) // 바이너리 패킷 인코더
                                    .addLast(packetHandler); // 패킷 처리 핸들러
                        }
                    })
                    .option(ChannelOption.SO_BACKLOG, 128)
                    .childOption(ChannelOption.SO_KEEPALIVE, true);

            serverChannelFuture = bs.bind(port).sync();
            log.info("게임 서버가 포트 {}에서 시작되었습니다.", port);

        } catch (Exception e) {
            log.error("게임 서버 시작 중 오류 발생", e);
            throw e;
        }
    }

    @PreDestroy
    public void stop() {
        log.info("게임 서버를 종료합니다.");

        if (serverChannelFuture != null) {
            serverChannelFuture.channel().close();
        }

        if (bossGroup != null) {
            bossGroup.shutdownGracefully();
        }

        if (workerGroup != null) {
            workerGroup.shutdownGracefully();
        }
    }
}
