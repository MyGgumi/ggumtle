/**
 * AR 평면 렌더링 및 시각화
 * - 감지된 평면들을 메시로 변환하여 렌더링
 * - 평면 텍스처 및 그림자 적용
 * - 삼각형 팬을 이용한 볼록 폴리곤 렌더링
 * - 수평/수직 평면 처리 및 UV 좌표 매핑
 */
package com.ggumtle.mission.ar.renderer

import android.content.Context
import com.ggumtle.mission.R
import com.ggumtle.mission.ar.util.count
import com.ggumtle.mission.ar.util.dimenV2A
import com.ggumtle.mission.ar.util.dimenV4A
import com.ggumtle.mission.ar.filament.Filament
import com.ggumtle.mission.ar.util.horizontalToUV
import com.ggumtle.mission.ar.util.matrix
import com.ggumtle.mission.ar.util.polygonToUV
import com.ggumtle.mission.ar.util.polygonToVertices
import com.ggumtle.mission.ar.util.readUncompressedAsset
import com.ggumtle.mission.ar.util.size
import com.ggumtle.mission.ar.util.triangleIndexArrayCreate
import com.google.android.filament.Box
import com.google.android.filament.Entity
import com.google.android.filament.EntityManager
import com.google.android.filament.IndexBuffer
import com.google.android.filament.Material
import com.google.android.filament.MaterialInstance
import com.google.android.filament.RenderableManager
import com.google.android.filament.TextureSampler
import com.google.android.filament.VertexBuffer
import com.google.android.filament.utils.TextureType
import com.google.android.filament.utils.loadTexture
import com.google.ar.core.Frame
import com.google.ar.core.Plane
import com.google.ar.core.TrackingState
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.nio.FloatBuffer
import java.nio.ShortBuffer
import kotlin.math.max
import kotlin.math.min

/**
 * AR 평면 렌더링을 담당하는 메인 클래스
 * ARCore에서 감지한 평면들을 3D 메시로 변환하여 시각화
 * @param context 안드로이드 애플리케이션 컨텍스트 - 에셋 파일 및 리소스 접근에 필요
 * @param filament 3D 렌더링 엔진 - 실제 평면 렌더링 담당
 */
class PlaneRenderer(context: Context, private val filament: Filament) {
    /**
     * 평면 렌더링 상수 정의
     */
    companion object {
        // 평면 렌더링에 사용될 최대 버텍스 개수 - 메모리 효율성을 위해 제한
        private const val PLANE_VERTEX_BUFFER_SIZE: Int = 1000
        // 인덱스 버퍼 크기 - 삼각형 팬 방식으로 (n-2)*3 공식 사용
        // n개의 버텍스로 n-2개의 삼각형 생성 가능, 각 삼각형당 3개의 인덱스
        private const val PLANE_INDEX_BUFFER_SIZE: Int = (PLANE_VERTEX_BUFFER_SIZE - 2) * 3
    }

    // 평면 텍스처 렌더링을 위한 지소(Material) - 컴파일된 셰이더 프로그램
    // .filamat 파일은 Filament 머티리얼 데이터 포맷 (바이너리 셰이더 코드 포함)
    private val textureMaterial: Material = context
        .readUncompressedAsset("materials/textured.filamat")  // assets에서 컴파일된 셰이더 로드
        .let { byteBuffer ->
            Material
                .Builder()  // Filament Material 빌더 생성
                .payload(byteBuffer, byteBuffer.remaining())  // 컴파일된 셰이더 데이터 설정
                .build(filament.engine)  // Filament 엔진에서 Material 인스턴스 생성
        }

