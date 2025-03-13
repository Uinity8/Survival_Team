using UnityEngine;

namespace _01_Scripts.Gameplay.Entity.Characters.Player.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class BasicRigidBodyPush : MonoBehaviour
    {
        // 밀 수 있는 레이어를 설정하기 위한 LayerMask
        public LayerMask pushLayers;

        // Rigidbody를 밀 수 있는지 여부를 결정하는 플래그
        public bool canPush;

        // 밀 때의 힘 크기를 조정할 수 있는 값 (0.5에서 5 사이)
        [Range(0.5f, 5f)] public float strength = 1.1f;

        // CharacterController가 Collider와 충돌했을 때 호출되는 메서드
        public void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // 밀기가 허용될 경우 충돌한 Rigidbody 처리
            if (canPush) PushRigidBodies(hit);
        }

        // Rigidbody를 미는 함수
        public void PushRigidBodies(ControllerColliderHit hit)
        {
            // 참고: https://docs.unity3d.com/ScriptReference/CharacterController.OnControllerColliderHit.html

            // 케이스 1: 충돌한 오브젝트에 kinematic이 아닌 Rigidbody가 있는지 확인
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return; // 없으면 종료

            // 케이스 2: 설정된 레이어에 해당하는 오브젝트만 밀도록 제한
            var bodyLayerMask = 1 << body.gameObject.layer;
            if ((bodyLayerMask & pushLayers.value) == 0) return; // 밀 수 없는 레이어일 경우 종료

            // 케이스 3: 캐릭터 아래쪽으로 있는 물체는 밀지 않음
            if (hit.moveDirection.y < -0.3f) return; // 아래쪽 방향 움직임은 처리하지 않음

            // 케이스 4: 수평 방향으로 밀기 위한 벡터 계산
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

            // 케이스 5: 힘의 크기를 strength에 맞게 적용하여 해당 방향으로 밀기
            body.AddForce(pushDir * strength, ForceMode.Impulse);
        }
    }
}