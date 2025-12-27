package com.ggumtle.loadtest.config

import com.charleskorn.kaml.Yaml
import mu.KotlinLogging
import java.io.File

private val logger = KotlinLogging.logger {}

/**
 * Loads configuration from YAML files
 */
object ConfigLoader {

    /**
     * Load configuration from a file
     */
    fun load(path: String): FullConfig {
        val file = File(path)
        return if (file.exists()) {
            logger.info { "Loading config from $path" }
            val content = file.readText()
            Yaml.default.decodeFromString(FullConfig.serializer(), content)
        } else {
            logger.warn { "Config file not found: $path, using defaults" }
            FullConfig()
        }
    }

    /**
     * Load configuration from resources
     */
    fun loadFromResources(resourcePath: String = "reference.yaml"): FullConfig {
        val content = ConfigLoader::class.java.classLoader
            .getResourceAsStream(resourcePath)
            ?.bufferedReader()
            ?.readText()

        return if (content != null) {
            Yaml.default.decodeFromString(FullConfig.serializer(), content)
        } else {
            logger.warn { "Resource not found: $resourcePath, using defaults" }
            FullConfig()
        }
    }
}
