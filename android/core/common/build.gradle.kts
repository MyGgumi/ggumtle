plugins {
    alias(libs.plugins.multimodulebase.jvm.library)
    alias(libs.plugins.multimodulebase.hilt)
}

dependencies {
    implementation(libs.kotlinx.coroutines.core)
}