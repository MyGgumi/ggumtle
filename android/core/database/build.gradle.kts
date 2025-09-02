plugins {
    alias(libs.plugins.multimodulebase.android.library)
    alias(libs.plugins.multimodulebase.android.room)
    alias(libs.plugins.multimodulebase.hilt)
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