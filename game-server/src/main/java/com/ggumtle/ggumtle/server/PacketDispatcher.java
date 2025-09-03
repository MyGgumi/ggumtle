package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.dto.Command;
import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.session.Session;
import io.netty.channel.ChannelHandlerContext;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationContext;
import org.springframework.context.ApplicationListener;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.stereotype.Component;

import java.lang.reflect.Method;
import java.nio.charset.StandardCharsets;
import java.util.EnumMap;

@Slf4j
@Component
@RequiredArgsConstructor
public class PacketDispatcher implements ApplicationListener<ContextRefreshedEvent> {

    private final ApplicationContext applicationContext;
    private final EnumMap<ReceivePacketType, HandlerInfo> commandHandlerMap = new EnumMap<>(ReceivePacketType.class);
    private final PacketMapper packetMapper;
    private final ChannelManager channelManager;

    @Override
    public void onApplicationEvent(ContextRefreshedEvent event) {
        initialize();
    }

    private void initialize() {
        for (String beanName : applicationContext.getBeanDefinitionNames()) {
            Object bean = applicationContext.getBean(beanName);

            for (Method method : bean.getClass().getMethods()) {
                PacketCommandHandler annotation = method.getAnnotation(PacketCommandHandler.class);

                if (annotation == null) {
                    continue;
                }

                ReceivePacketType type = annotation.type();
                Class<?>[] parameterTypes = method.getParameterTypes();

                for (Class<?> parameterType : parameterTypes) {
                    if (Command.class.isAssignableFrom(parameterType)) {
                        continue;
                    }

                    if (Session.class.isAssignableFrom(parameterType)) {
                        continue;
                    }

                    throw new RuntimeException("파라미터가 Command나 Session형이 아닙니다");
                }

                commandHandlerMap.put(type, new HandlerInfo(type, bean, method, parameterTypes));
            }
        }
    }

    public void dispatch(ChannelHandlerContext ctx, Packet packet) {
        // 채널 JWT 인증 처리
        ReceivePacketType receivePacketType = ReceivePacketType.fromValue(packet.header().packetType());
        if (receivePacketType == ReceivePacketType.VERIFY_TOKEN) {
            String token = new String(packet.data(), StandardCharsets.UTF_8);
            channelManager.authorizeChannel(ctx.channel(), token);

            return;
        }

        if (!channelManager.isAuthorized(ctx.channel())) {
            throw new RuntimeException("채널 인증 전입니다");
        }

        // 핸들러로 디스패치
        HandlerInfo handlerInfo = commandHandlerMap.get(receivePacketType);
        if (handlerInfo == null) {
            throw new RuntimeException("알 수 없는 패킷 타입: " + packet.header().packetType());
        }

        log.info("수신한 패킷 data():" + packet.data());

        try {
            Object[] parameters = new Object[handlerInfo.parameterTypes.length];

            for (int i = 0; i < handlerInfo.parameterTypes.length; i++) {
                Class<?> parameterType = handlerInfo.parameterTypes[i];

                if (Command.class.isAssignableFrom(parameterType)) {
                    Command command = packetMapper.getCommand(receivePacketType, packet.data());
                    parameters[i] = command;
                    continue;
                }

                if (Session.class.isAssignableFrom(parameterType)) {
                    parameters[i] = channelManager.getSession(ctx.channel());
                }
            }

            handlerInfo.method.invoke(handlerInfo.bean, parameters);

            log.info("패킷 처리 완료: {}", receivePacketType);
        } catch (Exception e) {
            log.error("패킷 처리 중 오류가 발생했습니다.", e);
            e.printStackTrace();
        }
    }

    private record HandlerInfo(
            ReceivePacketType type,
            Object bean,
            Method method,
            Class<?>[] parameterTypes
    ) {
    }
}
