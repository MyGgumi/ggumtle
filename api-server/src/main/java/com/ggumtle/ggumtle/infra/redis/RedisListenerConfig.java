package com.ggumtle.ggumtle.infra.redis;

import com.ggumtle.ggumtle.dream.application.DreamService;
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

    private static final String DREAM_CREATED_CHANEL = "dream_created";

    private final RoomMessageManager roomMessageManager;

    @Bean
    public RedisMessageListenerContainer redisContainer(RedisConnectionFactory connectionFactory) {
        RedisMessageListenerContainer container = new RedisMessageListenerContainer();
        container.setConnectionFactory(connectionFactory);
        container.addMessageListener(roomMessageManager, new PatternTopic(DREAM_CREATED_CHANEL));

        return container;
    }
}
