package com.example.data.websocket.di

import com.example.data.websocket.mapper.MessageMapper
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import kotlinx.serialization.json.Json
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object DataModule {

    @Provides
    @Singleton
    fun provideMessageMapper(
        json: Json
    ): MessageMapper {
        return MessageMapper(json)
    }
}