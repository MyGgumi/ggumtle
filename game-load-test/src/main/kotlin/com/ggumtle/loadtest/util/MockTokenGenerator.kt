package com.ggumtle.loadtest.util

import io.jsonwebtoken.Jwts
import io.jsonwebtoken.security.Keys
import java.util.Base64
import java.util.Date
import javax.crypto.SecretKey

/**
 * Generates JWT tokens for testing that are compatible with game-server's JwtService.
 *
 * Requires ACCESS_SECRET environment variable (BASE64 encoded) to match game-server config.
 */
object MockTokenGenerator {

    private val secret: SecretKey by lazy {
        val secretString = System.getenv("ACCESS_SECRET")
            ?: throw IllegalStateException(
                "ACCESS_SECRET 환경변수가 설정되지 않았습니다. " +
                "game-server와 동일한 값을 설정해주세요."
            )
        val keyBytes = Base64.getDecoder().decode(secretString)
        Keys.hmacShaKeyFor(keyBytes)
    }

    /**
     * Generate a valid JWT token for a player.
     *
     * @param playerId The player ID to use as the JWT subject (memberId)
     * @return JWT token string compatible with game-server
     */
    fun generate(playerId: Int): String {
        return Jwts.builder()
            .subject(playerId.toString())  // memberId - parsed by JwtService.parseId()
            .issuedAt(Date())
            .expiration(Date(System.currentTimeMillis() + 3600000)) // 1 hour
            .signWith(secret)
            .compact()
    }

    /**
     * Generate a JWT token with custom expiration.
     *
     * @param playerId The player ID
     * @param expirationMs Token expiration time in milliseconds from now
     * @return JWT token string
     */
    fun generate(playerId: Int, expirationMs: Long): String {
        return Jwts.builder()
            .subject(playerId.toString())
            .issuedAt(Date())
            .expiration(Date(System.currentTimeMillis() + expirationMs))
            .signWith(secret)
            .compact()
    }
}
