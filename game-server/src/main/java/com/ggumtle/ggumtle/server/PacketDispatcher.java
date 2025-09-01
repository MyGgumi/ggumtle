package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.dto.Command;
import com.ggumtle.ggumtle.common.PacketCommandHandler;
import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
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

                if (annotation == null) continue;

                ReceivePacketType type = annotation.type();
                Class<?>[] parameterTypes = method.getParameterTypes();

                // ctx, command
                if (parameterTypes.length != 1) continue;

                if (!Command.class.isAssignableFrom(parameterTypes[0])) {
                    throw new RuntimeException("두 번째 파라미터는 Command 여야 합니다.");
                }

                commandHandlerMap.put(type, new HandlerInfo(type, bean, method, parameterTypes));
            }
        }
    }

    public void dispatch(ChannelHandlerContext ctx, Packet packet) {
        ReceivePacketType receivePacketType = ReceivePacketType.fromValue(packet.header().packetType());
        if (receivePacketType == ReceivePacketType.VERIFY_TOKEN) {
            String token = new String(packet.data(), StandardCharsets.UTF_8);
            channelManager.authorizeChannel(ctx.channel(), token);

            return;
        }

        if (!channelManager.isAuthorized(ctx.channel())) {
            throw new RuntimeException("채널 인증 전입니다");
        }

        HandlerInfo handlerInfo = commandHandlerMap.get(receivePacketType);
        if (handlerInfo == null) {
            throw new RuntimeException("알 수 없는 패킷 타입: " + packet.header().packetType());
        }

        log.info("패킷 data():" + packet.data());

        try {
            Command command = packetMapper.getCommand(receivePacketType, packet.data());
            handlerInfo.method.invoke(handlerInfo.bean, command);

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
