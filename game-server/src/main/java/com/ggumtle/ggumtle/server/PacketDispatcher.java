package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.server.packet.Packet;
import com.ggumtle.ggumtle.server.packet.PacketType;
import io.netty.channel.ChannelHandlerContext;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationContext;
import org.springframework.context.ApplicationListener;
import org.springframework.context.event.ContextRefreshedEvent;
import org.springframework.stereotype.Component;

import java.lang.reflect.Method;
import java.util.EnumMap;

@Slf4j
@Component
@RequiredArgsConstructor
public class PacketDispatcher implements ApplicationListener<ContextRefreshedEvent> {

    private final ApplicationContext applicationContext;
    private final EnumMap<PacketType, HandlerInfo> commandHandlerMap = new EnumMap<>(PacketType.class);
    private final PacketMapper packetMapper;

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

                PacketType type = annotation.type();
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
        HandlerInfo handlerInfo = commandHandlerMap.get(packet.header().packetType());
        if (handlerInfo == null) {
            throw new RuntimeException("알 수 없는 패킷 타입: " + packet.header().packetType());
        }

        System.out.println("패킷 data():" + packet.data());

        try {
            Command command = packetMapper.getCommand(packet.header().packetType(), packet.data());
            handlerInfo.method.invoke(handlerInfo.bean, command);

            log.info("패킷 처리 완료: {}", packet.header().packetType());
        } catch (Exception e) {
            log.error("패킷 처리 중 오류가 발생했습니다.", e);
            e.printStackTrace();
        }
    }

    private record HandlerInfo(
            PacketType type,
            Object bean,
            Method method,
            Class<?>[] parameterTypes
    ) {
    }
}
