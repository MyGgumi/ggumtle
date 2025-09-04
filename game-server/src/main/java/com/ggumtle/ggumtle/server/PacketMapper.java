package com.ggumtle.ggumtle.server;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.lang.reflect.Constructor;
import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.ParameterizedType;
import java.lang.reflect.Type;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;

@Component
@RequiredArgsConstructor
@Slf4j
public class PacketMapper {

    public <T> T getInstance(Class<T> clazz, ByteBuffer buffer) throws InvocationTargetException, InstantiationException, IllegalAccessException {
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
                        Object object = getInstance(elementClass, buffer);
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
