plugins {
    alias(libs.plugins.ggumtle.android.feature)
    alias(libs.plugins.ggumtle.android.library.compose)
}

android {
    namespace = "com.ggumtle.home"
}

dependencies {
    implementation(libs.material)
}