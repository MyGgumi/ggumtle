package com.ggumtle.ggumtle.dream.application;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.ggumtle.ggumtle.dream.domain.DreamServer;
import com.ggumtle.ggumtle.dream.domain.OptimalServer;
import com.ggumtle.ggumtle.dream.persistence.DreamServerRepository;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.util.UriComponentsBuilder;

import java.util.HashMap;
import java.util.List;
import java.util.Map;

@Service
@Slf4j
public class DreamServerSelector {
    private final String prometheusUrl;
    private final RestTemplate restTemplate = new RestTemplate();
    private final ObjectMapper objectMapper;
    private final DreamServerRepository dreamServerRepository;
    private final RedisTemplate<String, OptimalServer> optimalServerRedisTemplate;

    @Autowired
    protected DreamServerSelector(
            @Value("${PROMETHEUS}") String url,
            ObjectMapper objectMapper,
            DreamServerRepository dreamServerRepository,
            RedisTemplate<String, OptimalServer> optimalServerRedisTemplate) {
        this.prometheusUrl = url;
        this.objectMapper = objectMapper;
        this.dreamServerRepository = dreamServerRepository;
        this.optimalServerRedisTemplate = optimalServerRedisTemplate;
    }

    @Scheduled(fixedDelay = 60000)
    public void findLeastBusyServer() {
        List<DreamServer> servers = dreamServerRepository.findAll();

        DreamServer bestServer = null;
        double bestScore = Double.MAX_VALUE;

        Map<String, Double> memoryUsages = queryDoubleMetric("jvm_memory_used_bytes");
        Map<String, Long> pendingWriteChannelCount = queryLongMetric("game_channel_pending_writes");
        log.debug("서버 조회 결과: 메모리={}, 쓰기 대기 채널={}", memoryUsages, pendingWriteChannelCount);

        double memoryWeight = 0.3;
        double pendingWriteWeight = 0.7;
        for (DreamServer server : servers) {
            String serverInstance = String.format("%s:%d", server.getHost(), server.getPort());
            double memory = memoryUsages.getOrDefault(serverInstance, Double.MAX_VALUE);
            long pendingWrites = pendingWriteChannelCount.getOrDefault(serverInstance, Long.MAX_VALUE);

            double score = memory * memoryWeight + pendingWrites * pendingWriteWeight;

            if (score < bestScore) {
                bestScore = score;
                bestServer = server;
            }
        }

        if (bestServer == null) {
            log.warn("서버 선정에 실패해 서버 업데이트 실패");
            return;
        }

        OptimalServer optimalServer = new OptimalServer(bestServer.getId(), bestServer.getHost(), bestServer.getPort());
        optimalServerRedisTemplate.opsForValue().set(OptimalServer.KEY, optimalServer);
        log.info("서버 선정 완료: {}", optimalServer);
    }

    private Map<String, Long> queryLongMetric(String metricName) {
        JsonNode root = query(metricName);
        if (root == null) {
            return Map.of();
        }

        Map<String, Long> metric = new HashMap<>();
        JsonNode results = root.path("data").path("result");
        for (JsonNode result : results) {
            metric.put(
                    result.path("metric").path("instance").asText(),
                    result.path("value").get(1).asLong()
            );
        }

        return metric;
    }

    private Map<String, Double> queryDoubleMetric(String metricName) {
        JsonNode root = query(metricName);
        if (root == null) {
            return Map.of();
        }

        Map<String, Double> metric = new HashMap<>();
        JsonNode results = root.path("data").path("result");
        for (JsonNode result : results) {
            metric.put(
                    result.path("metric").path("instance").asText(),
                    result.path("value").get(1).asDouble()
            );
        }

        return metric;
    }

    private JsonNode query(String metric) {
        String url = UriComponentsBuilder
                .fromUriString(prometheusUrl)
                .queryParam("query", metric)
                .toUriString();

        String queryResult = restTemplate.getForObject(url, String.class);

        JsonNode root;
        try {
            root = objectMapper.readTree(queryResult);
        } catch (JsonProcessingException e) {
            log.error("memory 지표 조회 결과를 JSON으로 변환하는 데 실패했습니다");
            return null;
        }

        if (!root.path("status").asText().equals("success")) {
            log.error("memory 지표 조회에 실패했습니다");
            return null;
        }

        return root;
    }
}