    // 평면 텍스처 머티리얼 인스턴스 - 실제 렌더링에 사용될 머티리얼 객체
    // Material은 템플릿, MaterialInstance는 그 템플릿에 구체적인 파라미터를 적용한 인스턴스
    private val textureMaterialInstance: MaterialInstance = textureMaterial
        .createInstance()  // 베이스 Material에서 인스턴스 생성
        .also { materialInstance ->
            // 평면 시각화에 사용될 텍스처 설정
            materialInstance.setParameter(
                "texture",  // 셰이더에서 정의된 texture 파라미터명
                // drawable 리소스에서 텍스처 로드 및 Filament 텍스처로 변환
                loadTexture(filament.engine, context.resources, R.drawable.sceneform_plane, TextureType.COLOR),
                // 텍스처 샘플링 설정 - anisotropy 8.0f로 고품질 필터링 적용
                TextureSampler().also { it.anisotropy = 8.0f },
            )

            // materialInstance.setParameter("alpha", 1f) // 투명도 설정 (주석 처리됨)
        }

    // 그림자 렌더링을 위한 지소(Material) - 평면 위에 그림자 효과 생성
    // 그림자는 모델이 평면에 떨어지는 그림자를 시각적으로 표현하여 추가적인 입체감 제공
    private val shadowMaterial: Material = context
        .readUncompressedAsset("materials/shadow.filamat")  // 그림자 전용 셰이더 로드
        .let { byteBuffer ->
            Material
                .Builder()  // 그림자 Material 빌더 생성
                .payload(byteBuffer, byteBuffer.remaining())  // 컴파일된 그림자 셰이더 데이터 설정
                .build(filament.engine)  // Filament 엔진에서 그림자 Material 인스턴스 생성
        }

    // 평면 버텍스 데이터를 저장할 네이티브 메모리 버퍼 - GPU와 직접 통신
    // PLANE_VERTEX_BUFFER_SIZE * dimenV4A(4차원 벡터) * Float 크기로 메모리 할당
    // 각 버텍스는 (x, y, z, w) 4개의 float 값으로 구성 (homogeneous coordinates)
    private val planeVertexFloatBuffer: FloatBuffer = ByteBuffer
        .allocateDirect(PLANE_VERTEX_BUFFER_SIZE * dimenV4A * Float.size)  // 네이티브 메모리 직접 할당
        .order(ByteOrder.nativeOrder())  // 플랫폼의 네이티브 바이트 순서 사용 (성능 최적화)
        .asFloatBuffer()  // ByteBuffer를 FloatBuffer로 변환

    // 평면 UV 좌표 데이터를 저장할 네이티브 메모리 버퍼 - 텍스처 매핑에 사용
    // PLANE_VERTEX_BUFFER_SIZE * dimenV2A(2차원 벡터) * Float 크기로 메모리 할당
    // 각 UV 좌표는 (u, v) 2개의 float 값으로 구성 (0.0~1.0 범위의 텍스처 좌표)
    private val planeUvFloatBuffer: FloatBuffer = ByteBuffer
        .allocateDirect(PLANE_VERTEX_BUFFER_SIZE * dimenV2A * Float.size)  // UV 좌표용 네이티브 메모리 할당
        .order(ByteOrder.nativeOrder())  // 네이티브 바이트 순서 사용
        .asFloatBuffer()  // ByteBuffer를 FloatBuffer로 변환

    // 평면 인덱스 데이터를 저장할 버퍼 - 삼각형 그리기 순서 정의
    // Short 타입 사용으로 메모리 효율성 높임 (0~65535 범위의 버텍스 인덱스)
    // 삼각형 팬 방식으로 볼록 폴리곤을 여러 삼각형으로 분할
    private val planeIndexShortBuffer: ShortBuffer = ShortBuffer
        .allocate(PLANE_INDEX_BUFFER_SIZE)  // JVM 힙 메모리에 Short 배열 할당

