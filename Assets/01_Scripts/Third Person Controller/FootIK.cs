using _01_Scripts.System;
using UnityEngine;

namespace _01_Scripts.Third_Person_Controller
{
    // AnimatorParameters 클래스의 일부로서, 애니메이터 파라미터를 해시값으로 미리 계산하여 저장합니다.
    public static partial class AnimatorParameters
    {
        // 좌측 발 IK 관련 파라미터
        public static readonly int LeftFootIK = Animator.StringToHash("leftFootIK");
        // 우측 발 IK 관련 파라미터
        public static readonly int RightFootIK = Animator.StringToHash("rightFootIK");
    }

    /// <summary>
    /// FootIK 클래스
    /// 캐릭터의 발 IK를 조정하여 발이 지면과 자연스럽게 닿도록 하는 역할을 함.
    /// 애니메이터의 IK 콜백 함수(OnAnimatorIK)를 통해 매 프레임 발 위치 및 회전을 보정합니다.
    /// </summary>
    public class FootIK : MonoBehaviour
    {
        [Tooltip("발 IK를 활성화할지 여부")]
        public bool enableFootIK = true;
        [Tooltip("발 IK를 적용할 루트 Transform (일반적으로 캐릭터의 기준 Transform)")]
        public Transform root;
        [SerializeField, Tooltip("엉덩이(hip) IK 보간 속도. 값이 클수록 빠르게 보간됩니다.")]
        float hipIKSmooth = 5;

        [SerializeField, Tooltip("발이 지면에 닿을 때 적용할 오프셋 값")]
        float footOffset = .1f;
        [SerializeField, Tooltip("발 위치를 찾기 위한 Ray(구) 캐스트 높이")]
        float footRayHeight = .8f;
        [SerializeField, Tooltip("발의 각도 보정을 위한 제한 각도")]
        float footAngleLimit = 30;
        [SerializeField, Tooltip("지면 탐지를 위한 레이어 마스크")]
        LayerMask groundLayer = 1;
        
        [SerializeField, Tooltip("발이 걸칠 ledge(낭떠러지) 탐지를 위한 레이어 마스크")]
        LayerMask ledgeLayer = 0;

        /// <summary>
        /// 외부에서 발 IK 활성화 여부를 설정할 수 있는 속성
        /// </summary>
        public bool IkEnabled { get; set; }

        // 내부 보간용 변수. IK 보간의 현재 가중치를 저장합니다.
        float ikSmooth;

        // 컴포넌트 참조 변수들
        Animator animator;               // 캐릭터의 Animator 컴포넌트
        PlayerController playerController; // 캐릭터의 PlayerController 컴포넌트

        // 엉덩이 오프셋을 저장하는 변수 (보간된 값)
        float offs;
        // 이전 프레임의 위치를 저장하여 이동 변화량을 판단하는 데 사용
        Vector3 prevPos;

        /// <summary>
        /// Awake() : 초기화 단계에서 필요한 컴포넌트를 가져오고, 레이어 마스크 설정을 보완합니다.
        /// </summary>
        private void Awake()
        {
            // Animator 컴포넌트와 PlayerController 컴포넌트를 가져옴
            animator = GetComponent<Animator>();
            playerController = GetComponent<PlayerController>();

            // groundLayer에 ledgeLayer가 포함되어 있지 않다면 추가합니다.
            // (비트 연산을 통해 ledgeLayer가 groundLayer에 포함되어 있는지 검사)
            if (groundLayer != (groundLayer | (1 << ledgeLayer)))
                groundLayer += 1 << ledgeLayer;
        }

        /// <summary>
        /// OnAnimatorIK() : 애니메이터의 IK 콜백 함수.
        /// 매 프레임 IK 설정을 적용하기 위해 호출되며, enableFootIK이 활성화된 경우 SetFootIK()를 호출합니다.
        /// </summary>
        /// <param name="layerIndex">IK 레이어 인덱스 (필요에 따라 사용 가능)</param>
        private void OnAnimatorIK(int layerIndex)
        {
            if (enableFootIK)
                SetFootIK();
        }

