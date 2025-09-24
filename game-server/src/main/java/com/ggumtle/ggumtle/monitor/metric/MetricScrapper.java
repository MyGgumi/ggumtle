package com.ggumtle.ggumtle.monitor.metric;

import com.ggumtle.ggumtle.server.applicatoin.ChannelManager;
import com.sun.management.OperatingSystemMXBean;
import com.sun.management.UnixOperatingSystemMXBean;
import io.micrometer.core.instrument.Gauge;
import lombok.AccessLevel;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;

import java.lang.management.GarbageCollectorMXBean;
import java.lang.management.ManagementFactory;
import java.lang.management.MemoryMXBean;
import java.lang.management.ThreadMXBean;
import java.util.Arrays;
import java.util.List;

@Component
@RequiredArgsConstructor(access = AccessLevel.PROTECTED)
@Slf4j
public class MetricScrapper {
    private final ChannelManager channelManager;

    @EventListener(ApplicationReadyEvent.class)
    public void registerMetric() {
        OperatingSystemMXBean osBean = ManagementFactory.getPlatformMXBean(OperatingSystemMXBean.class);

        cpu(osBean);
        fileDescriptor(osBean);

        memory();
        thread();
        garbageCollection();

        netty();

        log.info("매트릭 수집 등록 완료");
    }

    private void cpu(OperatingSystemMXBean osBean) {
        Gauge.builder("jvm_process_cpu_load", osBean, OperatingSystemMXBean::getProcessCpuLoad)
                .description("JVM 프로세스 CPU 사용률")
                .register(MetricRegistry.registry);

        Gauge.builder("system_cpu_load", osBean, OperatingSystemMXBean::getCpuLoad)
                .description("시스템 전체 CPU 사용률")
                .register(MetricRegistry.registry);
    }

    private void fileDescriptor(OperatingSystemMXBean osBean) {
        Gauge.builder("open_file_descriptors", () -> {
            try {
                UnixOperatingSystemMXBean unixBean = (UnixOperatingSystemMXBean) osBean;
                return unixBean.getOpenFileDescriptorCount();
            } catch (ClassCastException e) {
                return -1L;
            }
        }).description("현재 열린 파일 디스크립터 수").register(MetricRegistry.registry);

        Gauge.builder("max_file_descriptors", () -> {
            try {
                UnixOperatingSystemMXBean unixBean = (UnixOperatingSystemMXBean) osBean;
                return unixBean.getMaxFileDescriptorCount();
            } catch (ClassCastException e) {
                return -1L;
            }
        }).description("최대 파일 디스크립터 수").register(MetricRegistry.registry);
    }

    private void memory() {
        MemoryMXBean memoryMXBean = ManagementFactory.getMemoryMXBean();
        Gauge.builder("jvm_memory_used_bytes", memoryMXBean,
                        m -> m.getHeapMemoryUsage().getUsed())
                .description("JVM Heap 사용량")
                .register(MetricRegistry.registry);

        Gauge.builder("jvm_memory_committed_bytes", memoryMXBean,
                        m -> m.getHeapMemoryUsage().getCommitted())
                .description("JVM Heap 예약량")
                .register(MetricRegistry.registry);
    }

    private void thread() {
        Gauge.builder("jvm_threads_count", () -> ManagementFactory.getThreadMXBean().getThreadCount())
                .description("JVM 스레드 수")
                .register(MetricRegistry.registry);

        Gauge.builder("jvm_daemon_threads_count", () -> ManagementFactory.getThreadMXBean().getDaemonThreadCount())
                .description("Daemon 스레드 수")
                .register(MetricRegistry.registry);

        ThreadMXBean threadMXBean = ManagementFactory.getThreadMXBean();
        Gauge.builder("jvm_threads_runnable_count", () -> {
            long runnable = Arrays.stream(threadMXBean.dumpAllThreads(false, false))
                    .filter(t -> t.getThreadState() == Thread.State.RUNNABLE)
                    .count();
            return (double) runnable;
        }).description("RUNNABLE 상태 스레드 수").register(MetricRegistry.registry);

        Gauge.builder("jvm_threads_blocked_count", () -> {
            long blocked = Arrays.stream(threadMXBean.dumpAllThreads(false, false))
                    .filter(t -> t.getThreadState() == Thread.State.BLOCKED)
                    .count();
            return (double) blocked;
        }).description("BLOCKED 상태 스레드 수").register(MetricRegistry.registry);

        Gauge.builder("jvm_threads_waiting_count", () -> {
            long waiting = Arrays.stream(threadMXBean.dumpAllThreads(false, false))
                    .filter(t -> t.getThreadState() == Thread.State.WAITING ||
                            t.getThreadState() == Thread.State.TIMED_WAITING)
                    .count();
            return (double) waiting;
        }).description("WAITING/TIMED_WAITING 상태 스레드 수").register(MetricRegistry.registry);
    }

    private void garbageCollection() {
        List<GarbageCollectorMXBean> gcBeans = ManagementFactory.getGarbageCollectorMXBeans();
        for (GarbageCollectorMXBean gcBean : gcBeans) {
            Gauge.builder("jvm_gc_collection_count", gcBean, GarbageCollectorMXBean::getCollectionCount)
                    .description("GC 횟수")
                    .tag("gc", gcBean.getName())
                    .register(MetricRegistry.registry);

            Gauge.builder("jvm_gc_collection_time_ms", gcBean, GarbageCollectorMXBean::getCollectionTime)
                    .description("GC 소요 시간(ms)")
                    .tag("gc", gcBean.getName())
                    .register(MetricRegistry.registry);
        }
    }

    private void netty() {
        Gauge.builder("game_active_channels", channelManager, ChannelManager::getChannelCount)
                .description("현재 활성화된 채널 수")
                .register(MetricRegistry.registry);

        Gauge.builder("game_channel_pending_writes", channelManager, ChannelManager::getPendingWriteCount)
                .description("채널 쓰기 대기 큐 길이")
                .register(MetricRegistry.registry);
    }
}
