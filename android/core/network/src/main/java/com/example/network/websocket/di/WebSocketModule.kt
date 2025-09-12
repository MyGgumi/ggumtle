package com.ggumtle.network.websocket.di

import com.ggumtle.datastore.AuthManager
import com.ggumtle.network.websocket.client.WebSocketClient
import com.ggumtle.network.websocket.client.WebSocketClientImpl
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import okhttp3.OkHttpClient
import java.util.concurrent.TimeUnit
import javax.inject.Named
import javax.inject.Singleton

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
            .pingInterval(30, TimeUnit.SECONDS)
            .build()
    }

    @Provides
    @Singleton
    fun provideWebSocketClient(
        @Named("websocket") okHttpClient: OkHttpClient,
        authManager: AuthManager
    ): WebSocketClient {
        return WebSocketClientImpl(okHttpClient, authManager)
    }
}