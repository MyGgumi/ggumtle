package com.ggumtle.ggumtle.auth;

import com.ggumtle.ggumtle.common.property.JwtProperty;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.io.Decoders;
import io.jsonwebtoken.security.Keys;
import org.springframework.stereotype.Component;

import javax.crypto.SecretKey;

@Component
public class JwtService {
    private final SecretKey secret;

    public JwtService(JwtProperty jwtProperty) {
//        this.secret = new SecretKeySpec(
//                secret.getBytes(StandardCharsets.UTF_8),
//                Jwts.SIG.HS256.key().build().getAlgorithm()
//        );
        byte[] keyBytes = Decoders.BASE64.decode(jwtProperty.getSecret());
        this.secret = Keys.hmacShaKeyFor(keyBytes);
    }

    public boolean verifyToken(String token) {
        try {
            Jwts.parser()
                    .verifyWith(secret)
                    .build()
                    .parseSignedClaims(token)
                    .getPayload()
                    .getExpiration();
        } catch (JwtException | IllegalArgumentException e) {
            return false;
        }

        return true;
    }

    public long parseId(String token) {
        String sub = Jwts.parser()
                .verifyWith(secret)
                .build()
                .parseSignedClaims(token)
                .getPayload()
                .getSubject();

        return Long.parseLong(sub);
    }
}
