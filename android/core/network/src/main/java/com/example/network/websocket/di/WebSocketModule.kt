package com.example.network.websocket.di

import com.example.datastore.AuthManager
import com.example.network.websocket.datasource.OkHttpWebSocketDataSource
import com.example.network.websocket.datasource.WebSocketDataSource
import dagger.*
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.coroutines.CoroutineDispatcher
import okhttp3.OkHttpClient
import java.util.concurrent.TimeUnit
import javax.inject.*

// core/network/websocket/di/WebSocketModule.kt
@Module
@InstallIn(SingletonComponent::class)
object WebSocketModule {

    @Provides
    @Singleton
    @Named("websocket")
    fun provideWebSocketOkHttpClient(): OkHttpClient {
        return OkHttpClient.Builder()
            .connectTimeout(30, TimeUnit.SECONDS)
            .readTimeout(0, TimeUnit.SECONDS) // WebSocket은 무제한
            .writeTimeout(30, TimeUnit.SECONDS)
            .retryOnConnectionFailure(true)
            .build()
    }

    @Provides
    @Singleton
    fun provideWebSocketDataSource(
        @Named("websocket") okHttpClient: OkHttpClient,
        authManager: AuthManager
    ): WebSocketDataSource {
        return OkHttpWebSocketDataSource(okHttpClient, authManager)
    }
}