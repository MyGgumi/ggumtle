plugins {
    alias(libs.plugins.ggumtle.android.library)
    alias(libs.plugins.ggumtle.hilt)
}

android {
    namespace = "com.ggumtle.core.datastore"

    buildFeatures {
        buildConfig = true
    }

    defaultConfig {
        buildConfigField("String", "GOOGLE_CLIENT_ID", "\"${getProperty("GOOGLE_CLIENT_ID")}\"")
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
    api(libs.androidx.dataStore)
    api(libs.androidx.datastore.preferences.v117)
    api(projects.core.model)
    
    implementation(projects.core.common)
    implementation(libs.kotlinx.coroutines.core.v181)
    
    // Google OAuth
    implementation(libs.androidx.credentials)
    implementation(libs.androidx.credentials.play.services.auth)
    implementation(libs.google.identity.googleid)
}

fun getProperty(propertyKey: String): String {
    val properties = com.android.build.gradle.internal.cxx.configure.gradleLocalProperties(
        project.rootDir,
        providers
    )
    return properties.getProperty(propertyKey)
        ?: throw GradleException("Property $propertyKey not found in local.properties")
}