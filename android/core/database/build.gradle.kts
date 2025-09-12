plugins {
    alias(libs.plugins.ggumtle.android.library)
    alias(libs.plugins.ggumtle.android.room)
    alias(libs.plugins.ggumtle.hilt)
}
android {
    namespace = "com.ggumtle.core.database"
}

dependencies {
    api(projects.core.model)

    implementation(libs.room.runtime)
    implementation(libs.room.ktx)

    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)
}