    // Filament GPU 버텍스 버퍼 - 실제 GPU에서 사용될 버텍스 데이터 저장소
    // 2개의 버퍼를 사용: 0번 = 위치 데이터, 1번 = UV 좌표 데이터
    private val planeVertexBuffer: VertexBuffer = VertexBuffer
        .Builder()  // Filament VertexBuffer 빌더 생성
        .vertexCount(PLANE_VERTEX_BUFFER_SIZE)  // 최대 버텍스 개수 설정
        .bufferCount(2)  // 사용할 버퍼 개수 (위치 + UV)
        .attribute(  // 첫 번째 어트리뷰트: 버텍스 위치 정보
            VertexBuffer.VertexAttribute.POSITION,  // 위치 어트리뷰트 타입
            0,  // 버퍼 인덱스 0번 사용
            VertexBuffer.AttributeType.FLOAT4,  // 4개의 float 값 (x, y, z, w)
            0,  // 바이트 오프셋 (0부터 시작)
            0,  // stride (연속된 버텍스 간 간격, 0 = tight packing)
        )
        .attribute(  // 두 번째 어트리뷰트: UV 좌표 정보
            VertexBuffer.VertexAttribute.UV0,  // 첫 번째 UV 좌표 어트리뷰트
            1,  // 버퍼 인덱스 1번 사용
            VertexBuffer.AttributeType.FLOAT2,  // 2개의 float 값 (u, v)
            0,  // 바이트 오프셋
            0,  // stride
        )
        .build(filament.engine)  // Filament 엔진에서 VertexBuffer 인스턴스 생성

    // Filament GPU 인덱스 버퍼 - 삼각형 그리기 순서를 정의하는 인덱스 데이터 저장소
    // 버텍스 인덱스를 이용하여 폴리곤을 삼각형들로 분할하여 렌더링 효율성 향상
    private val planeIndexBuffer: IndexBuffer = IndexBuffer
        .Builder()  // Filament IndexBuffer 빌더 생성
        .indexCount(PLANE_INDEX_BUFFER_SIZE)  // 최대 인덱스 개수 설정
        .bufferType(IndexBuffer.Builder.IndexType.USHORT)  // unsigned short (16비트) 인덱스 사용
        .build(filament.engine)  // Filament 엔진에서 IndexBuffer 인스턴스 생성

    // 평면 렌더링 가능 객체의 엔티티 ID - Filament 엔티티 시스템에서 사용
    // @Entity 어노테이션은 이 값이 Filament 엔티티 ID임을 명시
    // EntityManager를 통해 고유한 엔티티 생성 후 씨에 추가하여 렌더링 대상으로 등록
    @Entity
    private val planeRenderable: Int =
        EntityManager.get().create().also { filament.scene.addEntity(it) }

