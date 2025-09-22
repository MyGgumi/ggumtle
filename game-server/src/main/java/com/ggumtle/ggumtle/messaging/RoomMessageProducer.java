package com.ggumtle.ggumtle.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.common.event.DreamEndEvent;
import com.ggumtle.ggumtle.messaging.message.EndDreamMessage;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.context.event.EventListener;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class RoomMessageProducer {
    private static final String END_ROOM_CHANNEL = "end_dream";

    private final RedisTemplate<String, String> redisTemplate;
    private final ObjectMapper objectMapper;

    @EventListener
    public void produceEndDream(DreamEndEvent event) {
        try {
            EndDreamMessage message = EndDreamMessage.from(event);

            String json =  objectMapper.writeValueAsString(message);

            redisTemplate.convertAndSend(END_ROOM_CHANNEL, json);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
