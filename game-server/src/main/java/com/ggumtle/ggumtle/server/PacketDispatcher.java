package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.annotation.PacketCommandHandler;
import com.ggumtle.ggumtle.dream.application.tickevent.TickEvent;
import com.ggumtle.ggumtle.room.application.RoomService;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.auth.body.AuthBody;
import io.netty.channel.ChannelHandlerContext;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationContext;
import org.springframework.context.ApplicationListener;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.stereotype.Component;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.EnumMap;
import java.util.ServiceLoader;


@Slf4j
@Component
@RequiredArgsConstructor
public class PacketDispatcher implements ApplicationListener<ContextRefreshedEvent> {

    private final ApplicationContext applicationContext;
    private final EnumMap<ReceivePacketType, HandlerInfo> commandHandlerMap = new EnumMap<>(ReceivePacketType.class);
    private final EnumMap<ReceivePacketType, TickEventInfo> tickEventMap = new EnumMap<>(ReceivePacketType.class);
    private final ChannelManager channelManager;
    private final RoomService roomService;

    @Override
    public void onApplicationEvent(ContextRefreshedEvent event) {
        initializePacketCommandHandler();
        initializeTickEvent();
    }

    private void initializePacketCommandHandler() {
        for (String beanName : applicationContext.getBeanDefinitionNames()) {
            Object bean = applicationContext.getBean(beanName);

            for (Method method : bean.getClass().getMethods()) {
                PacketCommandHandler annotation = method.getAnnotation(PacketCommandHandler.class);

                if (annotation == null) {
                    continue;
                }

                ReceivePacketType type = annotation.type();
                Class<?>[] parameterTypes = method.getParameterTypes();

                commandHandlerMap.put(type, new HandlerInfo(type, bean, method, parameterTypes));
            }
        }
    }

    private void initializeTickEvent() {
        ServiceLoader<TickEvent> loader = ServiceLoader.load(TickEvent.class);

        for (TickEvent event : loader) {
            tickEventMap.put(
                    event.type(),
                    new TickEventInfo(event.getClass())
            );
        }
    }

    public void dispatch(ChannelHandlerContext ctx, Packet packet) {
        // 채널 JWT 인증 처리
        ReceivePacketType receivePacketType = ReceivePacketType.fromValue(packet.header().packetType());
        if (receivePacketType == ReceivePacketType.VERIFY_TOKEN) {
            String token = new String(packet.data(), StandardCharsets.UTF_8);
            boolean result = channelManager.authorizeChannel(ctx.channel(), token);

            AuthBody body = new AuthBody(true);
            ctx.channel().writeAndFlush(body);

            return;
        }

        if (!ChannelManager.isAuthenticated(ctx.channel())) {
            log.error("[{}] 채널 인증 전에 인증 요청이 아닌 다른 요청을 수신함: {}", ctx.channel().id(), packet.data());
            throw new RuntimeException("채널 인증 전입니다");
        }

        log.debug("[{}] 수신한 패킷 데이터: {}", ctx.channel().id(), packet.data());

        // 핸들러로 디스패치
        HandlerInfo handlerInfo = commandHandlerMap.get(receivePacketType);
        if (handlerInfo != null) {
            dispatchHandlerCommand(handlerInfo, ctx, packet);
            return;
        }

        // 이벤트 큐로 디스패치
        TickEventInfo tickEventInfo = tickEventMap.get(receivePacketType);
        if (tickEventInfo != null) {
            dispatchTickEvent(tickEventInfo, ctx, packet);
            return;
        }

        throw new RuntimeException("알 수 없는 패킷 타입: " + packet.header().packetType());
    }

    private void dispatchHandlerCommand(HandlerInfo handlerInfo, ChannelHandlerContext ctx, Packet packet) {
        Object[] parameters = PacketMapper.decodePacket(ctx, packet.header(), packet.data() != null ? ByteBuffer.wrap(packet.data()) : ByteBuffer.allocate(0), handlerInfo.parameterTypes);

        log.debug("[{}] 핸들러: {}\n파라미터: {}", ctx.channel().id(), handlerInfo, Arrays.toString(parameters));
        try {
            handlerInfo.method.invoke(handlerInfo.bean, parameters);
        } catch (InvocationTargetException | IllegalAccessException e) {
            log.error("[{}] 핸들러 실행 중 오류 발생: {}", ctx.channel().id(), e.getMessage());
            e.printStackTrace();
        }
    }

    private void dispatchTickEvent(TickEventInfo tickEventInfo, ChannelHandlerContext ctx, Packet packet) {
        Object instance = PacketMapper.decodePacket(ctx, packet.header(), ByteBuffer.wrap(packet.data()), tickEventInfo.clazz);

        if (instance == null) {
            log.error("[{}] 틱 이벤트가 올바르게 파싱되지 않았습니다", ctx.channel().id());
            return;
        }

        TickEvent event = (TickEvent) instance;

        roomService.dispatchToRoom(event, ctx.channel());
    }

    private record HandlerInfo(
            ReceivePacketType type,
            Object bean,
            Method method,
            Class<?>[] parameterTypes
    ) {
    }

    private record TickEventInfo(
            Class<? extends TickEvent> clazz
    ) {
    }
}
