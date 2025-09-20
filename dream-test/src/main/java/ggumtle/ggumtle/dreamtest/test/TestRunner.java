package ggumtle.ggumtle.dreamtest.test;

import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;


@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class TestRunner {
    private final MoveTester moveTester;

    @EventListener(ApplicationReadyEvent.class)
    public void runTest() {
//         moveTester.run();
    }
}
