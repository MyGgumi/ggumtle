package com.ggumtle.ggumtle.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.dream.domain.OptimalServer;
import com.ggumtle.ggumtle.messaging.event.CreatedRoomEvent;
import com.ggumtle.ggumtle.messaging.message.CreatedDreamMessage;
import com.ggumtle.ggumtle.messaging.message.RequestRoomMessage;
import com.ggumtle.ggumtle.messaging.payload.RequestRoomPayload;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.ApplicationEventPublisher;
import org.springframework.data.redis.connection.Message;
import org.springframework.data.redis.connection.MessageListener;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class RoomMessageManager implements MessageListener {

    private static final String DREAM_REQUEST_PREFIX = "dream_request_";

    private final ApplicationEventPublisher applicationEventPublisher;
    private final MessageSender messageSender;
    private final ObjectMapper objectMapper;

    public void sendMessage(OptimalServer dreamServer, RequestRoomPayload payload) {
        String channel = DREAM_REQUEST_PREFIX + dreamServer.getId();
        RequestRoomMessage message = RequestRoomMessage.of(payload);

        log.info("레디스에 메시지 발행: 채널={}, 메시지={}", channel, message);
        messageSender.sendMessage(channel, message);
    }

    @Override
    public void onMessage(Message message, byte[] pattern) {
        try {
            CreatedDreamMessage createdDream = objectMapper.readValue(message.getBody(), CreatedDreamMessage.class);
            log.info("방 생성 메시지 수신: {}", createdDream);

            CreatedRoomEvent event = new CreatedRoomEvent(createdDream.roomId(), createdDream.requestId(), createdDream.dreamServerId());

            applicationEventPublisher.publishEvent(event);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
