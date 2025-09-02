plugins {
    alias(libs.plugins.multimodulebase.android.library)
    alias(libs.plugins.multimodulebase.hilt)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.ggumtle.core.data"
    testOptions {
        unitTests {
            isIncludeAndroidResources = true
        }
    }
}

dependencies {
    api(projects.core.common)
    api(projects.core.database)
    api(projects.core.datastore)
    implementation(projects.core.network)
    implementation(projects.core.domain)

    implementation(libs.kotlinx.serialization.json)
    implementation(libs.retrofit.core)
    implementation(libs.retrofit.kotlin.serialization)
    implementation(libs.okhttp.logging)
}
