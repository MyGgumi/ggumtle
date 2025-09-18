package com.ggumtle.data.rest.di

import com.example.data.rest.repository.UserRepositoryImpl
import com.example.domain.rest.repository.UserRepository
import com.ggumtle.data.rest.repository.AuthRepositoryImpl
import com.ggumtle.domain.rest.repository.AuthRepository
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
internal interface DataModule {

    @Binds
    @Singleton
    fun bindAuthRepository(
        authRepositoryImpl: AuthRepositoryImpl
    ): AuthRepository

    @Binds
    @Singleton
    fun bindUserRepository(
        userRepositoryImpl: UserRepositoryImpl
    ): UserRepository

}