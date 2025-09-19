package com.ggumtle.ggumtle.auth.jwt;

import io.jsonwebtoken.Claims;
import io.jsonwebtoken.ExpiredJwtException;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.MalformedJwtException;
import io.jsonwebtoken.io.Decoders;
import io.jsonwebtoken.security.Keys;

import io.jsonwebtoken.security.SignatureException;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import javax.crypto.SecretKey;
import java.util.Date;

@Component
@Slf4j
public class JwtProvider {

    private final SecretKey key;
    private final long accessTokenMillis;

    public JwtProvider(
            @Value("${jwt.access.secret}") String base64Secret,
            @Value("${jwt.access.expire}") long expire
    ) {
        byte[] keyBytes = Decoders.BASE64.decode(base64Secret);
        this.key = Keys.hmacShaKeyFor(keyBytes);
        this.accessTokenMillis = expire;
    }
    public String issueAccessToken(Long memberId) {
        long now = System.currentTimeMillis();
        Date issuedAt = new Date(now);
        Date expiresAt = new Date(now + accessTokenMillis);

        return Jwts.builder()
                .subject(String.valueOf(memberId))
                .issuedAt(issuedAt)
                .expiration(expiresAt)
                .signWith(key)
                .compact();
    }

    public Long parseMemberId(String token) {
        try {
            String sub = Jwts.parser()
                    .verifyWith(key)
                    .build()
                    .parseSignedClaims(token)
                    .getPayload()
                    .getSubject();
            return Long.parseLong(sub);
        } catch (ExpiredJwtException e) {
            log.error("JWT Token이 만료되었습니다: {}", token, e);
        } catch (MalformedJwtException e) {
            log.error("JWT Token 구조가 잘못되었습니다: {}", token, e);
        } catch (SignatureException e) {
            log.error("JWT Token 서명이 유효하지 않습니다: {}", token, e);
        } catch (JwtException e) {
            log.error("JWT Token 파싱 실패: {}", token, e);
        } catch (NumberFormatException e) {
            log.error("JWT Subject를 Long으로 변환할 수 없습니다: {}", token, e);
        }
        return null;
    }

    public boolean validateToken(String token) {
        try{
            Jwts.parser().verifyWith(key).build().parseSignedClaims(token);
            return true;
        }catch (Exception e){
            return false;
        }
    }

    public Long getRemainingMillis(String token) {
        try {
            Claims claims = Jwts.parser()
                    .verifyWith(key)
                    .build()
                    .parseClaimsJws(token)
                    .getPayload();

            Date expiration = claims.getExpiration();
            long now = System.currentTimeMillis();
            return expiration.getTime() - now;
        } catch (Exception e){
            return 0L;
        }
    }
}
