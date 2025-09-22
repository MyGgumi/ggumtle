package ggumtle.ggumtle.dreamtest.util;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Component;

import java.util.Base64;

@Slf4j
@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class TokenParser {
    private final ObjectMapper objectMapper;

    public long parseId(String token) {
        String payload = token.split("\\.")[1];
        String payloadJson = new String(Base64.getDecoder().decode(payload));

        try {
            JsonNode root = objectMapper.readTree(payloadJson);
            return root.get("sub").asLong();
        } catch (Exception e) {
            throw new RuntimeException("JWT 페이로드 파싱 실패", e);
        }
    }
}
