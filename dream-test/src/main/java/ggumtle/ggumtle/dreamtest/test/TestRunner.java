package ggumtle.ggumtle.dreamtest.test;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;


@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class TestRunner {
    private final MoveTester moveTester;

    @Value("${TEST_ID}")
    private int testId;

    @EventListener(ApplicationReadyEvent.class)
    public void runTest() {
        switch (testId) {
            case 1: moveTester.run(); break;

            default: log.error("Test ID {}에 해당하는 테스트를 찾을 수 없습니다", testId);
        }
    }
}
