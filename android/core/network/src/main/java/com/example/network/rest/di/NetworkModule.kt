package com.example.network.rest.di

import com.example.datastore.AuthManager
import com.example.multimodulebase.core.network.BuildConfig
import com.example.network.rest.util.authenticator.TokenAuthenticator
import com.example.network.rest.util.calladapter.NetworkResultCallAdapterFactory
import com.example.network.rest.util.convertor.NullOnEmptyConverterFactory
import com.example.network.rest.util.interceptor.AuthInterceptor
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import okhttp3.Authenticator
import okhttp3.Interceptor
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import java.util.concurrent.TimeUnit
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object NetworkModule {

    private val baseUrl = BuildConfig.REST_BASE_URL

    @Provides
    @Singleton
    fun provideJson(): Json {
        return Json {
            ignoreUnknownKeys = true // API에서 추가 필드가 와도 무시
            coerceInputValues = true // null 값을 기본값으로 변환
            encodeDefaults = true
        }
    }

    @Provides
    @Singleton
    fun provideAuthInterceptor(
        authManager: AuthManager
    ): Interceptor = AuthInterceptor(authManager)

    @Provides
    @Singleton
    fun provideTokenAuthenticator(
        tokenAuthenticator: TokenAuthenticator
    ): Authenticator = tokenAuthenticator

    @Provides
    @Singleton
    fun provideOkHttpClient(
        authInterceptor: Interceptor,
        tokenAuthenticator: Authenticator,
    ): OkHttpClient {
        return OkHttpClient.Builder().apply {

            // 인증 토큰 자동 첨부 인터셉터
            addInterceptor(authInterceptor)

            //토근 갱신
            authenticator(tokenAuthenticator)

            // 타임아웃 설정
            connectTimeout(30, TimeUnit.SECONDS)
            readTimeout(30, TimeUnit.SECONDS)
            writeTimeout(30, TimeUnit.SECONDS)

            // 재시도 설정
            retryOnConnectionFailure(true)

        }.build()
    }

    @Provides
    @Singleton
    fun provideRetrofit(
        okHttpClient: OkHttpClient
    ): Retrofit {
        val contentType = "application/json".toMediaType()
        val json = Json { ignoreUnknownKeys = true }
        return Retrofit.Builder()
            .baseUrl(baseUrl)
            .client(okHttpClient)
            .addConverterFactory(NullOnEmptyConverterFactory)
            .addConverterFactory(json.asConverterFactory(contentType))
            .addCallAdapterFactory(NetworkResultCallAdapterFactory.create())
            .build()
    }

}