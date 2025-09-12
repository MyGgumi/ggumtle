package com.ggumtle.ggumtle

import android.app.Application
import com.ggumtle.datastore.AuthManager
import com.ggumtle.domain.manager.GlobalInviteManager
import dagger.hilt.android.HiltAndroidApp
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.launch
import javax.inject.Inject

@HiltAndroidApp
class MyApplication : Application() {
    private val applicationScope = CoroutineScope(SupervisorJob() + Dispatchers.Main)

    @Inject
    lateinit var authManager: AuthManager
    
    @Inject
    lateinit var globalInviteManager: GlobalInviteManager

    override fun onCreate() {
        super.onCreate()
        applicationScope.launch {
            authManager.checkAutoLogin()
        }
        
        // 앱 시작 시 초대 알림 관찰 시작
        globalInviteManager.startObserving()
    }
    
    override fun onTerminate() {
        super.onTerminate()
        globalInviteManager.stopObserving()
    }
}