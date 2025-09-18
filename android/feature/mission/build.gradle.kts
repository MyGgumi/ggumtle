plugins {
    alias(libs.plugins.ggumtle.android.feature)
    alias(libs.plugins.ggumtle.android.library.compose)
}

android {
    namespace = "com.ggumtle.mission"
}

dependencies {
    implementation(libs.material)
    implementation(libs.core)
    implementation(libs.sceneform.core)
    implementation("androidx.camera:camera-camera2:1.3.4")
    implementation("androidx.camera:camera-lifecycle:1.3.4")
    implementation("androidx.camera:camera-view:1.3.4")
}