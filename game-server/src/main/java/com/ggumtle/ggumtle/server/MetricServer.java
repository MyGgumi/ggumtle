package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.property.MetricServerProperty;
import com.ggumtle.ggumtle.monitor.metric.MetricRegistry;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.sun.management.OperatingSystemMXBean;
import io.micrometer.core.instrument.Gauge;
import io.netty.bootstrap.ServerBootstrap;
import io.netty.channel.*;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.SocketChannel;
import io.netty.channel.socket.nio.NioServerSocketChannel;
import io.netty.handler.codec.http.*;
import jakarta.annotation.PostConstruct;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;

import java.lang.management.ManagementFactory;
import java.lang.management.MemoryMXBean;

@Component
@Slf4j
public class MetricServer {

    private final int port;
    private final ChannelManager channelManager;

    @Autowired
    public MetricServer(MetricServerProperty metricServerProperty, ChannelManager channelManager) {
        this.port = metricServerProperty.getPort();
        this.channelManager = channelManager;
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

        OperatingSystemMXBean osBean = ManagementFactory.getPlatformMXBean(OperatingSystemMXBean.class);
        Gauge.builder("jvm_process_cpu_load", osBean, OperatingSystemMXBean::getProcessCpuLoad)
                .description("JVM 프로세스 CPU 사용률")
                .register(MetricRegistry.registry);

        Gauge.builder("system_cpu_load", osBean, OperatingSystemMXBean::getSystemCpuLoad)
                .description("시스템 전체 CPU 사용률")
                .register(MetricRegistry.registry);

        MemoryMXBean memoryMXBean = ManagementFactory.getMemoryMXBean();
        Gauge.builder("jvm_memory_used_bytes", memoryMXBean,
                        m -> m.getHeapMemoryUsage().getUsed())
                .description("JVM Heap 사용량")
                .register(MetricRegistry.registry);

        Gauge.builder("jvm_memory_committed_bytes", memoryMXBean,
                        m -> m.getHeapMemoryUsage().getCommitted())
                .description("JVM Heap 예약량")
                .register(MetricRegistry.registry);

        Gauge.builder("jvm_threads_count", () -> ManagementFactory.getThreadMXBean().getThreadCount())
                .description("JVM 쓰레드 수")
                .register(MetricRegistry.registry);

        Gauge.builder("game_active_channels", channelManager, ChannelManager::getChannelCount)
                .description("현재 활성화된 채널 수")
                .register(MetricRegistry.registry);

        b.bind(port).sync();
        log.info("매트릭 서버 시작: {}", port);
    }
}


