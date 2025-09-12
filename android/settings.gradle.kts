pluginManagement {
    includeBuild("build-logic")
    repositories {
        google {
            content {
                includeGroupByRegex("com\\.android.*")
                includeGroupByRegex("com\\.google.*")
                includeGroupByRegex("androidx.*")
            }
        }
        mavenCentral()
        gradlePluginPortal()
    }
}

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
    }
}

rootProject.name = "ggumtle"

enableFeaturePreview("TYPESAFE_PROJECT_ACCESSORS")
include(":app")
include(":core")
include(":feature")
include(":core:common")
include(":core:network")
include(":core:database")
include(":core:datastore")
include(":core:designsystem")
include(":core:domain")
include(":core:data")
include(":core:model")
include(":core:ui")
include(":feature:auth")

// Unity 모듈 추가
include(":unityLibrary")

// Unity 라이브러리 경로 설정 (외부 경로에 있는 경우)
project(":unityLibrary").projectDir = file("C:\\Users\\SSAFY\\Documents\\unity\\mainpagetest\\unityLibrary")
include(":feature:startup")
include(":feature:social")
include(":feature:home")
include(":feature:growth")
