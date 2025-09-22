package com.ggumtle.ggumtle.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.messaging.event.EndDreamEvent;
import com.ggumtle.ggumtle.messaging.message.EndDreamMessage;
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
public class DreamMessageListener implements MessageListener {
    private final ApplicationEventPublisher applicationEventPublisher;
    private final ObjectMapper objectMapper;

    @Override
    public void onMessage(Message message, byte[] pattern) {
        try {
            EndDreamMessage endDreamMessage = objectMapper.readValue(message.getBody(), EndDreamMessage.class);
            log.info("게임 종료: {}", endDreamMessage);

            EndDreamEvent event = EndDreamEvent.from(endDreamMessage);

            applicationEventPublisher.publishEvent(event);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
