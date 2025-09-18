package com.ggumtle.ggumtle.auth.jwt;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import org.springframework.data.redis.core.StringRedisTemplate;
import org.springframework.stereotype.Repository;

import java.util.concurrent.TimeUnit;

@Repository
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
public class TokenBlacklistRepository {
    private final StringRedisTemplate redisTemplate;

    public void save(String accessToken, long ttlMillis){
        redisTemplate.opsForValue().set(
                "blacklist:" + accessToken,
                "logout",
                ttlMillis,
                TimeUnit.MILLISECONDS
        );
    }

    public boolean isBlacklisted(String accessToken){
        return redisTemplate.hasKey("blacklist:" + accessToken);
    }
}
