package com.ggumtle.loadtest.metrics

import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicLong
import java.util.concurrent.CopyOnWriteArrayList

/**
 * Collects metrics during load test execution
 */
class MetricsCollector {
    private val counters = ConcurrentHashMap<String, AtomicLong>()
    private val latencies = ConcurrentHashMap<String, MutableList<Long>>()
    private val errors = CopyOnWriteArrayList<ErrorRecord>()

    data class ErrorRecord(
        val playerId: Int,
        val phase: String,
        val message: String,
        val timestamp: Long = System.currentTimeMillis()
    )

    /**
     * Increment a counter
     */
    fun incrementCounter(name: String, amount: Long = 1) {
        counters.computeIfAbsent(name) { AtomicLong(0) }.addAndGet(amount)
    }

    /**
     * Record a latency measurement (in nanoseconds)
     */
    fun recordLatency(name: String, nanos: Long) {
        latencies.computeIfAbsent(name) { CopyOnWriteArrayList() }.add(nanos)
    }

    /**
     * Record an error
     */
    fun recordError(playerId: Int, phase: String, error: Throwable) {
        errors.add(ErrorRecord(playerId, phase, error.message ?: "Unknown error"))
    }

    /**
     * Get counter value
     */
    fun getCounter(name: String): Long = counters[name]?.get() ?: 0

    /**
     * Get all counters
     */
    fun getAllCounters(): Map<String, Long> = counters.mapValues { it.value.get() }

    /**
     * Get latency statistics for a metric
     */
    fun getLatencyStats(name: String): LatencyStats? {
        val samples = latencies[name] ?: return null
        if (samples.isEmpty()) return null

        val sorted = samples.sorted()
        val count = sorted.size

        return LatencyStats(
            count = count,
            min = sorted.first() / 1_000_000.0,
            max = sorted.last() / 1_000_000.0,
            avg = sorted.average() / 1_000_000.0,
            p50 = sorted[count / 2] / 1_000_000.0,
            p90 = sorted[(count * 0.9).toInt().coerceAtMost(count - 1)] / 1_000_000.0,
            p99 = sorted[(count * 0.99).toInt().coerceAtMost(count - 1)] / 1_000_000.0
        )
    }

    /**
     * Get error count
     */
    fun getErrorCount(): Int = errors.size

    /**
     * Get all errors
     */
    fun getErrors(): List<ErrorRecord> = errors.toList()

    /**
     * Get a summary string for progress reporting
     */
    fun getSummary(): String {
        val countersStr = counters.entries.joinToString(", ") { "${it.key}=${it.value.get()}" }
        return "Counters: [$countersStr], Errors: ${errors.size}"
    }

    /**
     * Generate a test report
     */
    fun generateReport(scenarioName: String): TestReport {
        val latencyStats = latencies.keys.associateWith { getLatencyStats(it)!! }

        return TestReport(
            scenarioName = scenarioName,
            counters = getAllCounters(),
            latencyStats = latencyStats,
            errorCount = errors.size,
            errors = errors.take(100) // Limit errors in report
        )
    }

    data class LatencyStats(
        val count: Int,
        val min: Double,
        val max: Double,
        val avg: Double,
        val p50: Double,
        val p90: Double,
        val p99: Double
    ) {
        override fun toString(): String =
            "count=$count, min=%.2fms, max=%.2fms, avg=%.2fms, p50=%.2fms, p90=%.2fms, p99=%.2fms"
                .format(min, max, avg, p50, p90, p99)
    }
}

/**
 * Test report containing all metrics
 */
data class TestReport(
    val scenarioName: String,
    val counters: Map<String, Long>,
    val latencyStats: Map<String, MetricsCollector.LatencyStats>,
    val errorCount: Int,
    val errors: List<MetricsCollector.ErrorRecord>
) {
    fun print() {
        println("\n===== Load Test Report: $scenarioName =====")
        println()

        println("--- Counters ---")
        counters.forEach { (name, value) ->
            println("  $name: $value")
        }
        println()

        println("--- Latencies ---")
        latencyStats.forEach { (name, stats) ->
            println("  $name: $stats")
        }
        println()

        println("--- Errors ---")
        println("  Total errors: $errorCount")
        if (errors.isNotEmpty()) {
            errors.take(10).forEach { error ->
                println("    [Player ${error.playerId}] ${error.phase}: ${error.message}")
            }
            if (errors.size > 10) {
                println("    ... and ${errors.size - 10} more")
            }
        }
        println()
        println("=".repeat(50))
    }
}
