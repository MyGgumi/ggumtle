plugins {
    alias(libs.plugins.ggumtle.jvm.library)
    alias(libs.plugins.ggumtle.hilt)
}

dependencies {
    implementation(libs.kotlinx.coroutines.core)
}