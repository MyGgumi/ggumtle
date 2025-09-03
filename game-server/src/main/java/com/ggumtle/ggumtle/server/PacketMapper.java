package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.dto.Command;
import com.ggumtle.ggumtle.server.packet.ReceivePacketType;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.lang.reflect.Parameter;
import java.nio.ByteBuffer;

@Component
@RequiredArgsConstructor
public class PacketMapper {

    public <T extends Command> T getCommand(ReceivePacketType receivePacketType, byte[] bytes) throws Exception {
        Class<? extends Command> clazz = receivePacketType.getClazz();

        ByteBuffer buffer = ByteBuffer.wrap(bytes);

        Object[] constructorArgs = new Object[clazz.getConstructors()[0].getParameterCount()];
        Parameter[] parameters = clazz.getConstructors()[0].getParameters();

        for (int i = 0; i < parameters.length; i++) {
            Parameter parameter = parameters[i];
            if (parameter.getType().equals(short.class) || parameter.getType().equals(Short.class)) {
                constructorArgs[i] = buffer.getShort();
            } else if (parameter.getType().equals(int.class) || parameter.getType().equals(Integer.class)) {
                constructorArgs[i] = buffer.getInt();
            } else if (parameter.getType().equals(long.class) || parameter.getType().equals(Long.class)) {
                constructorArgs[i] = buffer.getLong();
            }
        }

        return (T) clazz.cast(clazz.getConstructors()[0].newInstance(constructorArgs));
    }
}
