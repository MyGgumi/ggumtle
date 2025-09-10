plugins {
    alias(libs.plugins.multimodulebase.android.feature)
    alias(libs.plugins.multimodulebase.android.library.compose)
}

android {
    namespace = "com.ggumtle.home"
}

dependencies {
    implementation(libs.material)
}