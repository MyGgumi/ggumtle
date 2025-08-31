package com.ggumtle.ggumtle.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.dream.domain.DreamServer;
import com.ggumtle.ggumtle.messaging.event.CreatedRoomEvent;
import com.ggumtle.ggumtle.messaging.message.CreatedDreamMessage;
import com.ggumtle.ggumtle.messaging.message.RequestRoomMessage;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.data.redis.connection.Message;
import org.springframework.data.redis.connection.MessageListener;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Component;

import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class RoomMessageManager implements MessageListener {

    private static final String DREAM_REQUEST_PREFIX = "dream_request_";

    private final ApplicationEventPublisher applicationEventPublisher;
    private final RedisTemplate<String, String> redisTemplate;
    private final ObjectMapper objectMapper;

    public void sendMessage(DreamServer dreamServer, List<Long> playerIds) {
        String channel = DREAM_REQUEST_PREFIX + dreamServer.getId();
        RequestRoomMessage message = new RequestRoomMessage(playerIds);

        redisTemplate.convertAndSend(channel, message);
    }

    @Override
    public void onMessage(Message message, byte[] pattern) {
        try {
            CreatedDreamMessage createdDream = objectMapper.readValue(message.getBody(), CreatedDreamMessage.class);

            CreatedRoomEvent event = new CreatedRoomEvent(createdDream.roomId(), createdDream.dreamServerId());

            applicationEventPublisher.publishEvent(event);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
