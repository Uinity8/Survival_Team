using UnityEngine;

namespace Framework.Characters
{
    public class GroundChecker : MonoBehaviour
    {
        [Header("지면 체크")]
        [Tooltip("캐릭터가 지면에 닿아 있는지 여부 (CharacterController의 내장된 지면 체크 기능과는 별개)")]
        public bool Grounded = true;
        
        [Tooltip("지면 체크 시 오프셋 (불규칙한 지형에서 유용)")]
        public float GroundedOffset = -0.14f;
        
        [Tooltip("지면 체크에 사용될 원의 반지름 (CharacterController의 반지름과 일치해야 함)")]
        public float GroundedRadius = 0.28f;
        
        [Tooltip("캐릭터가 지면으로 인식할 레이어")] 
        public LayerMask GroundLayers;
        
        public bool GroundedCheck()
        {
            // 지면 체크를 위한 구체(Sphere) 위치 설정
            var spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
            
            return Grounded;
        }
        
        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f); // 지면일 때 초록색
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f); // 공중일 때 빨간색

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            // 지면 체크를 위한 구체(Gizmo) 표시
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }
    }
}