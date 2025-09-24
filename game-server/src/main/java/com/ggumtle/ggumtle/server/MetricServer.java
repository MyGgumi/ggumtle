package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.property.MetricServerProperty;
import com.ggumtle.ggumtle.monitor.metric.MetricRegistry;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.*;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.SocketChannel;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.handler.codec.http.*;
import jakarta.annotation.PostConstruct;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.stereotype.Component;

@Component
@Slf4j
public class MetricServer {

    private final int port;
    private final ApplicationEventPublisher applicationEventPublisher;

    public MetricServer(MetricServerProperty metricServerProperty, ApplicationEventPublisher applicationEventPublisher) {
        this.port = metricServerProperty.getPort();
        this.applicationEventPublisher = applicationEventPublisher;
    }

    @PostConstruct
    public void start() throws InterruptedException {
        EventLoopGroup bossGroup = new NioEventLoopGroup(1);
        EventLoopGroup workerGroup = new NioEventLoopGroup();

        ServerBootstrap b = new ServerBootstrap();
        b.group(bossGroup, workerGroup)
                .channel(NioServerSocketChannel.class)
                .childHandler(new ChannelInitializer<SocketChannel>() {
                    @Override
                    protected void initChannel(SocketChannel ch) {
                        ch.pipeline().addLast(new HttpServerCodec());
                        ch.pipeline().addLast(new HttpObjectAggregator(65536));
                        ch.pipeline().addLast(new SimpleChannelInboundHandler<FullHttpRequest>() {
                            @Override
                            protected void channelRead0(ChannelHandlerContext ctx, FullHttpRequest req) {
                                if (req.uri().equals("/metrics")) {
                                    String response = MetricRegistry.registry.scrape();
                                    FullHttpResponse httpResponse = new DefaultFullHttpResponse(
                                            HttpVersion.HTTP_1_1, HttpResponseStatus.OK,
                                            io.netty.buffer.Unpooled.copiedBuffer(response.getBytes())
                                    );
                                    httpResponse.headers().set(HttpHeaderNames.CONTENT_TYPE, "text/plain; version=0.0.4");
                                    httpResponse.headers().setInt(HttpHeaderNames.CONTENT_LENGTH, response.getBytes().length);
                                    ctx.writeAndFlush(httpResponse).addListener(ChannelFutureListener.CLOSE);
                                } else {
                                    ctx.writeAndFlush(new DefaultFullHttpResponse(
                                            HttpVersion.HTTP_1_1, HttpResponseStatus.NOT_FOUND
                                    ));
                                }
                            }
                        });
                    }
                });

        b.bind(port).sync();
        log.info("매트릭 서버 시작: {}", port);
    }
}


