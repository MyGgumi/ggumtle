package com.ggumtle.ggumtle.server;

import com.ggumtle.ggumtle.server.packet.PacketType;

import java.lang.annotation.ElementType;
import java.lang.annotation.Retention;
import java.lang.annotation.RetentionPolicy;
import java.lang.annotation.Target;

@Target(ElementType.METHOD)
@Retention(RetentionPolicy.RUNTIME)
public @interface PacketCommandHandler {

    PacketType type();
}
