package com.ggumtle.loadtest.config

import io.github.cdimascio.dotenv.dotenv
import mu.KotlinLogging
import kotlin.math.round

private val logger = KotlinLogging.logger {}

/**
 * .env 파일에서 환경변수를 로드하는 유틸리티.
 *
 * 우선순위: System.getenv() > .env 파일 > 기본값
 */
object EnvLoader {

    private val dotenv by lazy {
        dotenv {
            ignoreIfMissing = true
            ignoreIfMalformed = true
        }.also {
            logger.debug { ".env 파일 로더 초기화 완료" }
        }
    }

    /**
     * 환경변수 값을 가져옵니다.
     * @return 값이 없으면 null
     */
    fun get(key: String): String? {
        return System.getenv(key) ?: dotenv[key]
    }

    /**
     * 환경변수 값을 가져오거나 기본값을 반환합니다.
     */
    fun get(key: String, default: String): String {
        return get(key) ?: default
    }

    /**
     * 필수 환경변수를 가져옵니다. 없으면 예외 발생.
     */
    fun getRequired(key: String): String {
        return get(key) ?: throw IllegalStateException(
            "$key 환경변수가 설정되지 않았습니다. " +
                ".env 파일을 확인하거나 환경변수를 설정해주세요."
        )
    }

    // 자주 사용하는 값들
    val accessSecret: String by lazy { getRequired("ACCESS_SECRET") }
    val gameServerHost: String by lazy { get("GAME_SERVER_HOST", "localhost") }
    val gameServerPort: Int by lazy { get("GAME_SERVER_PORT", "9000").toInt() }

    /**
     * Unity FixedUpdate frequency (Hz). Default: 60
     */
    val fixedUpdateHz: Int by lazy {
        val value = get("FIXED_UPDATE_HZ")?.toIntOrNull()
        when {
            value == null -> {
                logger.info { "FIXED_UPDATE_HZ not set, using default 60Hz" }
                60
            }
            value <= 0 -> {
                logger.warn { "Invalid FIXED_UPDATE_HZ=$value, using default 60Hz" }
                60
            }
            else -> {
                logger.info { "FIXED_UPDATE_HZ=$value (interval=${round(1000.0 / value).toLong()}ms)" }
                value
            }
        }
    }

    /**
     * Movement packet interval in milliseconds.
     * Derived from fixedUpdateHz: 1000 / Hz, rounded.
     */
    val moveIntervalMs: Long by lazy {
        round(1000.0 / fixedUpdateHz).toLong().coerceAtLeast(1L)
    }
}
