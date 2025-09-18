package com.ggumtle.ggumtle.infra.redis;

import com.ggumtle.ggumtle.messaging.DreamMessageListener;
import com.ggumtle.ggumtle.messaging.RoomMessageManager;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.listener.PatternTopic;
import org.springframework.data.redis.listener.RedisMessageListenerContainer;

@Configuration
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class RedisListenerConfig {

    private static final String DREAM_CREATED_CHANEL = "created_room";
    private static final String END_DREAM_CHANNEL = "end_dream";

    private final RoomMessageManager roomMessageManager;
    private final DreamMessageListener dreamMessageListener;

    @Bean
    public RedisMessageListenerContainer redisContainer(RedisConnectionFactory connectionFactory) {
        RedisMessageListenerContainer container = new RedisMessageListenerContainer();
        container.setConnectionFactory(connectionFactory);

        container.addMessageListener(roomMessageManager, new PatternTopic(DREAM_CREATED_CHANEL));
        container.addMessageListener(dreamMessageListener, new PatternTopic(END_DREAM_CHANNEL));

        return container;
    }
}