        /// <summary>
        /// SetFootIK() : 발의 IK 위치 및 회전을 계산하여 적용하는 메서드.
        /// 캐릭터가 Locomotion 상태일 때만 발 IK를 적용하며, 땅과의 충돌 정보를 바탕으로 자연스러운 발 위치를 보정합니다.
        /// </summary>
        void SetFootIK()
        {
            // 캐릭터가 Locomotion(이동) 상태가 아니라면 발 IK를 적용하지 않음
            if (playerController.FocusedSystemState != SystemState.Locomotion)
                return;

            // IkEnabled가 true인 경우에만 발 위치 보정을 진행
            if (IkEnabled)
            {
                // 애니메이터에서 각 발과 엉덩이(Hips) Transform을 가져옴
                var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var hips = animator.GetBoneTransform(HumanBodyBones.Hips);

                // 주석 처리된 코드는 이동 중일 때 엉덩이 오프셋을 초기화하는 로직 (현재는 비활성화)
                //if (Vector3.Distance(prevPos, transform.position) > 0.001f)
                //{
                //    offs = Mathf.Lerp(offs, 0, Time.deltaTime * hipIKSmooth);
                //    root.localPosition = offs * hips.up;
                //    prevPos = transform.position;
                //    return;
                //}

                // 발의 위치를 찾기 위해 SphereCast를 사용하여 왼발과 오른발 각각 지면과의 충돌을 감지
                var leftFootHit = Physics.SphereCast(leftFoot.position + Vector3.up * footRayHeight / 2, 0.1f, Vector3.down, out RaycastHit leftHit, footRayHeight + footOffset, groundLayer);
                var rightFootHit = Physics.SphereCast(rightFoot.position + Vector3.up * footRayHeight / 2, 0.1f, Vector3.down, out RaycastHit rightHit, footRayHeight + footOffset, groundLayer);

                // 발이 지면에 닿았는지 여부에 따라 IK 가중치를 결정합니다.
                // 만약 충돌이 감지되고, 충돌한 지점의 Y값이 발 위치보다 충분히 높다면 가중치를 1로 설정,
                // 그렇지 않으면 기존 애니메이터 파라미터 값을 사용합니다.
                var leftFootIKWeight = (leftFootHit && leftHit.point.y > leftFoot.position.y - footOffset) ? 1 : animator.GetFloat(AnimatorParameters.LeftFootIK);
                var rightFootIKWeight = (rightFootHit && rightHit.point.y > rightFoot.position.y - footOffset) ? 1 : animator.GetFloat(AnimatorParameters.RightFootIK);

                // 감지된 지면 충돌 위치의 Y값을 기준으로 발의 Y 위치를 결정합니다.
                var leftFootPos = leftFootHit ? leftHit.point.y : leftFoot.position.y - footOffset;
                var rightFootPos = rightFootHit ? rightHit.point.y : rightFoot.position.y - footOffset;

                // 캐릭터의 기준 높이와 발 충돌 위치 간의 차이를 계산하여 엉덩이 오프셋 값을 도출함
                float leftOffset = leftFootPos - transform.position.y;
                float rightOffset = rightFootPos - transform.position.y;
                // Idle 상태에 따른 보정값: IdleType 파라미터가 낮으면 낮은 보정값(0.1), 아니면 1
                var idleVal = (animator.GetFloat(AnimatorParameters.IdleType) < 0.5f ? 0.1f : 1);
                // 두 발 중 낮은 값(더 많은 오프셋을 요구하는 쪽)을 선택하여 최종 오프셋에 idleVal을 곱함
                var offset = (leftOffset < rightOffset ? leftOffset : rightOffset) * idleVal;

                // 현재 offs 값을 hipIKSmooth 보간 속도로 업데이트
                offs = Mathf.Lerp(offs, offset, Time.deltaTime * hipIKSmooth);
                // hips와 root의 로컬 위치를 업데이트하여 엉덩이 위치를 보정
                Vector3 downDir = hips.up;
                hips.localPosition = offs * downDir;
                root.localPosition = offs * downDir;

                // Crouch 상태와 Idle 상태를 감지하여 추가 보정 여부 결정
                var isInCrouchIdle = animator.GetFloat(AnimatorParameters.CrouchType) > 0.5f && idleVal > 0.5f;
                // 웅크림 상태일 경우 footOffset에 약간의 추가 오프셋을 적용
                float _footOffset = isInCrouchIdle ? footOffset + 0.05f : footOffset;

                // 오른발이 지면에 충돌한 경우 오른발의 IK 위치 및 회전 보정
                if (rightFootHit)
                {
                    // 현재 오브젝트의 up 벡터와 충돌 면의 노말 벡터 간의 각도를 계산
                    var angle = Vector3.Angle(transform.up, rightHit.normal);
                    // footAngleLimit에 따라 노말 벡터를 보간(Slerp)하여 자연스러운 회전값 도출
                    var normal = Vector3.Slerp(transform.up, rightHit.normal, footAngleLimit / angle);
                    // 현재 회전에서 노말 보정을 적용한 회전값 계산
                    var rot = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
                    // 충돌 지점에 footOffset을 적용하여 최종 발 위치 계산 (엉덩이 오프셋 보정 포함)
                    var pos = rightHit.point + rightHit.normal * _footOffset - hips.localPosition;
                    // 애니메이터에 오른발의 IK 위치와 회전값을 설정
                    animator.SetIKPosition(AvatarIKGoal.RightFoot, pos);
                    animator.SetIKRotation(AvatarIKGoal.RightFoot, rot);
                }
                // 왼발이 지면에 충돌한 경우 왼발의 IK 위치 및 회전 보정 (오른발과 동일한 로직)
                if (leftFootHit)
                {
                    var angle = Vector3.Angle(transform.up, leftHit.normal);
                    var normal = Vector3.Slerp(transform.up, leftHit.normal, footAngleLimit / angle);
                    var rot = Quaternion.FromToRotation(transform.up, normal) * transform.rotation;
                    var pos = leftHit.point + leftHit.normal * _footOffset - hips.localPosition;
                    animator.SetIKPosition(AvatarIKGoal.LeftFoot, pos);
                    animator.SetIKRotation(AvatarIKGoal.LeftFoot, rot);
                }
                // ikSmooth 값을 천천히 증가시켜 IK 가중치 보간을 진행 (최대 1로 제한)
                ikSmooth = Mathf.Clamp01(ikSmooth + 0.1f * Time.deltaTime);
                // 이동 중이 아니거나 웅크린 상태가 아니라면 발 회전 가중치를 설정
                if (!isInCrouchIdle)
                {
                    // 이동량(MoveAmount) 파라미터에 따라 IK 회전 가중치를 보정하여 자연스러운 발 회전을 구현
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, leftFootIKWeight * 0.6f * (1 - animator.GetFloat(AnimatorParameters.MoveAmount) * 2f));
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, rightFootIKWeight * 0.6f * (1 - animator.GetFloat(AnimatorParameters.MoveAmount) * 2f));
                }
                // IK 위치 가중치 설정 (이동량에 따라 가중치 감소)
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootIKWeight * (1 - animator.GetFloat(AnimatorParameters.MoveAmount) * 2f));
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootIKWeight * (1 - animator.GetFloat(AnimatorParameters.MoveAmount) * 2f));

                // 현재 위치를 이전 위치 변수에 저장하여 다음 프레임과 비교
                prevPos = transform.position;
            }
            else 
            {
                // IkEnabled가 false인 경우, ikSmooth 값을 감소시키고 IK 가중치를 서서히 낮춤
                ikSmooth = Mathf.Clamp01(ikSmooth - 0.2f * Time.deltaTime);
                animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, ikSmooth * 0.6f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, ikSmooth * 0.6f);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, ikSmooth);
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, ikSmooth);
            }
        }

        /// <summary>
        /// Update() 메서드
        /// 매 프레임 실행되며, 캐릭터가 Locomotion 상태일 때 IK가 활성화되어 있으면 루트의 로컬 위치를 초기화합니다.
        /// 이는 발 IK 보정으로 인해 발생한 엉덩이 오프셋을 매 프레임 0으로 리셋하는 역할을 합니다.
        /// </summary>
        private void Update()
        {
            // 캐릭터의 현재 시스템 상태가 Locomotion이 아니면 아무 작업도 수행하지 않음
            if (playerController.FocusedSystemState != SystemState.Locomotion)
                return;
            // IK가 활성화되어 있으면 root Transform의 로컬 위치를 0으로 초기화
            if (IkEnabled)
                root.localPosition = Vector3.zero;
        }
    }
}