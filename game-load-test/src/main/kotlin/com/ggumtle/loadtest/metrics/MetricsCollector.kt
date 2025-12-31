package com.ggumtle.loadtest.metrics

import java.text.SimpleDateFormat
import java.util.*
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

    // 시간 추적
    private var startTime: Long = 0
    private var endTime: Long = 0
    private var playerCount: Int = 0

    companion object {
        // ANSI 색상 코드
        private const val RESET = "\u001B[0m"
        private const val RED = "\u001B[31m"
        private const val GREEN = "\u001B[32m"
        private const val YELLOW = "\u001B[33m"
        private const val BLUE = "\u001B[34m"
        private const val CYAN = "\u001B[36m"
        private const val BOLD = "\u001B[1m"

        // 한글 라벨 맵
        private val LABEL_KO = mapOf(
            // 응답 시간
            "auth" to "인증",
            "roomCreate" to "방 생성",
            "joinRoom" to "방 참가",
            // 액션 카운터
            "moves" to "이동",
            "digUps" to "꿈틀 캐기",
            "digUpsSuccess" to "꿈틀 캐기 성공",
            "digUpsFailed" to "꿈틀 캐기 실패",
            "feeds" to "먹이 주기",
            "boxOpens" to "상자 열기",
            "boxOpensSuccess" to "상자 열기 성공",
            "itemTakes" to "아이템 획득 시도",
            "itemTakesSuccess" to "아이템 획득 성공",
            "itemTakesFailed" to "아이템 획득 실패",
            "itemPutsSuccess" to "아이템 넣기 성공",
            "itemPutsFailed" to "아이템 넣기 실패",
            "hits" to "공격",
            "skills" to "스킬 사용",
            "escapes" to "탈출 시도"
        )

        // 성공/실패 매핑
        private val SUCCESS_PAIRS = mapOf(
            "boxOpens" to "boxOpensSuccess",
            "itemTakes" to "itemTakesSuccess",
            "digUps" to "digUpsSuccess"
        )

        fun translateKey(key: String): String = LABEL_KO[key] ?: key
    }

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
     * 테스트 시작 시점 기록
     */
    fun markStart(players: Int) {
        startTime = System.currentTimeMillis()
        playerCount = players
    }

    /**
     * 테스트 종료 시점 기록
     */
    fun markEnd() {
        endTime = System.currentTimeMillis()
    }

    /**
     * Get a summary string for progress reporting
     */
    fun getSummary(): String {
        val mainActions = listOf("moves", "hits", "skills", "digUps", "boxOpens", "feeds")
        val actionCounts = counters.entries
            .filter { it.key in mainActions }
            .joinToString(" │ ") { "${translateKey(it.key)}: ${it.value.get()}" }

        return "$CYAN📊$RESET $actionCounts │ ${if (errors.isEmpty()) "${GREEN}오류: 0$RESET" else "${RED}오류: ${errors.size}$RESET"}"
    }

    /**
     * 성공률 계산
     */
    fun calculateSuccessRates(): Map<String, Double> {
        return SUCCESS_PAIRS.mapNotNull { (attempt, success) ->
            val attempts = getCounter(attempt)
            if (attempts > 0) {
                val successes = getCounter(success)
                attempt to (successes.toDouble() / attempts * 100)
            } else null
        }.toMap()
    }

    /**
     * Generate a test report
     */
    fun generateReport(scenarioName: String): TestReport {
        if (endTime == 0L) markEnd()

        val latencyStats = latencies.keys.associateWith { getLatencyStats(it)!! }

        return TestReport(
            scenarioName = scenarioName,
            counters = getAllCounters(),
            latencyStats = latencyStats,
            errorCount = errors.size,
            errors = errors.take(100),
            startTime = startTime,
            endTime = endTime,
            playerCount = playerCount,
            successRates = calculateSuccessRates()
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
    val errors: List<MetricsCollector.ErrorRecord>,
    val startTime: Long,
    val endTime: Long,
    val playerCount: Int,
    val successRates: Map<String, Double>
) {
    companion object {
        // ANSI 색상 코드
        private const val RESET = "\u001B[0m"
        private const val RED = "\u001B[31m"
        private const val GREEN = "\u001B[32m"
        private const val YELLOW = "\u001B[33m"
        private const val BLUE = "\u001B[34m"
        private const val CYAN = "\u001B[36m"
        private const val BOLD = "\u001B[1m"
        private const val DIM = "\u001B[2m"

        private val dateFormat = SimpleDateFormat("yyyy-MM-dd HH:mm:ss")
    }

    fun print() {
        val duration = endTime - startTime
        val durationStr = formatDuration(duration)
        val startStr = if (startTime > 0) dateFormat.format(Date(startTime)) else "-"
        val endStr = if (endTime > 0) dateFormat.format(Date(endTime)) else "-"

        println()
        printHeader(scenarioName, startStr, endStr, durationStr, playerCount)
        println()
        printActionStats()
        println()
        printLatencyStats()
        println()
        printErrorSummary()
        println()
    }

    private fun printHeader(name: String, start: String, end: String, duration: String, players: Int) {
        val width = 68
        println("$CYAN$BOLD╔${"═".repeat(width)}╗$RESET")
        println("$CYAN$BOLD║${centerText("🎮 부하 테스트 보고서", width)}║$RESET")
        println("$CYAN$BOLD╠${"═".repeat(width)}╣$RESET")
        println("$CYAN$BOLD║$RESET ${padRight("시나리오: $BOLD$name$RESET", width - 1)}$CYAN$BOLD║$RESET")
        println("$CYAN$BOLD║$RESET ${padRight("실행 시간: $start ~ $end ($duration)", width - 1)}$CYAN$BOLD║$RESET")
        println("$CYAN$BOLD║$RESET ${padRight("플레이어: ${players}명", width - 1)}$CYAN$BOLD║$RESET")
        println("$CYAN$BOLD╚${"═".repeat(width)}╝$RESET")
    }

    private fun printActionStats() {
        val width = 68
        println("┌${"─".repeat(width)}┐")
        println("│${centerText("📊 액션 통계", width)}│")
        println("├${"─".repeat(18)}┬${"─".repeat(12)}┬${"─".repeat(12)}┬${"─".repeat(22)}┤")
        println("│${centerText("액션", 18)}│${centerText("시도", 12)}│${centerText("성공", 12)}│${centerText("성공률", 22)}│")
        println("├${"─".repeat(18)}┼${"─".repeat(12)}┼${"─".repeat(12)}┼${"─".repeat(22)}┤")

        // 성공/실패 추적이 있는 액션들
        val trackedActions = listOf("boxOpens", "itemTakes", "digUps")
        trackedActions.forEach { key ->
            val attempts = counters[key] ?: 0
            val successes = counters["${key}Success"] ?: 0
            val rate = successRates[key]
            val rateStr = rate?.let { formatSuccessRate(it) } ?: "-"
            val label = MetricsCollector.translateKey(key)

            println("│${padRight(" $label", 18)}│${padLeft(formatNumber(attempts), 11)} │${padLeft(formatNumber(successes), 11)} │${padLeft(rateStr, 21)} │")
        }

        println("├${"─".repeat(18)}┼${"─".repeat(12)}┼${"─".repeat(12)}┼${"─".repeat(22)}┤")

        // 추적 없는 단순 카운터
        val simpleCounters = listOf("moves", "hits", "skills", "feeds", "escapes")
        simpleCounters.forEach { key ->
            val count = counters[key] ?: return@forEach
            if (count == 0L) return@forEach
            val label = MetricsCollector.translateKey(key)

            println("│${padRight(" $label", 18)}│${padLeft(formatNumber(count), 11)} │${centerText("-", 12)}│${centerText("-", 22)}│")
        }

        println("└${"─".repeat(18)}┴${"─".repeat(12)}┴${"─".repeat(12)}┴${"─".repeat(22)}┘")
    }

    private fun printLatencyStats() {
        if (latencyStats.isEmpty()) return

        val width = 68
        println("┌${"─".repeat(width)}┐")
        println("│${centerText("⏱️  응답 시간 (ms)", width)}│")
        println("├${"─".repeat(18)}┬${"─".repeat(8)}┬${"─".repeat(8)}┬${"─".repeat(8)}┬${"─".repeat(8)}┬${"─".repeat(7)}┬${"─".repeat(7)}┤")
        println("│${centerText("작업", 18)}│${centerText("횟수", 8)}│${centerText("최소", 8)}│${centerText("최대", 8)}│${centerText("평균", 8)}│${centerText("p90", 7)}│${centerText("p99", 7)}│")
        println("├${"─".repeat(18)}┼${"─".repeat(8)}┼${"─".repeat(8)}┼${"─".repeat(8)}┼${"─".repeat(8)}┼${"─".repeat(7)}┼${"─".repeat(7)}┤")

        latencyStats.forEach { (name, stats) ->
            val label = MetricsCollector.translateKey(name)
            println("│${padRight(" $label", 18)}│${padLeft(stats.count.toString(), 7)} │${padLeft("%.1f".format(stats.min), 7)} │${padLeft("%.1f".format(stats.max), 7)} │${padLeft("%.1f".format(stats.avg), 7)} │${padLeft("%.1f".format(stats.p90), 6)} │${padLeft("%.1f".format(stats.p99), 6)} │")
        }

        println("└${"─".repeat(18)}┴${"─".repeat(8)}┴${"─".repeat(8)}┴${"─".repeat(8)}┴${"─".repeat(8)}┴${"─".repeat(7)}┴${"─".repeat(7)}┘")
    }

    private fun printErrorSummary() {
        val width = 68
        println("┌${"─".repeat(width)}┐")

        val errorHeader = if (errorCount == 0) {
            "${GREEN}✓ 오류 없음$RESET"
        } else {
            "${RED}❌ 오류 요약$RESET"
        }
        println("│${centerText(errorHeader, width + countAnsiChars(errorHeader))}│")

        if (errorCount > 0) {
            println("├${"─".repeat(width)}┤")
            println("│${padRight(" ${RED}총 오류 수: $errorCount$RESET", width + countAnsiChars(RED) + countAnsiChars(RESET))}│")
            println("├${"─".repeat(18)}┬${"─".repeat(width - 19)}┤")

            errors.take(10).forEach { error ->
                val playerCol = "[플레이어 ${error.playerId}]"
                val msgCol = "${error.phase}: ${error.message}"
                println("│${padRight(" $playerCol", 18)}│${padRight(" $msgCol", width - 19)}│")
            }

            if (errors.size > 10) {
                val remaining = errors.size - 10
                println("│${padRight(" $DIM... 외 ${remaining}건$RESET", 18 + countAnsiChars(DIM) + countAnsiChars(RESET))}│${" ".repeat(width - 19)}│")
            }
        }

        println("└${"─".repeat(if (errorCount > 0) 18 else width)}${if (errorCount > 0) "┴${"─".repeat(width - 19)}" else ""}┘")
    }

    // 유틸리티 함수들
    private fun formatDuration(millis: Long): String {
        val seconds = millis / 1000
        val minutes = seconds / 60
        val secs = seconds % 60
        return if (minutes > 0) "${minutes}분 ${secs}초" else "${secs}초"
    }

    private fun formatNumber(num: Long): String {
        return "%,d".format(num)
    }

    private fun formatSuccessRate(rate: Double): String {
        return when {
            rate >= 95 -> "$GREEN%.1f%%$RESET".format(rate)
            rate >= 80 -> "$YELLOW%.1f%%$RESET".format(rate)
            else -> "$RED%.1f%%$RESET".format(rate)
        }
    }

    private fun centerText(text: String, width: Int): String {
        val visibleLen = text.length - countAnsiChars(text)
        val padding = (width - visibleLen) / 2
        val extra = (width - visibleLen) % 2
        return " ".repeat(padding) + text + " ".repeat(padding + extra)
    }

    private fun padRight(text: String, width: Int): String {
        val visibleLen = text.length - countAnsiChars(text)
        return text + " ".repeat((width - visibleLen).coerceAtLeast(0))
    }

    private fun padLeft(text: String, width: Int): String {
        val visibleLen = text.length - countAnsiChars(text)
        return " ".repeat((width - visibleLen).coerceAtLeast(0)) + text
    }

    private fun countAnsiChars(text: String): Int {
        val ansiPattern = Regex("\u001B\\[[0-9;]*m")
        return ansiPattern.findAll(text).sumOf { it.value.length }
    }
}
