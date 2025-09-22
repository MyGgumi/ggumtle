package com.ggumtle.network.rest.util.authenticator

import com.ggumtle.core.network.BuildConfig
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import okhttp3.MediaType.Companion.toMediaType
import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.kotlinx.serialization.asConverterFactory
import java.util.concurrent.TimeUnit
import javax.inject.Qualifier
import javax.inject.Singleton
import kotlin.jvm.java

@Qualifier
@Retention(AnnotationRetention.BINARY)
annotation class TokenRefreshRetrofit

@Module
@InstallIn(SingletonComponent::class)
object TokenServiceModule {

    private val baseUrl = BuildConfig.REST_BASE_URL

    @Provides
    @Singleton
    @TokenRefreshRetrofit
    fun provideTokenRefreshOkHttpClient(): OkHttpClient {
        return OkHttpClient.Builder()
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .writeTimeout(30, TimeUnit.SECONDS)
            .retryOnConnectionFailure(true)
            .build()
    }

    @Provides
    @Singleton
    @TokenRefreshRetrofit
    fun provideTokenRefreshRetrofit(
        @TokenRefreshRetrofit okHttpClient: OkHttpClient,
        json: Json
    ): Retrofit {
        return Retrofit.Builder()
            .baseUrl(baseUrl)
            .client(okHttpClient)
            .addConverterFactory(json.asConverterFactory("application/json".toMediaType()))
            .build()
    }

    @Provides
    @Singleton
    fun provideTokenRefreshService(
        @TokenRefreshRetrofit retrofit: Retrofit  // @TokenRefreshRetrofit 추가!
    ): TokenRefreshService {
        return retrofit.create(TokenRefreshService::class.java)
    }
}