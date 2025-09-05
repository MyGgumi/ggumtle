package com.example.data.websocket.mapper

import com.example.data.websocket.model.request.WebSocketRequestDto
import com.example.data.websocket.model.request.toDto
import com.example.data.websocket.model.response.FriendRequestResponseDto
import com.example.data.websocket.model.response.toDomain
import com.example.domain.websocket.model.MessageTypes
import com.example.domain.websocket.model.request.FriendRequestData
import com.example.domain.websocket.model.response.WebSocketResponse
import kotlinx.serialization.json.*
import javax.inject.*

@Singleton
class MessageMapper @Inject constructor(
    private val json: Json
) {

    fun <T> createRequestJson(type: String, data: T): Result<String> {
        return try {
            val dto = when (type) {
                MessageTypes.REQUEST_FRIEND -> {
                    val domainData = data as? FriendRequestData
                        ?: return Result.failure(IllegalArgumentException("Invalid data type for REQUEST_FRIEND"))
                    WebSocketRequestDto(type, domainData.toDto())
                }
                else -> WebSocketRequestDto(type, data)
            }
            Result.success(json.encodeToString(dto))
        } catch (e: Exception) {
            Result.failure(e)
        }
    }

    fun parseResponseJson(jsonString: String): Result<WebSocketResponse<*>> {
        return try {
            val jsonElement = json.parseToJsonElement(jsonString)
            val jsonObject = jsonElement.jsonObject

            val type = jsonObject["type"]?.jsonPrimitive?.content
                ?: return Result.failure(IllegalArgumentException("Missing 'type' field"))
            val success = jsonObject["success"]?.jsonPrimitive?.boolean
                ?: return Result.failure(IllegalArgumentException("Missing 'success' field"))
            val code = jsonObject["code"]?.jsonPrimitive?.contentOrNull
            val dataElement = jsonObject["data"]
                ?: return Result.failure(IllegalArgumentException("Missing 'data' field"))

            val response = when (type) {
                MessageTypes.REQUEST_FRIEND -> {
                    if (success) {
                        val dto = json.decodeFromJsonElement<FriendRequestResponseDto>(dataElement)
                        WebSocketResponse(type, success, code, dto.toDomain())
                    } else {
                        val errorMessage = dataElement.jsonPrimitive.content
                        WebSocketResponse(type, success, code, errorMessage)
                    }
                }
                else -> {
                    val data = if (dataElement is JsonPrimitive) {
                        dataElement.content
                    } else {
                        dataElement.jsonObject
                    }
                    WebSocketResponse(type, success, code, data)
                }
            }

            Result.success(response)
        } catch (e: Exception) {
            Result.failure(IllegalArgumentException("Failed to parse WebSocket message: ${e.message}", e))
        }
    }
}