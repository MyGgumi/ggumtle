package com.ggumtle.ggumtle.infra.redis;

import com.ggumtle.ggumtle.messaging.RoomMessageListener;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.data.redis.connection.RedisConnectionFactory;
import org.springframework.data.redis.listener.PatternTopic;
import org.springframework.data.redis.listener.RedisMessageListenerContainer;

@Configuration
public class RedisListenerConfig {

    private final String dreamRequestChannel;
    private final RoomMessageListener roomMessageListener;

    protected RedisListenerConfig(@Value("${game.server.id}") String gameServerId, RoomMessageListener roomMessageListener) {
        this.dreamRequestChannel = "dream_request_" + gameServerId;
        this.roomMessageListener = roomMessageListener;
    }

    @Bean
    public RedisMessageListenerContainer redisContainer(RedisConnectionFactory connectionFactory) {
        RedisMessageListenerContainer container = new RedisMessageListenerContainer();
        container.setConnectionFactory(connectionFactory);
        container.addMessageListener(roomMessageListener, new PatternTopic(dreamRequestChannel));

        return container;
    }
}
