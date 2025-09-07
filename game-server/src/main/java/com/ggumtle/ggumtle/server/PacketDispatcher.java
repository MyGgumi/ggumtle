package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.PacketHeader;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import com.ggumtle.ggumtle.session.Session;
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
import java.util.Optional;

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

        log.info("[{}] 수신한 패킷 데이터: {}", ctx.channel().id(), packet.data());

        // 핸들러로 디스패치
        HandlerInfo handlerInfo = commandHandlerMap.get(receivePacketType);
        if (handlerInfo == null) {
            throw new RuntimeException("알 수 없는 패킷 타입: " + packet.header().packetType());
        }

        Object[] parameters;
        if (packet.data() == null || packet.data().length == 0) {
            parameters = parseParameter(handlerInfo, ctx, packet.header());
        }
        else {
            parameters = parseParameterWithData(handlerInfo, packet.data(), ctx, packet.header());
        }

        log.info("[{}] 핸들러: {}\n파라미터: {}", ctx.channel().id(), handlerInfo, Arrays.toString(parameters));
        try {
            handlerInfo.method.invoke(handlerInfo.bean, parameters);
        } catch (InvocationTargetException | IllegalAccessException e) {
            log.error("[{}] 핸들러 실행 중 오류 발생: {}", ctx.channel().id(), e.getMessage());
            e.printStackTrace();
        }
    }

    private Object[] parseParameter(HandlerInfo handlerInfo, ChannelHandlerContext ctx, PacketHeader header) {
        Object[] parameters = new Object[handlerInfo.parameterTypes.length];

        try {
            for (int i = 0; i < handlerInfo.parameterTypes.length; i++) {
                Class<?> parameterType = handlerInfo.parameterTypes[i];

                if (Session.class.isAssignableFrom(parameterType)) {
                    Optional<Session> optionalSession = channelManager.getSession(ctx.channel());

                    if (optionalSession.isEmpty()) {
                        throw new RuntimeException("Session 정보를 요청하는 핸들러를 호출하였으나 채널에 세션이 없습니다");
                    }

                    parameters[i] = optionalSession.get();
                    continue;
                }

                if (Timestamp.class.isAssignableFrom(parameterType)) {
                    parameters[i] = new Timestamp(header.timestamp());
                    continue;
                }

                throw new RuntimeException("Data가 없는데 핸들러가 파라미터를 요구합니다");
            }
        } catch (Exception e) {
            log.error("[{}] 패킷 데이터 직렬화 중 오류 발생: {}", ctx.channel().id(), e.getMessage());
            e.printStackTrace();
        }

        return parameters;
    }

    private Object[] parseParameterWithData(HandlerInfo handlerInfo, byte[] data, ChannelHandlerContext ctx, PacketHeader header) {
        ByteBuffer buffer = ByteBuffer.wrap(data);
        Object[] parameters = new Object[handlerInfo.parameterTypes.length];

        try {
            for (int i = 0; i < handlerInfo.parameterTypes.length; i++) {
                Class<?> parameterType = handlerInfo.parameterTypes[i];

                if (Session.class.isAssignableFrom(parameterType)) {
                    Optional<Session> optionalSession = channelManager.getSession(ctx.channel());

                    if (optionalSession.isEmpty()) {
                        throw new RuntimeException("Session 정보를 요청하는 핸들러를 호출하였으나 채널에 세션이 없습니다");
                    }

                    parameters[i] = optionalSession.get();
                    continue;
                }

                if (Timestamp.class.isAssignableFrom(parameterType)) {
                    parameters[i] = new Timestamp(header.timestamp());
                    continue;
                }

                parameters[i] = packetMapper.getInstance(parameterType, buffer);
            }
        } catch (Exception e) {
            log.error("[{}] 패킷 데이터 직렬화 중 오류 발생: {}", ctx.channel().id(), e.getMessage());
            e.printStackTrace();
        }

        return parameters;
    }

    private record HandlerInfo(
            ReceivePacketType type,
            Object bean,
            Method method,
            Class<?>[] parameterTypes
    ) {
    }
}
