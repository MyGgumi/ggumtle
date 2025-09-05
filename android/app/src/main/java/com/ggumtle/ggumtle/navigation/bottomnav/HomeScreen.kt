//package com.ggumtle.ggumtle.navigation.bottomnav
//
//import androidx.compose.foundation.background
//import androidx.compose.foundation.layout.*
//import androidx.compose.material3.*
//import androidx.compose.runtime.*
//import androidx.compose.ui.Alignment
//import androidx.compose.ui.Modifier
//import androidx.compose.ui.graphics.Color
//import androidx.compose.ui.unit.dp
//import androidx.compose.ui.unit.sp
//import androidx.compose.foundation.shape.RoundedCornerShape
//import com.unity3d.player.UnityPlayer
//
//@Composable
//fun HomeScreen() {
//    var characterCount by remember { mutableStateOf(0) }
//
//    Column(
//        modifier = Modifier
//            .fillMaxWidth()
//            .padding(20.dp),
//        horizontalAlignment = Alignment.CenterHorizontally
//    ) {
//        Text(
//            text = "캐릭터: $characterCount",
//            color = Color.White,
//            fontSize = 20.sp,
//            modifier = Modifier
//                .background(Color.Black.copy(0.7f), RoundedCornerShape(8.dp))
//                .padding(12.dp)
//        )
//
//        Spacer(modifier = Modifier.height(16.dp))
//
//        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
//            Button(
//                onClick = {
//                    try {
//                        UnityPlayer.UnitySendMessage("UnityAndroidBridge", "AddCharacterFromAndroid", "")
//                        characterCount++
//                    } catch (e: Exception) {
//                        // Unity 메시지 전송 실패 처리
//                        println("Unity message send failed: ${e.message}")
//                    }
//                }
//            ) {
//                Text("추가")
//            }
//
//            Button(
//                onClick = {
//                    if (characterCount > 0) {
//                        try {
//                            UnityPlayer.UnitySendMessage("UnityAndroidBridge", "RemoveCharacterFromAndroid", "")
//                            characterCount--
//                        } catch (e: Exception) {
//                            // Unity 메시지 전송 실패 처리
//                            println("Unity message send failed: ${e.message}")
//                        }
//                    }
//                },
//                enabled = characterCount > 0
//            ) {
//                Text("제거")
//            }
//        }
//    }
//}