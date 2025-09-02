package com.example.convention

import com.android.build.api.dsl.CommonExtension
import org.gradle.api.Project
import org.gradle.api.provider.Provider
import org.gradle.kotlin.dsl.configure
import org.gradle.kotlin.dsl.dependencies
import org.jetbrains.kotlin.compose.compiler.gradle.ComposeCompilerGradlePluginExtension

// Android Compose 설정을 공통화하기 위한 확장 함수
internal fun Project.configureAndroidCompose(
    commonExtension: CommonExtension<*, *, *, *, *, *>,
) {
    commonExtension.apply {

        buildFeatures {
            compose = true
        }

        // Compose 관련 의존성 추가
        dependencies {
            // BOM(Bill of Materials)을 사용해 Compose 라이브러리 버전 정합성 유지
            val bom = libs.findLibrary("androidx-compose-bom").get()
            "implementation"(platform(bom)) // 일반 구현 시 BOM 사용
            "androidTestImplementation"(platform(bom)) // 테스트에도 BOM 적용

            // Compose UI 미리보기 지원
            "implementation"(libs.findLibrary("androidx-ui-tooling-preview").get())
            // 디버그 빌드에서만 툴링 툴 포함
            "debugImplementation"(libs.findLibrary("androidx-ui-tooling").get())
            // UI 테스트 매니페스트 (디버그용)
            "debugImplementation"(libs.findLibrary("androidx-ui-test-manifest").get())
        }

        // 단위 테스트 시 Android 리소스를 사용할 수 있도록 설정 (Robolectric 테스트 대응)
        testOptions {
            unitTests {
                isIncludeAndroidResources = true
            }
        }
    }

    // Compose 컴파일러 확장 설정 (성능 분석 및 디버깅용)
    extensions.configure<ComposeCompilerGradlePluginExtension> {
        // true인 경우에만 동작하게 하기 위한 확장 함수
        fun Provider<String>.onlyIfTrue() = flatMap { provider { it.takeIf(String::toBoolean) } }

        // 프로젝트 루트 상대 경로로 저장될 디렉토리 위치 계산
        fun Provider<*>.relativeToRootProject(dir: String) = map {
            isolated.rootProject.projectDirectory
                .dir("build")
                .dir(projectDir.toRelativeString(rootDir))
        }.map { it.dir(dir) }

        // gradle.properties에 enableComposeCompilerMetrics=true 일 때 metrics 저장
        project.providers.gradleProperty("enableComposeCompilerMetrics").onlyIfTrue()
            .relativeToRootProject("compose-metrics")
            .let(metricsDestination::set)

        // gradle.properties에 enableComposeCompilerReports=true 일 때 리포트 저장
        project.providers.gradleProperty("enableComposeCompilerReports").onlyIfTrue()
            .relativeToRootProject("compose-reports")
            .let(reportsDestination::set)

        // 안정성 분석용 설정파일 (선택사항)
        stabilityConfigurationFiles
            .add(isolated.rootProject.projectDirectory.file("compose_compiler_config.conf"))
    }
}