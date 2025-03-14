using UnityEngine;

namespace _01_Scripts.Third_Person_Controller
{
    // FootTrigger 클래스는 발이 닿는 트리거 역할을 하며, 발 착지 효과를 발생시킵니다.
    public class FootTrigger : MonoBehaviour
    {
        // 현재 충돌한 Collider를 저장합니다.
        Collider currCollider;
        // 현재 바닥(플로어) 데이터를 저장합니다.
        FloorStepData currFloorData;

        // 발 착지 효과를 담당하는 FootStepEffects 컴포넌트 참조
        FootStepEffects footStepEffects;
        
        private void Awake()
        {
            // 상위 객체에서 FootStepEffects 컴포넌트를 가져옵니다.
            footStepEffects = GetComponentInParent<FootStepEffects>();
            // 이 게임 오브젝트의 레이어를 "FootTrigger"로 설정합니다.
            this.gameObject.layer = LayerMask.NameToLayer("FootTrigger");
        }

        // OnTriggerEnter: 다른 Collider와 충돌 시 호출되는 함수
        private void OnTriggerEnter(Collider other)
        {
            // FootStepEffects 컴포넌트가 없으면 아무 동작도 하지 않습니다.
            if (footStepEffects == null)
                return;

            // overrideStepEffects 목록이 존재하는 경우, 발 착지 효과를 재정의할 데이터(FloorStepData)를 생성합니다.
            if (footStepEffects.OverrideStepEffects is { Count: > 0 })
            {
                // 이전 데이터가 없거나, 충돌 Collider가 변경되었거나, 이전 데이터에 Terrain 정보가 있는 경우 새로 생성합니다.
                if (currFloorData == null || currCollider != other || currFloorData.Terrain != null)
                {
                    currCollider = other;
                    currFloorData = new FloorStepData(transform, footStepEffects, other.transform, other, footStepEffects.OverrideType == StepEffectsOverrideType.Tag);
                }
            }

            // FootStepEffects 컴포넌트의 OnFootLand 함수를 호출하여 발 착지 효과(소리, 파티클)를 발생시킵니다.
            footStepEffects.OnFootLand(transform, currFloorData);
        }
    }

    // FloorStepData 클래스는 발 착지 시 바닥의 재질, 텍스처, 태그, Terrain 정보를 저장하는 데이터 클래스입니다.
    public class FloorStepData
    {
        // 바닥의 재질 이름 (읽기 전용)
        public readonly string MaterialName;
        // 바닥의 텍스처 이름 (읽기 전용)
        public readonly string TextureName;
        // 바닥의 태그 (읽기 전용)
        public readonly string Tag;
        // 바닥이 Terrain인 경우 해당 Terrain 참조 (읽기 전용)
        public readonly Terrain Terrain;

        // FootStepEffects 참조 (내부 사용)
        readonly FootStepEffects stepEffects;

        // 생성자: 발의 위치, FootStepEffects, 바닥 Transform, 충돌 Collider, 태그만 사용할지 여부(onlyTakeTag)를 입력받습니다.
        public FloorStepData(Transform footTransform, FootStepEffects stepEffects, Transform groundTransform, Collider floorCollider, bool onlyTakeTag = false)
        {
            // 충돌한 Collider의 태그를 저장합니다.
            Tag = floorCollider.tag;
            this.stepEffects = stepEffects;
            // 만약 오직 태그만 사용하도록 설정되었다면 나머지 정보를 가져오지 않고 리턴합니다.
            if (onlyTakeTag) return;

            // 바닥 Transform에 Renderer 컴포넌트가 있는지 확인합니다.
            var renderer1 = groundTransform.GetComponent<Renderer>();

            if (renderer1 != null && renderer1.material != null)
            {
                // Renderer가 있다면 첫 번째 재질을 사용합니다.
                var material1 = renderer1.materials[0];
                // Terrain은 사용하지 않으므로 null로 설정합니다.
                Terrain = null;
                // 재질 이름에서 뒤의 11글자를 잘라내고 소문자로 변환하여 MaterialName에 저장합니다.
                MaterialName = material1.name.Substring(0, material1.name.Length - 11).ToLower();
                // 재질의 메인 텍스처 이름을 TextureName에 저장합니다.
                TextureName = material1.mainTexture?.name;
            }
            else
            {
                // Renderer가 없으면 바닥 Transform에서 Terrain 컴포넌트를 가져옵니다.
                Terrain = groundTransform.GetComponent<Terrain>();

                if (Terrain != null)
                {
                    // Terrain이 있으면 발 위치를 기준으로 Terrain의 레이어(텍스처) 정보를 가져옵니다.
                    string terrainLayer = GetTerrainLayerAtPosition(footTransform);
                    MaterialName = terrainLayer.ToLower();
                    TextureName = terrainLayer.ToLower();
                }
            }
        }

        // GetTerrainLayerAtPosition: 발 Transform을 기준으로, Raycast를 통해 Terrain의 알파맵 정보를 읽어
        // 가장 영향력이 큰(가중치가 높은) Terrain Layer의 텍스처 이름을 반환합니다.
        string GetTerrainLayerAtPosition(Transform footTransform)
        {
            var footPos = footTransform.position;

            // 발 위치 위쪽에 약간 오프셋을 주어 아래 방향으로 Raycast 실행 (최대 0.5m)
            if (Physics.Raycast(footPos + Vector3.up * 0.1f, Vector3.down, out _, 0.5f, stepEffects.GroundLayer))
            {
                // TerrainData를 가져옵니다.
                TerrainData terrainData = Terrain.terrainData;

                // 발 위치를 Terrain의 로컬 좌표로 변환
                Vector3 terrainPosition = footPos - Terrain.transform.position;
                float relativeX = terrainPosition.x / terrainData.size.x;
                float relativeZ = terrainPosition.z / terrainData.size.z;

                // 알파맵(스플랫맵)에서 해당 위치의 인덱스를 계산합니다.
                int mapX = Mathf.FloorToInt(relativeX * terrainData.alphamapWidth);
                int mapZ = Mathf.FloorToInt(relativeZ * terrainData.alphamapHeight);

                // 해당 위치의 알파맵 데이터를 가져옵니다.
                float[,,] alphaMap = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

                // 가장 영향력이 큰 Terrain Layer의 인덱스를 찾습니다.
                int terrainLayer = 0;
                float maxWeight = 0;
                for (int i = 0; i < alphaMap.GetLength(2); i++)
                {
                    if (alphaMap[0, 0, i] > maxWeight)
                    {
                        maxWeight = alphaMap[0, 0, i];
                        terrainLayer = i;
                    }
                }

                // 해당 Terrain Layer의 diffuseTexture 이름을 반환합니다.
                string textureName = terrainData.terrainLayers[terrainLayer].diffuseTexture.name;
                // Debug.Log("Player is standing on Terrain Layer: " + textureName);

                return textureName;
            }

            // Raycast에 실패하면 빈 문자열을 반환합니다.
            return "";
        }
    }
}