plugins {
    alias(libs.plugins.multimodulebase.android.feature)
    alias(libs.plugins.multimodulebase.android.library.compose)
}

android {
    namespace = "com.ggumtle.social"
}

dependencies {
    implementation(libs.material)
}