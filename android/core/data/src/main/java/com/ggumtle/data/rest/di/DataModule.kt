package com.ggumtle.data.rest.di

import com.ggumtle.data.rest.repository.MemberRepositoryImpl
import com.ggumtle.domain.rest.repository.MemberRepository
import com.ggumtle.data.rest.repository.AuthRepositoryImpl
import com.ggumtle.data.rest.repository.GrowthRepositoryImpl
import com.ggumtle.data.rest.repository.MissionRepositoryImpl
import com.ggumtle.domain.rest.repository.AuthRepository
import com.ggumtle.domain.rest.repository.GrowthRepository
import com.ggumtle.domain.rest.repository.MissionRepository
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
        memberRepositoryImpl: MemberRepositoryImpl
    ): MemberRepository

    @Binds
    @Singleton
    fun bindGrowthRepository(
        growthRepositoryImpl: GrowthRepositoryImpl
    ): GrowthRepository

    @Binds
    @Singleton
    fun bindMissionRepository(
        missionRepositoryImpl: MissionRepositoryImpl
    ): MissionRepository

}