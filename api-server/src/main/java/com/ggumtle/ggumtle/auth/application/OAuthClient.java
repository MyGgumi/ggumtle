package com.ggumtle.ggumtle.auth.application;

import com.google.api.client.googleapis.auth.oauth2.GoogleIdToken;
import com.google.api.client.googleapis.auth.oauth2.GoogleIdToken.Payload;
import com.google.api.client.googleapis.auth.oauth2.GoogleIdTokenVerifier;
import com.google.api.client.http.javanet.NetHttpTransport;
import com.google.api.client.json.gson.GsonFactory;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.util.Collections;
import java.util.Optional;

import static java.rmi.server.LogStream.log;

@Component
@Slf4j
public class OAuthClient  {
    private static final String ISSUER_1 = "https://accounts.google.com";
    private static final String ISSUER_2 = "accounts.google.com";
    private final GoogleIdTokenVerifier googleIdTokenVerifier;

    public OAuthClient (@Value("${spring.security.oauth2.client.registration.google.client-id}") String clientId) {
        this.googleIdTokenVerifier = new GoogleIdTokenVerifier.Builder(
                new NetHttpTransport(),
                GsonFactory.getDefaultInstance()
        )
                .setAudience(Collections.singletonList(clientId))
                .build();
    }

    public Optional<Payload> verify(String idToken) {
        try {
            GoogleIdToken token = googleIdTokenVerifier.verify(idToken);
            if (token == null) {
                return Optional.empty();
            }

            Payload payload = token.getPayload();

            String issuer = payload.getIssuer();
            if (!ISSUER_1.equals(issuer) && !ISSUER_2.equals(issuer)) {
                return Optional.empty();
            }
                return Optional.of(payload);


        } catch (Exception e) {
            return Optional.empty();
        }
    }
}
