plugins {
    alias(libs.plugins.multimodulebase.android.application)
    alias(libs.plugins.multimodulebase.android.application.compose)
    alias(libs.plugins.multimodulebase.hilt)
    id("com.google.android.gms.oss-licenses-plugin")
    alias(libs.plugins.baselineprofile)
    alias(libs.plugins.kotlin.serialization)
}

android {
    namespace = "com.ggumtle.ggumtle"

    defaultConfig {
        applicationId = "com.ggumtle.ggumtle"
        versionCode = 1
        versionName = "1.0"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }
}

dependencies {

    implementation(projects.core.designsystem)
    implementation(projects.core.common)
    implementation(projects.core.data)
    implementation(projects.core.domain)
    implementation(projects.core.datastore)
    implementation(projects.feature.auth)
    implementation(projects.feature.startup)
    implementation(projects.feature.social)
    implementation(projects.feature.home)

    implementation(project(":unityLibrary"))
    implementation("androidx.games:games-activity:3.0.5")
    implementation("androidx.appcompat:appcompat:1.6.1")
    implementation(files("C:\\Users\\SSAFY\\Documents\\unity\\mainpagetest\\unityLibrary\\libs\\unity-classes.jar"))


    // ───── Android 기본 ─────
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.activity.compose)

    // ───── Compose UI ─────
    implementation(libs.androidx.ui)
    implementation(libs.androidx.ui.graphics)
    implementation(libs.androidx.material3)

    // ───── Navigation ─────
    implementation(libs.androidx.navigation.compose)
    implementation(libs.androidx.hilt.navigation.compose)

    // ───── System UI Controller ─────
    implementation(libs.accompanist.systemuicontroller)

    // ───── 테스트 의존성 ─────
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.ui.test.junit4)
    androidTestImplementation(libs.androidx.navigation.testing)
}