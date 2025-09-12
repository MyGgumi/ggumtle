plugins {
    alias(libs.plugins.ggumtle.android.library)
    alias(libs.plugins.ksp)
}
android {
    namespace = "com.ggumtle.core.domain"
}

dependencies {
    api(projects.core.model)
    api(projects.core.common)
    api(projects.core.datastore)

    implementation(libs.javax.inject)
    implementation(libs.kotlinx.coroutines.core)
}