    /**
     * AR 프레임 업데이트 함수 - 매 프레임마다 평면 데이터 업데이트 및 렌더링
     * @param frame ARCore에서 제공하는 현재 프레임 데이터 - 감지된 평면 정보 포함
     */
    fun doFrame(frame: Frame) {
        // ARCore에서 감지된 평면 데이터 수집 및 필터링
        val planeTrackables = frame
            .getUpdatedTrackables(Plane::class.java)  // 현재 프레임에서 업데이트된 평면 목록 획득
            .map { plane -> plane.subsumedBy ?: plane }  // 병합된 평면이 있으면 부모 평면 사용, 없으면 원본 평면 사용
            .toSet()  // 중복 제거 - 동일한 평면이 여러 번 처리되는 것을 방지
            .filter { plane -> plane.trackingState == TrackingState.TRACKING }  // 제대로 추적되는 평면만 필터링
            .also { if (it.isEmpty()) return }  // 렌더링할 평면이 없으면 조기 종료
            .sortedBy { it.type != Plane.Type.HORIZONTAL_UPWARD_FACING }  // 수평 평면을 먼저 렌더링 (그림자 적용 순서)

        // 수평 평면이 아닌 첫 번째 평면의 인덱스 - 그림자 적용 범위 결정에 사용
        // 수평 평면에만 그림자를 적용하고, 수직 평면에는 그림자를 적용하지 않음
        val indexNotUpwardFacing = planeTrackables
            .indexOfFirst { it.type != Plane.Type.HORIZONTAL_UPWARD_FACING }

        // 모든 평면을 포함하는 바운딩 박스 계산을 위한 최소/최대 좌표 초기화
        // 바운딩 박스는 렌더링 최적화를 위해 사용 (컬링, 절두체 검사 등)
        var xMin = Float.POSITIVE_INFINITY  // X축 최소값 초기화 (양의 무한대로 설정)
        var xMax = Float.NEGATIVE_INFINITY  // X축 최대값 초기화 (음의 무한대로 설정)
        var yMin = Float.POSITIVE_INFINITY  // Y축 최소값 초기화
        var yMax = Float.NEGATIVE_INFINITY  // Y축 최대값 초기화
        var zMin = Float.POSITIVE_INFINITY  // Z축 최소값 초기화
        var zMax = Float.NEGATIVE_INFINITY  // Z축 최대값 초기화

        // 버퍼 오프셋 초기화 - 여러 평면의 데이터를 순차적으로 배치하기 위해 사용
        var vertexBufferOffset = 0  // 버텍스 버퍼에서의 현재 위치 오프셋
        var indexBufferOffset = 0   // 인덱스 버퍼에서의 현재 위치 오프셋

        // 그림자가 적용되지 않는 첫 번째 인덱스 - 수직 평면의 시작 지점
        // 수평 평면에만 그림자를 적용하고 수직 평면에는 적용하지 않기 위해 사용
        var indexWithoutShadow: Int? = null

        // 버퍼 포지션을 처음으로 되돌리기 - 새로운 프레임 데이터로 덮어쓰기 준비
        // rewind()는 position을 0으로 설정하여 버퍼의 처음부터 데이터를 쓸 수 있도록 함
        planeVertexFloatBuffer.rewind()  // 버텍스 버퍼 포지션 초기화
        planeUvFloatBuffer.rewind()      // UV 버퍼 포지션 초기화
        planeIndexShortBuffer.rewind()   // 인덱스 버퍼 포지션 초기화

        // 감지된 모든 평면에 대해 순차적으로 처리 - 각 평면을 메시로 변환
        for (i in 0 until planeTrackables.count()) {
            val plane = planeTrackables[i]  // i번째 평면 객체 획득

            // 그림자 적용 시작 지점 결정 - 수평 평면과 수직 평면 구분
            // 수평 평면(바닥)에만 그림자를 적용하고, 수직 평면(벽)에는 적용하지 않음
            if (i == indexNotUpwardFacing) {
                indexWithoutShadow = indexBufferOffset  // 수직 평면 시작 인덱스 기록
            }

            // 평면의 경계 폴리곤을 3D 월드 좌표계의 버텍스 리스트로 변환
            // plane.polygon: 평면 로컬 좌표계에서의 경계점들
            // plane.centerPose.matrix(): 평면 중심의 월드 변환 매트릭스
            val planeVertices = plane.polygon.polygonToVertices(plane.centerPose.matrix())

            // 볼록 폴리곤을 삼각형들로 분할하는 삼각형 팬(Triangle Fan) 인덱스 생성
            // n개의 버텍스로 n-2개의 삼각형 생성 (fan 형태로 방사형 분할)
            // 첫 번째 버텍스를 중심으로 하여 인접한 두 버텍스와 삼각형 형성
            val planeTriangleIndices =
                triangleIndexArrayCreate(
                    planeVertices.count - 2,  // 생성할 삼각형 개수
                    { vertexBufferOffset.toShort() },  // 중심 버텍스 인덱스 (항상 첫 번째)
                    { k -> (vertexBufferOffset + k + 1).toShort() },  // k번째 삼각형의 두 번째 버텍스
                    { k -> (vertexBufferOffset + k + 2).toShort() },  // k번째 삼각형의 세 번째 버텍스
                )

            // 버퍼 오버플로우 방지 검사 - 미리 할당된 버퍼 크기를 초과하는지 확인
            // 버퍼 크기를 초과하면 더 이상 평면을 추가하지 않고 현재까지의 데이터로 렌더링
            if (vertexBufferOffset + planeVertices.count > PLANE_VERTEX_BUFFER_SIZE ||  // 버텍스 버퍼 오버플로우 검사
                indexBufferOffset + planeTriangleIndices.shortArray.count() > PLANE_INDEX_BUFFER_SIZE  // 인덱스 버퍼 오버플로우 검사
            ) {
                break  // 루프 중단 - 더 이상 평면 처리 불가
            }

            // 모든 평면을 포함하는 바운딩 박스 계산 - 렌더링 최적화를 위해 필요
            // 버텍스 데이터는 4개씩 그룹화되어 있음 (x, y, z, w) - step 4로 순회
            for (k in planeVertices.floatArray.indices step 4) {
                xMin = min(planeVertices.floatArray[k + 0], xMin)  // X좌표 최소값 업데이트
                xMax = max(planeVertices.floatArray[k + 0], xMax)  // X좌표 최대값 업데이트
                yMin = min(planeVertices.floatArray[k + 1], yMin)  // Y좌표 최소값 업데이트
                yMax = max(planeVertices.floatArray[k + 1], yMax)  // Y좌표 최대값 업데이트
                zMin = min(planeVertices.floatArray[k + 2], zMin)  // Z좌표 최소값 업데이트
                zMax = max(planeVertices.floatArray[k + 2], zMax)  // Z좌표 최대값 업데이트
            }

            // 버텍스 데이터를 CPU 메모리 버퍼에 추가 - GPU 전송 전 중간 단계
            // NIO 버퍼를 사용하여 효율적인 메모리 관리 및 GPU 전송 준비
            planeVertexFloatBuffer.put(planeVertices.floatArray)  // 변환된 버텍스 데이터를 버퍼에 추가

            // UV 좌표 데이터를 버퍼에 추가 - 텍스처 매핑을 위한 좌표
            // 평면 타입에 따라 다른 UV 매핑 방식 사용
            planeUvFloatBuffer.put(
                if (plane.type == Plane.Type.VERTICAL) {
                    // 수직 평면: 평면 로컬 좌표계에서 UV 좌표 계산
                    // 벽면 같은 수직 평면은 평면 자체의 2D 좌표계를 사용
                    plane.polygon.polygonToUV()
                } else {
                    // 수평 평면: 3D 월드 좌표를 2D UV로 변환
                    // 바닥 같은 수평 평면은 월드 좌표의 X, Z 좌표를 UV로 사용
                    planeVertices.horizontalToUV()
                }.floatArray,  // 계산된 UV 데이터를 float 배열로 변환하여 버퍼에 추가
            )

            // 삼각형 인덱스 데이터를 버퍼에 추가 - GPU에서 삼각형 그리기 순서 지정
            planeIndexShortBuffer.put(planeTriangleIndices.shortArray)

            // 다음 평면을 위해 버퍼 오프셋 업데이트
            // 현재 평면의 데이터 크기를 오프셋에 추가하여 다음 평면의 시작 위치 결정
            vertexBufferOffset += planeVertices.count  // 버텍스 버퍼에서 다음 사용 위치
            indexBufferOffset += planeTriangleIndices.shortArray.count()  // 인덱스 버퍼에서 다음 사용 위치
        }

        // CPU 메모리 버퍼에서 GPU 메모리로 데이터 전송 - 실제 렌더링을 위한 준비
        // 버텍스 데이터 전송: 위치 좌표 (x, y, z, w)
        var count = planeVertexFloatBuffer.capacity() - planeVertexFloatBuffer.remaining()  // 실제 사용된 바이트 수 계산
        planeVertexFloatBuffer.rewind()  // 버퍼 포지션을 처음으로 되돌려 데이터 읽기 준비

        // GPU VertexBuffer의 0번 슬롯에 버텍스 위치 데이터 업로드
        planeVertexBuffer.setBufferAt(
            filament.engine,        // Filament 엔진 인스턴스
            0,                      // 버퍼 인덱스 0번 (위치 데이터)
            planeVertexFloatBuffer, // 전송할 CPU 버퍼
            0,                      // 시작 오프셋
            count,                  // 전송할 데이터 사이즈
        )

        // UV 좌표 데이터 전송: 텍스처 매핑 좌표 (u, v)
        count = planeUvFloatBuffer.capacity() - planeUvFloatBuffer.remaining()  // UV 데이터 사이즈 계산
        planeUvFloatBuffer.rewind()  // UV 버퍼 포지션 리셋

        // GPU VertexBuffer의 1번 슬롯에 UV 데이터 업로드
        planeVertexBuffer.setBufferAt(
            filament.engine,    // Filament 엔진 인스턴스
            1,                  // 버퍼 인덱스 1번 (UV 데이터)
            planeUvFloatBuffer, // 전송할 UV 버퍼
            0,                  // 시작 오프셋
            count,              // 전송할 UV 데이터 사이즈
        )

        // 인덱스 데이터 전송: 삼각형 그리기 순서 지정
        count = planeIndexShortBuffer.capacity() - planeIndexShortBuffer.remaining()  // 인덱스 데이터 사이즈 계산
        planeIndexShortBuffer.rewind()  // 인덱스 버퍼 포지션 리셋

        // GPU IndexBuffer에 인덱스 데이터 업로드
        planeIndexBuffer.setBuffer(
            filament.engine,        // Filament 엔진 인스턴스
            planeIndexShortBuffer,  // 전송할 인덱스 버퍼
            0,                      // 시작 오프셋
            count,                  // 전송할 인덱스 데이터 사이즈
        )

        // Filament RenderableManager로 평면 렌더링 설정 구성
        // 2개의 지오메트리를 사용: 1) 텍스처 평면, 2) 그림자 평면
        RenderableManager
            .Builder(1)  // 1개의 primitive 지오메트리 사용
            .castShadows(false)    // 평면은 그림자를 던지지 않음 (반사만 함)
            .receiveShadows(true)  // 평면은 다른 객체의 그림자를 받음
            .culling(true)         // 백페이스 컬링 활성화 (성능 최적화)
            .boundingBox(  // 평면들을 둘러싼는 바운딩 박스 설정
                Box(
                    (xMin + xMax) / 2f,  // 바운딩 박스 중심 X 좌표
                    (yMin + yMax) / 2f,  // 바운딩 박스 중심 Y 좌표
                    (zMin + zMax) / 2f,  // 바운딩 박스 중심 Z 좌표
                    (xMax - xMin) / 2f,  // X축 반지름 (너비의 절반)
                    (yMax - yMin) / 2f,  // Y축 반지름 (높이의 절반)
                    (zMax - zMin) / 2f,  // Z축 반지름 (깊이의 절반)
                )
            )
//            .geometry(  // 첫 번째 지오메트리: 모든 평면에 텍스처 적용
//                0,                                      // 지오메트리 인덱스 0번
//                RenderableManager.PrimitiveType.TRIANGLES,  // 삼각형 primitive 타입
//                planeVertexBuffer,                      // 버텍스 데이터 (위치 + UV)
//                planeIndexBuffer,                       // 인덱스 데이터 (삼각형 순서)
//                0,                                      // 인덱스 시작 오프셋
//                indexBufferOffset,                      // 렌더링할 인덱스 개수 (모든 평면)
//            )
//            .material(0, textureMaterialInstance)  // 0번 지오메트리에 텍스처 머티리얼 적용
            .geometry(  // 두 번째 지오메트리: 수평 평면에만 그림자 적용
                1,                                          // 지오메트리 인덱스 1번
                RenderableManager.PrimitiveType.TRIANGLES,      // 삼각형 primitive 타입
                planeVertexBuffer,                          // 동일한 버텍스 데이터 사용
                planeIndexBuffer,                           // 동일한 인덱스 데이터 사용
                0,                                          // 인덱스 시작 오프셋
                // 그림자 적용 범위: 수직 평면이 있으면 그 전까지(수평 평면만), 없으면 모든 평면
                indexWithoutShadow ?: indexBufferOffset,   // 수직 평면 시작 지점 또는 전체 끝점
            )
            .material(1, shadowMaterial.defaultInstance)  // 1번 지오메트리에 그림자 머티리얼 적용
            .build(filament.engine, planeRenderable)  // 설정된 RenderableManager를 평면 엔티티에 적용
    }
}
