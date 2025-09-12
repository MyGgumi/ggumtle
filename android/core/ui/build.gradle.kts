plugins {
    alias(libs.plugins.ggumtle.android.library)
    alias(libs.plugins.ggumtle.android.library.compose)
}

android {
    namespace = "com.ggumtle.core.ui"
}

dependencies {
    api(projects.core.designsystem)
    api(projects.core.model)

    implementation(libs.androidx.browser)
    implementation(libs.coil.kt)
    implementation(libs.coil.kt.compose)
}