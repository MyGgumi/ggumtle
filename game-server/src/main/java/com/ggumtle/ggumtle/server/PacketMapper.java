package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.common.dto.Timestamp;
import com.ggumtle.ggumtle.server.packet.PacketHeader;
import io.netty.channel.Channel;
import io.netty.channel.ChannelHandlerContext;
import lombok.extern.slf4j.Slf4j;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.ParameterizedType;
import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;

@Slf4j
public class PacketMapper {
    public static Object decodePacket(ChannelHandlerContext ctx, PacketHeader header, ByteBuffer buffer, Class<?> type) {
        try {
            if (Channel.class.isAssignableFrom(type)) {
                return ctx.channel();
            }

            if (Timestamp.class.isAssignableFrom(type)) {
                return new Timestamp(header.timestamp());
            }

            return decodeObject(type, buffer);
        } catch (Exception e) {
            log.error("[{}] 패킷 직렬화 중 오류 발생: {}", ctx.channel().id(), e.getMessage());
            e.printStackTrace();
        }

        return null;
    }

    public static Object[] decodePacket(ChannelHandlerContext ctx, PacketHeader header, ByteBuffer buffer, Class<?>... types) {
        Object[] instances = new Object[types.length];

        for (int i = 0; i < types.length; i++) {
            instances[i] = decodePacket(ctx, header, buffer, types[i]);
        }

        return instances;
    }

    public static <T> T decodeObject(Class<T> clazz, ByteBuffer buffer) throws InvocationTargetException, InstantiationException, IllegalAccessException {
        Constructor<?> constructor = clazz.getConstructors()[0];
        Type[] parameters = constructor.getGenericParameterTypes();

        Object[] constructorArgs = new Object[parameters.length];

        for (int i = 0; i < parameters.length; i++) {
            Type type = parameters[i];

            if (type instanceof Class<?> classType) {
                if (classType.equals(short.class) || classType.equals(Short.class)) {
                    constructorArgs[i] = buffer.getShort();
                    continue;
                }
                if (classType.equals(int.class) || classType.equals(Integer.class)) {
                    constructorArgs[i] = buffer.getInt();
                    continue;
                }
                if (classType.equals(long.class) || classType.equals(Long.class)) {
                    constructorArgs[i] = buffer.getLong();
                    continue;
                }
                if (classType.equals(byte.class) || classType.equals(Byte.class)) {
                    constructorArgs[i] = buffer.get();
                    continue;
                }
                if (classType.equals(boolean.class) || classType.equals(Boolean.class)) {
                    constructorArgs[i] = buffer.get() != 0;
                    continue;
                }
            }

            if (type instanceof ParameterizedType parameterizedType) {
                Class<?> rawType = (Class<?>) parameterizedType.getRawType();

                Type elementType = parameterizedType.getActualTypeArguments()[0];
                Class<?> elementClass;
                if (elementType instanceof Class<?>) {
                    elementClass = (Class<?>) elementType;
                } else if (elementType instanceof ParameterizedType pt) {
                    elementClass = (Class<?>) pt.getRawType();
                } else {
                    throw new RuntimeException("지원하지 않는 요소 타입: " + elementType);
                }

                if (rawType.equals(List.class)) {
                    int count = buffer.getInt();
                    List<Object> list = new ArrayList<>();

                    for (int j = 0; j < count; j++) {
                        Object object = decodeObject(elementClass, buffer);
                        list.add(object);
                    }

                    constructorArgs[i] = list;
                    continue;
                }

                log.warn("List형이 아닌 제네릭 타입 파라미터가 있습니다");
                throw new RuntimeException("다룰 수 없는 제네릭 타입 파라미터가 있어 데이터를 파싱할 수 없습니다");
            }
        }

        return (T) constructor.newInstance(constructorArgs);
    }
}
