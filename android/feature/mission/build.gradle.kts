plugins {
    alias(libs.plugins.ggumtle.android.feature)
    alias(libs.plugins.ggumtle.android.library.compose)
}

android {
    namespace = "com.ggumtle.mission"

    packagingOptions {
        // Filament 네이티브 라이브러리 포함
        jniLibs {
            pickFirsts += "**/libfilament-jni.so"
            pickFirsts += "**/libgltfio-jni.so"
        }
    }
}

dependencies {
    implementation(libs.material)

    // AR dependencies matching app module
    implementation(libs.core) // AR Core
    implementation(libs.filament.android)
    implementation(libs.filament.utils.android)
    implementation(libs.gltfio.android)
    implementation(libs.slf4j.simple)
    implementation(libs.arrow.fx)
    implementation(libs.coil.kt)
    implementation(libs.coil.kt.compose)
}