package com.ggumtle.ggumtle.common.annotation;

import com.ggumtle.ggumtle.server.packet.ReceivePacketType;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Target(ElementType.TYPE)
@Retention(RetentionPolicy.RUNTIME)
public @interface TickEventType {
    ReceivePacketType type();
}
