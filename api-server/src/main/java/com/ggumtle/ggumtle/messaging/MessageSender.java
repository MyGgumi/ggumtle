package com.ggumtle.ggumtle.messaging;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
@Slf4j
public class MessageSender {
    private final RedisTemplate<String, String> redisTemplate;
    private final ObjectMapper objectMapper;

    public boolean sendMessage(String channel, Object message) {
        String data;
        try {
            data = objectMapper.writeValueAsString(message);
        } catch (JsonProcessingException e) {
            log.error("JSON 프로세싱 중 오류 발생: {}", e);
            return false;
        }

        redisTemplate.convertAndSend(channel, data);
        return true;
    }
}
