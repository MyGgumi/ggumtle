import java.util.Properties

plugins {
    alias(libs.plugins.ggumtle.android.library)
    alias(libs.plugins.ggumtle.hilt)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.ggumtle.core.network"
    buildFeatures {
        buildConfig = true
    }
    testOptions {
        unitTests {
            isIncludeAndroidResources = true
        }
    }
    defaultConfig {
        buildConfigField("String", "REST_BASE_URL", "\"${getLocalProperty("REST_BASE_URL")}\"")
        buildConfigField("String", "WS_BASE_URL", "\"${getLocalProperty("WS_BASE_URL")}\"")
    }
}

dependencies {
    api(libs.kotlinx.datetime)
    api(projects.core.common)
    api(projects.core.model)

    implementation(projects.core.datastore)

    implementation(libs.kotlinx.serialization.json)
    implementation(libs.okhttp.logging)
    implementation(libs.retrofit.core)
    implementation(libs.retrofit.kotlin.serialization)
}

fun getLocalProperty(propertyKey: String): String {
    val properties = Properties()
    properties.load(rootProject.file("local.properties").inputStream())
    return properties.getProperty(propertyKey) ?: throw GradleException("Property $propertyKey not found in local.properties")
}