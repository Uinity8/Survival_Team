using System;
using System.Collections;
using System.Linq;
using _01_Scripts.Input;
using _01_Scripts.System;
using AnimatorHash;
using UnityEngine;

namespace AnimatorHash
{
    public static partial class AnimatorParameters
    {
        public static readonly int MoveAmount = Animator.StringToHash("moveAmount"); // 이동량
        public static readonly int StrafeAmount = Animator.StringToHash("strafeAmount"); // 좌우(횡이동) 움직임
        public static readonly int IsGrounded = Animator.StringToHash("IsGrounded"); // 땅에 닿아있는 상태
        public static readonly int FallAmount = Animator.StringToHash("fallAmount"); // 낙하 정도
        public static readonly int IdleType = Animator.StringToHash("idleType"); // 정지 상태 타입
        public static readonly int CrouchType = Animator.StringToHash("crouchType"); // 앉은 자세 타입
        public static readonly int Rotation = Animator.StringToHash("rotation"); // 캐릭터 회전
        public static readonly int TurnBackMirror = Animator.StringToHash("turnback Mirror"); // 180도 뒤돌기 방향
        public static readonly int RunToStopAmount = Animator.StringToHash("RunToStopAmount"); // 정지 동작(러닝에서 멈출 때)
        public static readonly int LocomotionType = Animator.StringToHash("locomotionType"); // 로코모션 타입
    }

}

namespace _01_Scripts.Third_Person_Controller
{

    public class LocomotionController : SystemBase, ICharacter, IDamageable
    {
        [Header("Movement Parameters")] [SerializeField]
        float sprintSpeed = 6.5f; // 스프린트 속도

        [SerializeField] float runSpeed = 4.5f; // 달리기 속도
        [SerializeField] float walkSpeed = 2f; // 걷기 속도
        [SerializeField] float rotationSpeed = 2.5f; // 회전 속도

        [field: Space(2)] public float acceleration = 8f; // 가속도
        public float deceleration = 6f; // 감속도

        [field: Space(2)] [Tooltip("스프린트를 비활성화합니다.")]
        public bool enableSprint = true; // 스프린트를 활성화/비활성화

        [Tooltip("달리기를 기본 상태로 설정합니다.")] public bool setDefaultStateToRunning = true; // 기본 상태를 달리기로 설정

        [field: Space(2)] [Header("Additional Features")]
        public bool useMultiDirectionalAnimation; // 다중 방향 애니메이션 사용 여부

        [Tooltip("0일 경우 캐릭터가 이동 방향을 봅니다. 1일 경우 카메라 방향을 봅니다.")] [Range(0, 1)]
        public float playerDirectionBlend = 1f; // 플레이어 방향 블렌드 (0: 이동 방향, 1: 카메라 방향)

        [Tooltip("참으로 설정하면 캐릭터가 공전 상태에서도 항상 카메라를 바라봅니다.")]
        public bool faceCameraForwardWhenIdle; // 아이들 상태에서도 카메라 방향을 항상 바라볼지 여부

        [Tooltip("다중 방향 애니메이션에 서로 다른 속도를 적용하려면 이 배율을 사용하세요.")]
        public float speedMultiplier = 0.8f; // 다중 방향 애니메이션에 사용할 속도 배율

        [Tooltip("플레이어가 전방도를 이 비율만큼 바라보고 있을 때만 스프린트합니다.")] [Range(0, 1)]
        public float sprintDirectionThreshold = 0.9f; // 스프린트 조건 (전방 방향 비율)

        [Range(0, 1)] public float forwardHipRotationBlend = 0.5f; // 전방 허리 회전 블렌드 비율
        public bool rotateHipForBackwardAnimation = true; // 후방 허리 회전을 위한 허리 회전 여부
        [Range(0, 1)] public float backwardHipRotationBlend = 0.8f; // 후방 허리 회전 블렌드 비율

        Vector3 currentSpeed; // 현재 속도

        public bool verticalJump; // 수직 점프 여부

        [Tooltip("캐릭터가 점프 정점에 도달하는 데 걸리는 시간을 정의합니다.")]
        public float timeToJump = 0.4f; // 점프 정점까지 도달 시간

        [Tooltip("플레이어가 공중에 있을 때의 이동 속도입니다.")] public float jumpMoveSpeed = 4f; // 공중에서의 이동 속도

        [Tooltip("플레이어가 공중에 있을 때의 이동 가속도입니다.")]
        public float jumpMoveAcceleration = 4f; // 공중에서의 이동 가속도

        float moveSpeed = 0; // 현재 이동 속도 값

        [field: Space(3)] [field: Tooltip("캐릭터가 낭떠러지에서 자동으로 이동을 멈춥니다.")] [SerializeField]
        bool preventFallingFromLedge = true; // 가장자리에서 떨어지지 않도록 방지

        [field: Tooltip("낭떠러지에서 활주 동작의 한계를 정의합니다.")] [Range(-1, 1)] [SerializeField]
        float slidingMovementThresholdFromLedge = -1f; // 가장자리 슬라이드 이동 한계

        [field: Tooltip("이동할 수 없는 경우, 낭떠러지에서의 캐릭터 회전을 방지합니다.")] [SerializeField]
        bool preventLedgeRotation = false; // 가장자리에서 회전 방지

        [field: Tooltip("로코모션 도중 벽 근처에서 캐릭터가 슬라이딩 되는 것을 방지합니다.")] [SerializeField]
        bool preventWallSlide = false; // 벽 근처에서 슬라이딩 방지

        [field: Tooltip("좁은 통로 위에서 균형을 잡고 걷는 기능을 활성화합니다.")] [field: Space(3)]
        public bool enableBalanceWalk = true; // 좁은 통로에서 균형 걷기 활성화

        public BalanceWalkDetectionType balanceWalkDetectionType = BalanceWalkDetectionType.Dynamic; // 균형 걷기 감지 방식

        public enum BalanceWalkDetectionType
        {
            Dynamic,
            Tagged,
            Both
        } // 균형 걷기 감지 유형

        [Header("Optional Animations")] [field: Tooltip("회전 애니메이션을 재생합니다.")]
        public bool enableTurningAnim = true; // 회전 애니메이션 사용 여부

        [field: Tooltip("빠른 회전 애니메이션을 재생합니다.")]
        public bool playQuickTurnAnimation = true; // 빠른 회전 애니메이션 사용 여부

        [field: Tooltip("이 moveAmount 값 이상에서만 빠른 회전 애니메이션을 재생합니다.")]
        public float QuickTurnThreshhold = -0.01f; // 빠른 회전을 위한 최소 이동량

        [field: Tooltip("빠른 정지 애니메이션을 재생합니다.")]
        public bool playQuickStopAnimation = false; // 빠른 정지 애니메이션 사용 여부

        [field: Tooltip("이 moveAmount 값 이상에서만 빠른 정지 애니메이션을 재생합니다.")]
        public float runToStopThreshhold = 0.4f; // 빠른 정지를 위한 최소 이동량

        [Header("Ground Check Settings")] [Tooltip("지면 체크 시 오프셋 (불규칙한 지형에서 유용)")] [SerializeField]
        float groundCheckOffset = -0.14f;

        [Tooltip("지면 체크에 사용될 원의 반지름 (CharacterController의 반지름과 일치해야 함)")] [SerializeField]
        float groundCheckRadius = 0.28f;


        [Tooltip("땅으로 간주되어야 할 모든 레이어")] public LayerMask groundLayer = 1; // 땅으로 간주되는 레이어들
        public LayerMask LedgeLayer = 0; // 가장자리 레이어


        float controllerDefaultHeight = .87f; // 기본 캐릭터 컨트롤러 높이
        float controllerDefaultYOffset = 1.7f; // 기본 캐릭터 컨트롤러 Y 오프셋

        bool isGrounded; // 캐릭터가 현재 지면에 닿아 있는지 여부를 나타냄

        Vector3 desiredMoveDir; // 캐릭터가 이동하려는 목표 방향
        Vector3 moveInput; // 사용자 입력에 따른 이동 값
        Vector3 moveDir; // 최종적으로 계산된 이동 방향
        float moveAmount; // 이동량 (0이면 정지, 1이면 최대 이동)
        Vector3 velocity; // 현재 이동 속도 벡터

        float ySpeed; // 캐릭터의 수직 속도
        Quaternion targetRotation; // 캐릭터의 목표 회전 방향

        float rotationValue = 0; // 회전 값 (좌우 방향)
        float crouchVal = 0; // 캐릭터가 앉을 때의 값
        float dynamicCrouchVal = 0; // 동적 앉기 값 (앉기 애니메이션 계산 값)
        float footOffset = .1f; // 캐릭터 발 위치의 오프셋
        float footRayHeight = .8f; // 캐릭터의 발 레이캐스트 높이

        bool turnBack; // 캐릭터가 180도 회전을 수행하고 있는지 여부
        bool useRootMotion; // 캐릭터의 애니메이션 루트 모션 사용 여부
        bool useRootmotionMovement; // 루트 모션에 따라 이동하는지 여부

        Vector3 prevAngle; // 이전 회전 값
        bool prevValue; // 이전 입력 값 상태를 저장

        float headIK; // 머리 IK 값 (상반신 방향 조정용)

        bool preventLocomotion; // 캐릭터의 이동을 멈추게 할지 여부

        float addedMomentum = 0f; // 이동 중 추가적인 모멘텀 값 (가속도/감속도 등)

        float jumpHeightDiff; // 현재 점프 높이와 지면의 차이
        float minJumpHeightForHardland = 3f; // 하드 랜딩을 위한 최소 점프 높이
        float jumpMaxPosY; // 캐릭터 점프 최대 높이 값

        float headHeightThreshold = .75f; // 머리 감지 높이 임계값
        float sprintModeTimer = 0; // 스프린트 모드 유지 시간

        // 땅 감지 영역의 오프셋 (Getter/Setter)
        public float GroundCheckOffset
        {
            get => groundCheckOffset; // 현재 땅 감지 오프셋 반환
            set => groundCheckOffset = value; // 새로운 땅 감지 오프셋 설정
        }

        // 땅 감지 구체의 반지름 값을 반환 (읽기 전용)
        public float GroundCheckRadius => groundCheckRadius;

        // 애니메이터의 MoveAmount 파라미터 값을 반환 (읽기 전용)
        public float MoveAmount => animator.GetFloat(AnimatorParameters.MoveAmount);

        // 현재 시스템 상태를 반환 (로코모션 상태)
        public override SystemState State => SystemState.Locomotion;

        // 캐릭터와 로코모션 관련 오브젝트 및 컴포넌트 참조
        FootIK footIk; // 발 IK 관리 컴포넌트
        PlayerController playerController; // 플레이어 컨트롤러
        GameObject cameraGameObject; // 카메라 오브젝트
        CharacterController characterController; // 캐릭터 컨트롤러 컴포넌트
        Animator animator; // 애니메이션을 제어하는 애니메이터
        EnvironmentScanner environmentScanner; // 주변 환경을 분석하는 스캐너
        LocomotionInputManager inputManager; // 움직임 입력을 처리하는 매니저

        private void Awake()
        {
            Debug.Log((Camera.main != null ? "Camera found" : "Camera not found"));
            if(Camera.main != null)
                cameraGameObject = Camera.main.gameObject;
                
            _walkSpeed = walkSpeed;
            _runSpeed = runSpeed;
            _sprintSpeed = sprintSpeed;
        }

        void Start()
        {
            playerController = GetComponent<PlayerController>();
            animator = GetComponent<Animator>();
            environmentScanner = GetComponent<EnvironmentScanner>();
            characterController = GetComponent<CharacterController>();
            inputManager = GetComponent<LocomotionInputManager>();
            controllerDefaultHeight = characterController.height;
            controllerDefaultYOffset = characterController.center.y;
            footIk = GetComponent<FootIK>();
            groundLayer |= LedgeLayer; //Ledge는 그라운드에 포함 되어야함
        }

        private void OnAnimatorIK(int layerIndex)
        {
            // 엉덩이(Hips) 위치의 트랜스폼 가져오기
            var hipPos = animator.GetBoneTransform(HumanBodyBones.Hips).transform;

            // 머리(Head) 위치의 현재 월드 좌표 가져오기
            var headPos = animator.GetBoneTransform(HumanBodyBones.Head).transform.position;

            // 엉덩이와 머리 사이 거리 계산 (Head와 Hips 간의 높이 차이)
            var offset = Vector3.Distance(hipPos.position, headPos);

            // LookAt 위치 설정 (카메라 방향 + 높이 오프셋 추가)
            animator.SetLookAtPosition(cameraGameObject.transform.position + cameraGameObject.transform.forward * (5f) +
                                       new Vector3(0, offset, 0));

            // 머리의 LookAtWeight 설정
            // animator.SetLookAtWeight(headIK);
        }


        public override void HandleFixedUpdate()
        {
            // 균형 걷기가 활성화되어 있는 경우
            if (enableBalanceWalk)
            {
                // 균형 감지 타입이 Dynamic인 경우
                if (balanceWalkDetectionType == BalanceWalkDetectionType.Dynamic)
                    HandleBalanceOnNarrowBeam(); // 동적으로 균형 처리

                // 균형 감지 타입이 Tagged인 경우
                else if (balanceWalkDetectionType == BalanceWalkDetectionType.Tagged)
                    HandleBalanceOnNarrowBeamWithTag(); // 특정 태그 기반으로 균형 처리

                // 균형 감지 타입이 Both인 경우
                else if (balanceWalkDetectionType == BalanceWalkDetectionType.Both)
                {
                    // 동적 앉기가 0.5 미만이면 태그 기반 처리
                    if (dynamicCrouchVal < 0.5f)
                        HandleBalanceOnNarrowBeamWithTag();

                    // 앉기 값이 0.5 미만이거나 동적 앉기가 0.5 이상이면 균형 처리 실행
                    if (crouchVal < 0.5f || dynamicCrouchVal > 0.5f)
                    {
                        HandleBalanceOnNarrowBeam(); // 동적 균형 처리
                        dynamicCrouchVal = crouchVal; // dynamicCrouchVal 업데이트
                    }
                }
            }
        }


        public override void HandleUpdate()
        {
            // 이동이 제한되었거나 루트 모션이 활성화된 경우, ySpeed를 줄이고 종료
            if (preventLocomotion || UseRootMotion)
            {
                ySpeed = Gravity / 4; // 중력만 적용 하고 리턴
                return;
            }

            // 애니메이터에게 이동 타입 전달 (다방향 애니메이션 사용 여부)
            animator.SetFloat(AnimatorParameters.LocomotionType, useMultiDirectionalAnimation ? 1 : 0);

            // 이전 지상 상태 저장
            var wasGroundedPreviously = isGrounded;

            // 현재 지상 상태 확인
            GroundCheck();

            // 땅에 닿았으며 이전에 공중에 있었다면 착지 처리
            if (isGrounded && !wasGroundedPreviously)
            {
                if (ySpeed < Gravity) // 착지 속도가 일정 값 이상이라면 착지 처리 진행 << 수정해야할듯
                {
                    playerController.OnLand?.Invoke(
                        Mathf.Clamp(Mathf.Abs(ySpeed) * 0.0007f, 0.0f,
                            0.01f), //카메라 쉐이크(amount, duration) << SignalManager로 변경예정
                        1f);
                    animator.SetFloat(AnimatorParameters.FallAmount,
                        Mathf.Clamp(Mathf.Abs(ySpeed) * 0.06f, 0.6f, 1f)); // 애니메이션 변수 설정
                    StartCoroutine(DoLocomotionAction("Landing", true)); // 착지 애니메이션 재생
                }
                else
                {
                    animator.SetFloat(AnimatorParameters.FallAmount, 0); // 착지 크기 0으로 설정
                }
            }

            // 속도 초기화
            velocity = Vector3.zero;

            if (isGrounded) // 캐릭터가 지면에 있을 경우
            {
                ySpeed = Gravity / 2; // ySpeed를 중력 기반으로 절반 설정
                //footIk.IkEnabled = true; // (주석된 부분: 발 IK 처리 활성화)

                // 기본 상태가 달리기로 설정되었는지 확인하고 변경
                setDefaultStateToRunning =
                    inputManager.ToggleRun ? !setDefaultStateToRunning : setDefaultStateToRunning;
                
                // 속도 값을 설정 (달리기/걷기 여부)
                float targetSpeed = setDefaultStateToRunning ? runSpeed : walkSpeed;
                
                if(inputManager.SprintKey || sprintModeTimer > 2f)
                    targetSpeed = enableSprint ? sprintSpeed : runSpeed;

                // 속도 계산 (걷기, 달리기, 스프린트)
                moveSpeed = targetSpeed;

                var curSpeedDir = currentSpeed; //현재 이동방향은 현재 스피드?
                curSpeedDir.y = 0; //수직값은 제거

                var sprintDir = Vector3.Dot(curSpeedDir.normalized, transform.forward); // 앞쪽 방향과 얼마나 일치하는지 
                sprintDir = sprintDir > sprintDirectionThreshold
                    ? sprintDir
                    : 0; //정해지 기준(기본값 0.9) 보다 일치하면 sprintDir유지 아니면 초기화

                moveSpeed = Mathf.Lerp(moveSpeed, targetSpeed, Mathf.Clamp01(sprintDir));


                var currentRunSpeed = runSpeed; //현재 달리기 속도는 뛰기 속도

                // 앉기 값(crouchVal)에 따른 이동 속도 감소
                if (Mathf.Approximately(crouchVal, 1)) 
                    moveSpeed *= .6f; // 앉아있을 때 60% 속도
                else if (useMultiDirectionalAnimation)
                {
                    moveSpeed *= speedMultiplier; // 다방향 애니메이션 사용 중일 경우 속도 가중치 적용(기본값 0.8)
                    currentRunSpeed *= speedMultiplier;
                }

                // 애니메이터 값을 갱신
                animator.SetFloat(AnimatorParameters.IdleType, crouchVal, 0.5f, Time.deltaTime);

                // 목표 방향을 따라 속도 갱신
                velocity = desiredMoveDir * moveSpeed;

                // 회전 애니메이션 처리
                if (enableTurningAnim)
                    HandleTurning();

                // 특정 조건에서 캐릭터가 아래로 점프 처리
                if (inputManager.Drop && MoveDir != Vector3.zero && !preventLocomotion && IsGrounded && IsOnLedge)
                {
                    var hitData = environmentScanner.ObstacleCheck(performHeightCheck: false);
                    if (!hitData.ForwardHitFound && !Physics.Raycast(transform.position + Vector3.up * 0.1f,
                            transform.forward, 0.5f, environmentScanner.ObstacleLayer))
                    {
                        StartCoroutine(DoLocomotionAction("Jump Down", _useRootMotionMovement: true,
                            _targetRotation: Quaternion.LookRotation(MoveDir)));
                        IsOnLedge = false;
                        animator.SetBool(AnimatorParameters.IsGrounded, isGrounded = false);
                        return;
                    }
                }

                //  velocity > 0 ? 가속 : 감속
                currentSpeed = Vector3.MoveTowards(currentSpeed, velocity,
                    (velocity != Vector3.zero ? acceleration : deceleration) * Time.deltaTime);

                // 캐릭터 실제 이동 속도 계산
                var characterVelocity = characterController.velocity;
                characterVelocity.y = 0;

                //전방속도
                float forwardSpeed = Vector3.Dot(characterVelocity, transform.forward);
                animator.SetFloat(AnimatorParameters.MoveAmount, forwardSpeed / currentRunSpeed, 0.2f, Time.deltaTime);

                //좌위 속도
                float strafeSpeed = Vector3.Dot(characterVelocity, transform.right);
                animator.SetFloat(AnimatorParameters.StrafeAmount, strafeSpeed / currentRunSpeed, 0.2f, Time.deltaTime);

                // 급정지 애니메이션 재생
                if (playQuickStopAnimation && ((MoveAmount > runToStopThreshhold && velocity == Vector3.zero) ||
                                               (forwardSpeed / currentRunSpeed < 0.1f &&
                                                MoveAmount > runToStopThreshhold)) &&
                    animator.GetFloat(AnimatorParameters.IdleType) < 0.2f)
                {
                    currentSpeed = Vector3.zero;
                    animator.SetBool(AnimatorParameters.TurnBackMirror, strafeSpeed > 0.03f);
                    animator.SetFloat(AnimatorParameters.RunToStopAmount, MoveAmount);
                    StartCoroutine(DoLocomotionAction("Run To Stop", _useRootMotionMovement: false, crossFadeTime: 0.3f,
                        onComplete: () => { animator.SetFloat(AnimatorParameters.MoveAmount, 0); }));
                }
                else if (playQuickTurnAnimation)
                {
                    TurnBack();
                }
            }
            else // 공중 상태일 때
            {
                ySpeed = Mathf.Clamp(ySpeed + Gravity * Time.deltaTime, Gravity,
                    Mathf.Abs(Gravity) * timeToJump); // ySpeed 조정
            }

            velocity.y = ySpeed;

            // 캐릭터 컨트롤러를 이동
            characterController.Move(velocity * Time.deltaTime);

            currentSpeed.y = 0;

            // 캐릭터 회전 처리
            if (!playerController.PreventRotation)
            {
                SetTargetRotation(moveDir, ref targetRotation);
                float turnSpeed = Mathf.Lerp(rotationSpeed * 100f, 2 * rotationSpeed * 100f, moveSpeed / runSpeed);
                transform.rotation =
                    Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
            }
            else
                targetRotation = transform.rotation;
        }

        void SetTargetRotation(Vector3 direction, ref Quaternion rotation)
        {
            // 카메라의 전방 벡터를 가져오되 수직(y) 방향은 제거하여 수평 평면 상의 전방만 사용
            var cameraForward = cameraGameObject.transform.forward;
            cameraForward.y = 0;

            // 기본 dotProd 값을 1로 설정
            var dotProd = 1f;

            // 다방향 애니메이션과 후방 방향 애니메이션 회전이 활성화된 경우, dotProd를 설정
            if (useMultiDirectionalAnimation && rotateHipForBackwardAnimation)
                dotProd = Vector3.Dot(cameraForward, direction) + 0.35f;

            // 플레이어가 움직이고 있는 경우 (방향 벡터 크기가 0보다 클 때)
            if (direction.magnitude > 0f)
            {
                // 다방향 애니메이션을 사용하지 않을 경우 단순히 캐릭터를 움직이는 방향으로 회전
                if (!useMultiDirectionalAnimation)
                {
                    rotation = Quaternion.LookRotation(direction);
                    return;
                }

                // dotProd 또는 IdleType 값에 따라 플레이어 회전 설정
                if (dotProd > 0 || animator.GetFloat(AnimatorParameters.IdleType) > 0.5f)
                {
                    // 방향 벡터와 카메라 전방 벡터를 혼합하여 회전값 계산
                    rotation = Quaternion.LookRotation(Vector3.Lerp(direction, cameraForward,
                        playerDirectionBlend - animator.GetFloat(AnimatorParameters.IdleType)));
                }
                else
                {
                    // 조건에 따라 반대 방향(-direction)으로 회전하며, 혼합 값 설정
                    rotation = Quaternion.LookRotation(Vector3.Lerp(-direction, cameraForward,
                        playerDirectionBlend - backwardHipRotationBlend -
                        animator.GetFloat(AnimatorParameters.IdleType)));
                }

                // 현재 회전 값과 이동 방향의 내적(dot) 값 계산
                var playerMoveDirDot = Vector3.Dot(rotation * Vector3.forward, direction.normalized) -
                                       (1 - forwardHipRotationBlend);

                // 회전 방향을 다시 계산하여 최종 회전값을 설정
                rotation = Quaternion.LookRotation(Vector3.Lerp(rotation * Vector3.forward, direction,
                    playerMoveDirDot - animator.GetFloat(AnimatorParameters.IdleType)));
            }
            // 방향 벡터가 0이며 플레이어가 정지 상태일 경우
            else if (faceCameraForwardWhenIdle)
            {
                // 플레이어를 카메라가 보는 방향으로 회전
                rotation = Quaternion.LookRotation(cameraForward);
            }
        }

        void HandleBalanceOnNarrowBeam()
        {
            int hitCount = 0; // 지면과의 충돌 횟수를 계산

            // 방향 벡터 설정: 오른쪽, 전방, 위쪽 방향
            Vector3 right = transform.right * 0.3f, forward = transform.forward * 0.3f, up = Vector3.up * 0.2f;

            // 전방 후방 검사
            hitCount += Physics.CheckCapsule(transform.position + forward + up,
                transform.position + forward - up,
                0.1f, groundLayer) ? 1 : 0;
            hitCount +=  Physics.CheckCapsule(transform.position - forward + up,
                transform.position - forward - up,
                0.1f, groundLayer)? 1 : 0;
            

            // 캡슐 충돌 체크: 발 앞쪽(왼쪽 발, 오른쪽 발) 검사
            bool rightFootHit = Physics.CheckCapsule(transform.position - right + up, transform.position - right - up, 0.1f,
                groundLayer);
            bool leftFootHit = Physics.CheckCapsule(transform.position + right + up, transform.position + right - up, 0.1f,
                groundLayer);

            // 발이 지면에 닿았는지 여부에 따라 hitCount 증가
            hitCount += rightFootHit ? 1 : 0;
            hitCount += leftFootHit ? 1 : 0;

            // 예측 점프를 처리하기 위한 특수 조건 적용
            if ((rightFootHit || leftFootHit) &&
                !Physics.Linecast(transform.position + up, transform.position - up,
                    groundLayer)) // 위에서 아래로의 투사선(Linecast) 확인
            {
                hitCount -= 1; // 특정 조건에서 충돌 횟수를 감소
            }

            // hitCount에 따라 `crouchVal` 값 결정: 3개 이상의 충돌이 있으면 서있는 상태(0), 그렇지 않으면 구부리기 상태(1)
            crouchVal = hitCount > 2 ? 0f : 1f;

            // 애니메이터에 "IdleType" 값 설정 (캐릭터의 구부리기 상태 전달)
            animator.SetFloat(AnimatorParameters.IdleType, crouchVal, 0.2f, Time.deltaTime);

            // 구부리기 값(crouchVal)이 0.2 이상이면 추가 처리 진행
            if (animator.GetFloat(AnimatorParameters.IdleType) > .2f)
            {
                // 양 발이 지면에 닿고 있는지 여부로 상태 결정
                var hasSpace = leftFootHit && rightFootHit;
                animator.SetFloat(AnimatorParameters.CrouchType, hasSpace ? 0 : 1, 0.2f,
                    Time.deltaTime); // CrouchType 값 설정
            }

            // 캐릭터 컨트롤러의 중심점 및 높이를 crouchVal 값에 따라 조정
            characterController.center = new Vector3(
                characterController.center.x,
                Mathf.Approximately(crouchVal, 1) ? controllerDefaultYOffset * .7f : controllerDefaultYOffset,
                characterController.center.z
            );

            characterController.height = Mathf.Approximately(crouchVal, 1)
                ? controllerDefaultHeight * .7f // 구부린 상태: 컨트롤러 높이를 줄임
                : controllerDefaultHeight; // 서있는 상태: 기본 높이로 설정
            
        }
        
        void HandleBalanceOnNarrowBeamWithTag()
        {
            // 플레이어 주변(.2f 반지름 구형 영역)에서 특정 태그("NarrowBeam" 또는 "SwingableLedge")를 가진 오브젝트 탐지
            var hitObjects = Physics.OverlapSphere(
                transform.TransformPoint(new Vector3(0f, 0.15f, 0.07f)), // 플레이어의 약간 앞쪽 위치를 기준으로
                .2f // 구의 반지름
            ).ToList().Where(g =>
                g.gameObject.CompareTag("NarrowBeam") || g.gameObject.CompareTag("SwingableLedge")
            ).ToArray();

            // 탐지된 오브젝트가 있으면 crouchVal을 1로 설정 (구부린 상태), 없으면 0으로 설정 (서 있는 상태)
            crouchVal = hitObjects.Length > 0 ? 1f : 0;

            // 애니메이터에 IdleType 값을 갱신하여 플레이어 상태 반영
            animator.SetFloat(AnimatorParameters.IdleType, crouchVal, 0.2f, Time.deltaTime);

            // 구부린 상태(crouchVal > 0.2f)일 경우 추가 처리
            if (animator.GetFloat(AnimatorParameters.IdleType) > .2f)
            {
                // 왼쪽 발이 지면에 닿아있는지 확인 (SphereCast 사용)
                var leftFootHit = Physics.SphereCast(
                    transform.position - transform.forward * 0.3f + Vector3.up * footRayHeight / 2, // 왼발 위치
                    0.1f, // 구의 반지름
                    Vector3.down, // 아래로 검사
                    out _, // 충돌 정보는 사용하지 않음
                    footRayHeight + footOffset, // 레이가 닿을 최대 거리
                    groundLayer // 지면 레이어
                );

                // 오른쪽 발이 지면에 닿아있는지 확인 (SphereCast 사용)
                var rightFootHit = Physics.SphereCast(
                    transform.position + transform.forward * 0.3f + Vector3.up * footRayHeight / 2, // 오른발 위치
                    0.1f,
                    Vector3.down,
                    out _,
                    footRayHeight + footOffset,
                    groundLayer
                );

                // 양발이 모두 닿아있는지 검사하여 CrouchType 애니메이터 값 설정
                var hasSpace = leftFootHit && rightFootHit;
                animator.SetFloat(AnimatorParameters.CrouchType, hasSpace ? 0 : 1, 0.2f, Time.deltaTime);
            }

            // 캐릭터 컨트롤러의 중심(center)와 높이(height)를 crouchVal 값에 따라 조정
            characterController.center = new Vector3(
                characterController.center.x,
                Mathf.Approximately(crouchVal, 1)
                    ? controllerDefaultYOffset * .7f
                    : controllerDefaultYOffset, // 구부린 상태라면 중심을 낮춤
                characterController.center.z
            );

            characterController.height = Mathf.Approximately(crouchVal, 1)
                ? controllerDefaultHeight * .7f // 구부린 상태라면 높이를 줄임
                : controllerDefaultHeight; // 서 있는 상태라면 기본 높이
        }

        void HandleTurning()
        {
            // 캐릭터의 현재 회전 값과 이전 회전 값의 차이를 계산
            var rotDiff = transform.eulerAngles - prevAngle;

            // 회전 속도에 따라 회전 민감도를 설정 (달리기 상태일 경우 더 작은 민감도 사용)
            var threshold = moveSpeed >= runSpeed ? 0.025 : 0.1;

            // 회전 차이가 민감도(threshold)보다 작으면 회전 중이 아님
            if (rotDiff.sqrMagnitude < threshold)
            {
                rotationValue = 0; // 회전 값을 0으로 설정
            }
            else
            {
                // 회전 차이가 민감도를 넘을 경우, 회전 방향을 계산하여 회전 값 설정
                // Mathf.Sign(rotDiff.y)는 회전 차이에 따라 양수 또는 음수를 반환 (회전 방향 구분)
                rotationValue = Mathf.Sign(rotDiff.y) * .5f;
            }

            // 애니메이션 컨트롤러에 회전 값을 전달
            animator.SetFloat(AnimatorParameters.Rotation, rotationValue, 0.35f, Time.deltaTime);

            // 현재 회전 값을 prevAngle로 저장하여 이후 비교에 사용
            prevAngle = transform.eulerAngles;
        }

        bool isTurning = false;

        public void GetInputFromInputManager(Vector2 input)
        {
            // 입력 값(DirectionInput)을 가져와 x축(좌/우)과 y축(앞/뒤) 값을 각각 저장
            float h = input.x; // 수평 입력
            float v = input.y; // 수직 입력

            // 입력된 방향의 벡터를 생성 및 저장 (y 값은 0으로 고정)
            moveInput = new Vector3(h, 0, v);

            // 입력의 크기를 계산하여 moveAmount에 저장 (0에서 1 사이 값)
            moveAmount = moveInput.magnitude;

            // 카메라의 평면 회전 정보를 획득하여 입력된 방향 벡터(desiredMoveDir)에 적용
            // 이 계산은 플레이어의 입력 방향을 카메라 방향에 맞게 변환
            desiredMoveDir = playerController.CameraPlanarRotation * moveInput;

            // 입력된 방향의 벡터 길이를 1로 제한하여 정규화된 값을 유지 (Vector3.ClampMagnitude)
            // Normalize를 하지 않는 이유는 콘솔입력을 위함
            desiredMoveDir = Vector3.ClampMagnitude(desiredMoveDir, 1);

            // 캐릭터가 이동할 방향 벡터를 최종적으로 moveDir에 저장
            moveDir = desiredMoveDir;


            // 모바일 플랫폼(Android 또는 iOS)에서만 실행
#if UNITY_ANDROID || UNITY_IOS
    // 기본 상태를 달리기 상태로 설정한 경우
    if (setDefaultStateToRunning)
    {
        // moveAmount가 1(최대 이동량)일 경우 스프린트 모드 타이머 증가
        if (moveAmount == 1)
            sprintModeTimer += Time.deltaTime; // 경과 시간을 누적
        else
            sprintModeTimer = 0; // 이동량이 1이 아닐 경우 타이머 초기화
    }
#endif
        }

        Quaternion velocityRotation;

        bool TurnBack()
        {
            // 움직임 입력이나 목표 방향이 없으면 되돌아보지 않음
            if (moveInput == Vector3.zero || desiredMoveDir == Vector3.zero)
                return false;

            // 목표 방향(desiredMoveDir)에 기반해 목표 회전값(velocityRotation)을 설정
            SetTargetRotation(desiredMoveDir, ref velocityRotation);

            // 현재 캐릭터의 앞 방향(transform.forward)과 목표 회전 방향 간의 각도 계산
            var angle = Vector3.SignedAngle(transform.forward, velocityRotation * Vector3.forward, Vector3.up);

            // "되돌아보기(Turnback)" 조건 체크
            if (
                Mathf.Abs(angle) > 130 && // 130도 이상 뒤 방향일 경우
                MoveAmount > QuickTurnThreshhold && // 플레이어가 일정 속도로 움직이고 있을 경우
                animator.GetFloat(AnimatorParameters.IdleType) < 0.2f && // 플레이어가 서있지 않을 경우
                Physics.Raycast(
                    transform.position + Vector3.up * 0.1f + transform.forward * 0.3f +
                    transform.forward * MoveAmount / 1.5f, // 발 바로 앞을 검사
                    Vector3.down, 0.3f) && // 발 밑에 장애물이 있는지 확인
                !Physics.Raycast(transform.position + Vector3.up * 0.1f, // 앞에 장애물이 없는지 확인
                    transform.forward, 0.6f))
            {
                // 되돌아보기 활성화 플래그 설정
                turnBack = true;

                // 회전 방향이 왼쪽이면 TurnBackMirror 플래그를 설정
                animator.SetBool(AnimatorParameters.TurnBackMirror, angle <= 0);

                // 현재 애니메이터 상태가 "Locomotion"인지 확인
                bool isInLocomotionBlendTree = animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion");

                // 애니메이션 "Running Turn 180" 재생
                StartCoroutine(DoLocomotionAction("Running Turn 180", onComplete: () =>
                    {
                        // 애니메이션 실행 후 현재 플레이어의 속도와 이동 적용
                        currentSpeed = transform.forward * (runSpeed * (MoveAmount));
                        characterController.Move(currentSpeed * Time.deltaTime);

                        // 목표 회전값 갱신
                        targetRotation = transform.rotation;

                        // 되돌아보기 플래그 해제
                        turnBack = false;
                    }, crossFadeTime: isInLocomotionBlendTree ? 0.08f : 0.2f, // 애니메이션 전환 시간 결정
                    setMoveAmount: true));

                return true; // 되돌아보기를 성공적으로 실행
            }

            return false; // 되돌아보기가 실행되지 않음
        }

        void GroundCheck()
        {
            var spherePosition = new Vector3(transform.position.x, transform.position.y - groundCheckOffset,
                transform.position.z);
            isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayer,
                QueryTriggerInteraction.Ignore);

            // isGrounded = Physics.CheckSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius,
            //     groundLayer);
            animator.SetBool(AnimatorParameters.IsGrounded, isGrounded);
        }


        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f); // 지면일 때 초록색
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f); // 공중일 때 빨간색

            Gizmos.color = isGrounded ? transparentGreen : transparentRed;

            // 지면 체크를 위한 구체(Gizmo) 표시
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - groundCheckOffset,
                    transform.position.z), groundCheckRadius);

            // Gizmos.DrawSphere(transform.TransformPoint(groundCheckOffset), groundCheckRadius);
        }


        public void HandleTurningAnimation(bool enable)
        {
            enableTurningAnim = enable;
            if (!enable)
                animator.SetFloat(AnimatorParameters.Rotation, 0);
        }

        public IEnumerator DoLocomotionAction(string anim, bool _useRootMotionMovement = false,
            Action onComplete = null, float crossFadeTime = .2f, Quaternion? _targetRotation = null,
            bool setMoveAmount = false)
        {
            // Locomotion 동작(애니메이션)이 시작되었음을 알리기 위해 locomotion을 일시적으로 비활성화
            preventLocomotion = true;

            // Root Motion 사용 여부 설정 (Root Motion: 애니메이션에서 직접 이동 데이터를 가져오는 것)
            this.useRootmotionMovement = _useRootMotionMovement;
            EnableRootMotion(); // Root Motion 활성화

            // 애니메이션 시작 (CrossFade를 통해 지정된 기간(crossFadeTime) 동안 부드럽게 새 애니메이션으로 전환)
            animator.CrossFade(anim, crossFadeTime);

            yield return null; // 한 프레임 대기 (애니메이터가 상태를 업데이트할 시간을 줌)

            // 다음 애니메이션 상태 가져오기
            var animState = animator.GetNextAnimatorStateInfo(0);

            float timer = 0f; // 애니메이션 지속 시간 추적
            while (timer <= animState.length) // 현재 애니메이션 상태의 길이 동안 루프 실행
            {
                // Turnback 플래그가 비활성화되어 있고 새 Turnback 조건이 만족되면 Locomotion 종료
                if (!turnBack && TurnBack()) yield break;

                // MoveAmount 값을 애니메이터에 업데이트 (속도를 유지하거나 런닝 상태로 전환)
                if (setMoveAmount)
                    animator.SetFloat(AnimatorParameters.MoveAmount,
                        moveAmount * (setDefaultStateToRunning ? 1f : 0.5f), // 런닝 여부에 따라 가중치 부여
                        animState.length * 1.3f, // 애니메이션 길이에 기반하여 값 설정
                        1f * Time.deltaTime); // 스무스한 전환(Time.deltaTime 가중)

                // 특정 목표 회전값(_targetRotation)이 지정된 경우, 해당 각도로 회전
                if (_targetRotation.HasValue && !playerController.PreventRotation)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, // 현재 회전값
                        _targetRotation.Value, // 목표 회전값
                        500f * Time.deltaTime); // 회전 속도 (초당 500도)

                timer += Time.deltaTime; // 애니메이션 시간 누적
                yield return null; // 한 프레임 대기
            }

            // 애니메이션 종료 후 Root Motion 비활성화
            DisableRootMotion();
            this.useRootmotionMovement = false;

            // 완료 콜백(onComplete)이 지정된 경우 실행
            onComplete?.Invoke();

            // Locomotion 종료 플래그 해제
            preventLocomotion = false;
        }

        public void EnableRootMotion()
        {
            // 현재 Root Motion의 상태를 prevValue에 저장한 후, Root Motion을 활성화
            prevValue = useRootMotion;
            useRootMotion = true;
        }

        public void DisableRootMotion()
        {
            // 현재 Root Motion의 상태를 prevValue에 저장한 후, Root Motion을 비활성화
            prevValue = useRootMotion;
            useRootMotion = false;
        }

        void OnAnimatorMove()
        {
            // Root Motion이 활성화되어 있을 경우 애니메이터의 이동 및 회전 값을 적용
            if (useRootMotion)
            {
                // Animator가 계산한 현재 프레임의 회전 값(deltaRotation)을 transform 회전에 추가
                transform.rotation *= animator.deltaRotation;

                // Root Motion 이동 데이터를 transform 위치에 적용 (useRootmotionMovement가 활성화된 경우)
                if (useRootmotionMovement)
                    transform.position += animator.deltaPosition;
            }
        }

        public IEnumerator TweenVal(float start, float end, float duration, Action<float> onLerp)
        {
            float timer = 0f; // 시간 카운터 초기화
            float percent = timer / duration; // 진행 비율 계산

            while (percent <= 1f) // 진행 비율이 1(100%)에 도달할 때까지 반복
            {
                timer += Time.deltaTime; // 매 프레임 시간 증가
                percent = timer / duration; // 누적 시간에 기반한 진행 비율 계산
                var lerpVal = Mathf.Lerp(start, end, percent); // 시작 값(start)과 종료 값(end) 사이를 보간
                onLerp?.Invoke(lerpVal); // 보간된 값을 onLerp 콜백을 통해 전달 (값 갱신)

                yield return null; // 다음 프레임까지 대기
            }
        }

        public void VerticalJump()
        {
            // 점프를 수행할 조건 검사: verticalJump가 활성화되어 있어야 하며, 땅에 닿아 있어야(IsGrounded) 함
            if (!verticalJump || !IsGrounded) return;

            // 머리 위에 장애물이 있는지 확인(SphereCast 사용)
            var headHit = Physics.SphereCast(
                animator.GetBoneTransform(HumanBodyBones.Head).position, // 머리의 위치
                .15f, // SphereCast의 반지름
                Vector3.up, // 위쪽 방향 조건
                out _,
                headHeightThreshold, // 최대 높이
                environmentScanner.ObstacleLayer // 검사할 충돌 레이어
            );

            // 머리에 장애물이 없을 경우 수직 점프 코루틴 시작
            if (!headHit)
                StartCoroutine(HandleVerticalJump());
        }

        public IEnumerator HandleVerticalJump()
        {
            yield return new WaitForFixedUpdate(); // 물리 연산 후 한 프레임 대기

            // 현재 플레이어 상태가 이 함수의 조건(State)와 다를 경우 점프 실행 중단
            if (playerController.CurrentSystemState != State)
                yield break;

            // 초기 점프 상태 설정
            jumpMaxPosY = transform.position.y - 1; // 최대 점프 높이 추적 변수 초기화
            var velocity = Vector3.zero; // 이동 속도 초기화
            var velocityY = Mathf.Abs(Gravity) * timeToJump; // 중력과 점프 시간을 기반으로 초기 수직 속도를 계산
            preventLocomotion = true; // 점프 중에는 Locomotion 제어 방지
            currentSpeed *= 0.1f; // 현재 속도 감소

            // 애니메이션 시작 (Vertical Jump로 교체)
            isGrounded = false;
            animator.CrossFadeInFixedTime("Vertical Jump", .2f);

            yield return new WaitForSeconds(0.1f); // 애니메이션 실행 후 잠시 대기

            // 캐릭터 초기 속도 가져오기
            var characterVelocity = characterController.velocity;

            while (!isGrounded) // 땅에 닿을 때까지 반복 (점프 루프)
            {
                // 공중 상태 처리
                playerController.IsInAir = true;

                // 중력을 점진적으로 적용하여 수직 속도 갱신
                velocityY += Gravity * Time.deltaTime;

                // 목표 이동 속도 계산 (점프 이동 속도를 적용)
                velocity = new Vector3(
                    (moveDir * jumpMoveSpeed).x, // 이동 방향 x축 속도
                    characterController.velocity.y, // 현재 y축 속도
                    (moveDir * jumpMoveSpeed).z // 이동 방향 z축 속도
                );

                // 현재 속도를 목표 속도로 스무스하게 변경
                characterVelocity = Vector3.MoveTowards(
                    characterVelocity,
                    velocity,
                    jumpMoveAcceleration * Time.deltaTime // 가속도에 따라 변경
                );

                // 수직 속도 갱신
                characterVelocity.y = velocityY;

                // 캐릭터 이동 (CharacterController를 사용하여 위치 갱신)
                characterController.Move(characterVelocity * Time.deltaTime);

                // 캐릭터가 하강 중일 때 바닥 감지
                if (velocityY < 0)
                    GroundCheck();

                // 점프 높이 갱신 (현재 높이가 jumpMaxPosY보다 크면 갱신)
                if (jumpMaxPosY < transform.position.y)
                    jumpMaxPosY = transform.position.y;

                // 이동 방향이 있고 회전 제한이 없다면 캐릭터의 회전 적용
                if (moveDir != Vector3.zero && !playerController.PreventRotation)
                {
                    if (useMultiDirectionalAnimation)
                    {
                        // 다방향 애니메이션을 사용하는 경우 목표 각도로 회전
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            targetRotation,
                            Time.deltaTime * 100 * rotationSpeed
                        );
                    }
                    else
                    {
                        // 단방향 (기본) 설정: 이동 방향을 향해 회전
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(moveDir),
                            Time.deltaTime * 100 * rotationSpeed
                        );
                    }
                }

                yield return null; // 한 프레임 대기

                // 플레이어 상태가 점프 조건과 다른 경우 (강제 취소)
                if (playerController.CurrentSystemState != State)
                    yield break;
            }

            // 점프 종료 처리
            targetRotation = transform.rotation; // 현재 각도를 목표 회전 값으로 설정
            playerController.IsInAir = false; // 공중 상태 해제

            // 점프 착지 처리 코루틴 호출
            yield return VerticalJumpLanding();

            preventLocomotion = false; // Locomotion 동작 제한 해제
        }

        IEnumerator VerticalJumpLanding()
        {
            // 점프의 최대 높이(`jumpMaxPosY`)와 현재 위치(y축) 간의 차이를 계산
            jumpHeightDiff = Mathf.Abs(jumpMaxPosY - transform.position.y);

            // 강한 착지(하드 랜딩)가 필요한 최소 높이 차이를 넘어선 경우
            if (jumpHeightDiff > minJumpHeightForHardland)
            {
                // 캐릭터를 약간 아래로 이동 (CharacterController 사용)
                characterController.Move(Vector3.down);

                // 착지 위치의 앞 방향으로 구르기 공간이 있는지 확인
                var halfExtends = new Vector3(.3f, .9f, 0.01f); // BoxCast 검사 크기
                var hasSpaceForRoll = Physics.BoxCast(
                    transform.position + Vector3.up, // 시작 위치 (캐릭터의 약간 위)
                    halfExtends, // BoxCast 크기
                    transform.forward, // 검사 방향 (캐릭터의 앞 방향)
                    Quaternion.LookRotation(transform.forward), // 검사 방향의 회전값
                    2.5f, // 검사 거리
                    environmentScanner.ObstacleLayer // 충돌 검사용 레이어
                );

                // 캐릭터 앞쪽 위 장애물(높은 곳)이 있는지 확인
                halfExtends = new Vector3(.1f, .9f, 0.01f); // 검사 Box의 크기 조정
                var heightHiting = true;

                // 반복 루프로 높이 검사(BoxCast 반복)를 통해 캐릭터가 움츠리거나 구를 수 있는 공간이 얼마나 있는지 확인
                for (int i = 0; i < 6 && heightHiting; i++)
                {
                    heightHiting = Physics.BoxCast(
                        transform.position + Vector3.up * 1.8f + transform.forward * (i * .5f + .5f), // 박스 시작 위치
                        halfExtends, // 검사 박스의 크기
                        Vector3.down, // 검사 방향 (아래 방향)
                        Quaternion.LookRotation(Vector3.down), // 검사 방향 회전값
                        2.2f + i * .1f, // 검사 거리 점진적으로 증가
                        environmentScanner.ObstacleLayer // 충돌 검사용 레이어
                    );
                }

                // 시스템 시작 처리
                OnStartSystem(this);

                // Root Motion 활성화
                EnableRootMotion();

                // 특정 조건에 따라 적절한 착지 애니메이션 실행
                // 구르는 공간이 없고 위에서 장애물이 있는 경우 롤링 애니메이션 적용 로직의 주석 처리됨
                // if (!hasSpaceForRoll && heightHiting)
                //     yield return DoLocomotionAction("FallingToRoll", crossFadeTime: .1f);
                // else
                yield return DoLocomotionAction("Landing", crossFadeTime: .1f);

                // Root Motion 비활성화
                DisableRootMotion();

                // 시스템 종료 처리
                OnEndSystem(this);
            }
            else
            {
                // 점프 높이가 작을 경우(하드 랜딩이 필요 없는 경우) "LandAndStepForward" 애니메이션 실행
                animator.CrossFadeInFixedTime("LandAndStepForward", .1f);
            }
        }

        public bool IsOnLedge { get; set; }

        public (Vector3, Vector3) LedgeMovement(Vector3 currMoveDir, Vector3 currVelocity)
        {
            // 입력된 이동 방향이 없는 경우, 그대로 반환
            if (currMoveDir == Vector3.zero) return (currMoveDir, currVelocity);

            // 캐릭터 주변의 검사 범위 초기값 설정
            float yOffset = 0.5f; // 위로 약간 올려 장애물 감지 검사 시 Offset 높이
            float xOffset = 0.4f; // 양쪽 발 중앙 및 벽 근처 감지 공간 크기
            float forwardOffset = xOffset / 2f; // 정면 감지 공간 기준 값
            var radius = xOffset / 2; // 스피어캐스트 반경 (움직임 각도 확인 가능)

            // 캐릭터 상태에 따라 검사 크기 축소 (앉은 자세 등 처리)
            if (animator.GetFloat(AnimatorParameters.IdleType) > 0.5f)
            {
                xOffset = 0.2f;
                radius = xOffset / 2f; // 작은 각도와 작은 오프셋 범위 사용 (앉은 상태)
                forwardOffset = xOffset / 2f; // 앞 방향의 좁은 이동 계산
            }

            // 검사에 사용할 각도 및 속도값 초기화
            float maxAngle = 60f; // 감지할 플랫폼 경사 최대 각도
            float velocityMag = currVelocity.magnitude; // 현재 속도 크기
            var dir = currMoveDir; // 현재 이동 방향 복사

            // 검사 대상이 되는 3개 주요 포인트 초기화
            var positionOffset = transform.position + currMoveDir * xOffset; // 캐릭터의 정중앙 위치
            var rigthVec = Vector3.Cross(Vector3.up, currMoveDir); // 이동 방향에 수직인 우측 방향 벡터
            var rightLeg = transform.position + currMoveDir * forwardOffset + rigthVec * xOffset / 2; // 오른발 위치
            var leftLeg = transform.position + currMoveDir * forwardOffset - rigthVec * xOffset / 2; // 왼발 위치

            // Debug 용 Ray를 그릴 때 주석 제거 가능
            //Debug.DrawRay(positionOffset + Vector3.up * yOffset, Vector3.down);
            //Debug.DrawRay(rightLeg + Vector3.up * yOffset, Vector3.down);
            //Debug.DrawRay(leftLeg + Vector3.up * yOffset, Vector3.down);

            // 오른쪽 발 아래에서 플랫폼 레이캐스트를 사용하여 설정된 조건 확인
            var rightFound =
                (Physics.Raycast(rightLeg + Vector3.up * yOffset, Vector3.down, out var rightHit,
                     yOffset + environmentScanner.ledgeHeightThreshold, environmentScanner.ObstacleLayer) &&
                 (rightHit.distance - yOffset) < environmentScanner.ledgeHeightThreshold &&
                 Vector3.Angle(Vector3.up, rightHit.normal) < maxAngle);

            // 왼쪽 발 아래에서 플랫폼 레이캐스트를 사용하여 설정된 조건 확인
            var leftFound =
                (Physics.Raycast(leftLeg + Vector3.up * yOffset, Vector3.down, out var leftHit,
                     yOffset + environmentScanner.ledgeHeightThreshold, environmentScanner.ObstacleLayer) &&
                 (leftHit.distance - yOffset) < environmentScanner.ledgeHeightThreshold &&
                 Vector3.Angle(Vector3.up, leftHit.normal) < maxAngle);

            // 오른쪽/왼쪽 발 중 하나라도 검출되지 않은 경우, 측면으로 위치를 이동해 다시 확인
            if (!rightFound) positionOffset += rigthVec * xOffset / 2;
            if (!leftFound) positionOffset -= rigthVec * xOffset / 2;

            // 플랫폼 탐지 플래그 초기화
            IsOnLedge = false;

            // 레지(ledge) 감지: 중앙 지점에서 스피어캐스트를 사용하여 벽 등을 감지
            if (!(Physics.SphereCast(positionOffset + Vector3.up * yOffset, radius, Vector3.down, out var newHit,
                    yOffset + environmentScanner.ledgeHeightThreshold, environmentScanner.ObstacleLayer)) ||
                ((newHit.distance - yOffset) > environmentScanner.ledgeHeightThreshold &&
                 Vector3.Angle(Vector3.up, newHit.normal) > maxAngle))
            {
                // 감지된 장애물이 없거나 조건을 벗어난 경우 현재 위치를 속도와 방향 중지
                IsOnLedge = true;

                if (!rightFound || !leftFound)
                {
                    if (!(!rightFound && !leftFound) && preventLedgeRotation) // 특정 상황에서는 회전을 제한
                        currMoveDir = Vector3.zero;
                    currVelocity = Vector3.zero;
                }
            }
            else if ((!rightFound || !leftFound)) // 한쪽 발만 발견된 경우
            {
                // 오른쪽 발이 있을 경우
                if (rightFound)
                {
                    if (Physics.SphereCast(leftLeg + Vector3.up * yOffset, 0.1f, Vector3.down, out leftHit,
                            yOffset + environmentScanner.ledgeHeightThreshold, environmentScanner.ObstacleLayer))
                        currVelocity = (newHit.point - leftHit.point).normalized * velocityMag; // 왼쪽 감지 기반 속도
                    else
                        currVelocity = (newHit.point - leftLeg).normalized * velocityMag; // 왼발에서 거리 계산
                }
                // 왼쪽 발이 있을 경우
                else if (leftFound)
                {
                    if (Physics.SphereCast(rightLeg + Vector3.up * yOffset, 0.1f, Vector3.down, out rightHit,
                            yOffset + environmentScanner.ledgeHeightThreshold, environmentScanner.ObstacleLayer))
                        currVelocity = (newHit.point - rightHit.point).normalized * velocityMag; // 오른쪽 감지 기반 속도
                    else
                        currVelocity = (newHit.point - rightLeg).normalized * velocityMag; // 오른발에서 거리 계산
                }
                // 양쪽 발 검출 불가 또는 경사가 너무 클 경우 속도 제거
                else if ((rightHit.transform != null && Vector3.Angle(Vector3.up, rightHit.normal) > maxAngle) ||
                         (leftHit.transform != null && Vector3.Angle(Vector3.up, leftHit.normal) > maxAngle))
                    currVelocity = Vector3.zero;
            }

            // 속도가 없는 경우 이동과 속도를 그대로 반환
            if (currVelocity == Vector3.zero)
                return (currMoveDir, currVelocity);

            // x, z 축만 사용하는 이동 방향 반환
            return (new Vector3(currVelocity.x, 0, currVelocity.z), currVelocity);
        }

        #region changeSpeed

        float _walkSpeed; // 초기 저장용 걸음 속도
        float _runSpeed; // 초기 저장용 달리기 속도
        float _sprintSpeed; // 초기 저장용 스프린트 속도

        public void ChangeMoveSpeed(float walk, float run, float sprint)
        {
            // 매개변수를 통해 현재 이동 속도를 설정
            walkSpeed = walk; // 현재 걸음 속도
            runSpeed = run; // 현재 달리기 속도
            sprintSpeed = sprint; // 현재 스프린트 속도
        }

        public void ResetMoveSpeed()
        {
            // 초기 설정된 이동 속도로 되돌림
            walkSpeed = _walkSpeed; // 초기 걸음 속도로 복구
            runSpeed = _runSpeed; // 초기 달리기 속도로 복구
            sprintSpeed = _sprintSpeed; // 초기 스프린트 속도로 복구
        }

        #endregion

        #region Interface

// 시스템 시작 시 호출되는 함수
        public void OnStartSystem(SystemBase systemBase)
        {
            // 모든 시스템에서 포커스를 해제
            playerController.UnfocusAllSystem();

            // 시스템 활성화를 위한 해당 스크립트 포커싱
            systemBase.FocusScript();

            // 시스템에 진입 (필요한 진입 동작 실행)
            systemBase.EnterSystem();

            // Locomotion(이동) 비활성화
            preventLocomotion = true;

            // 현재 캐릭터 이동 속도 0으로 설정
            currentSpeed *= 0f;

            // 플레이어 컨트롤러에 현재 시스템 상태(SetSystemState) 전달
            playerController.SetSystemState(systemBase.State);

            // 현재 캐릭터의 회전을 목표 회전값으로 설정
            targetRotation = transform.rotation;

            // 캐릭터를 공중 상태로 설정
            isGrounded = false;

            // 애니메이터에서 매개변수 값들을 점진적으로 0으로 변경 (트윈 애니메이션 효과)
            StartCoroutine(TweenVal(animator.GetFloat(AnimatorParameters.MoveAmount), 0, 0.15f,
                (lerpVal) => { animator.SetFloat(AnimatorParameters.MoveAmount, lerpVal); }));
            StartCoroutine(TweenVal(animator.GetFloat(AnimatorParameters.Rotation), 0, 0.15f,
                (lerpVal) => { animator.SetFloat(AnimatorParameters.Rotation, lerpVal); }));
            StartCoroutine(TweenVal(animator.GetFloat(AnimatorParameters.IdleType), 0, 0.15f,
                (lerpVal) => { animator.SetFloat(AnimatorParameters.IdleType, lerpVal); }));
        }

// 시스템 종료 시 호출되는 함수
        public void OnEndSystem(SystemBase systemBase)
        {
            // 시스템 비활성화를 위한 해당 스크립트 언포커싱
            systemBase.UnFocusScript();

            // 시스템에서 나가기 (Exit 동작 실행)
            systemBase.ExitSystem();

            // 플레이어 컨트롤러 상태 초기화 (Reset 상태)
            playerController.ResetState();

            // 현재 캐릭터 회전을 목표 회전값으로 설정
            targetRotation = transform.rotation;

            // Locomotion(이동) 활성화
            preventLocomotion = false;
        }

// 캐릭터 이동 방향에 대한 게터(get)와 세터(set)
        public Vector3 MoveDir
        {
            get { return desiredMoveDir; } // 현재 이동 방향 반환
            set { desiredMoveDir = value; } // 새로운 이동 방향 설정
        }

// 캐릭터가 땅에 닿아있는지 여부를 반환
        public bool IsGrounded => isGrounded;

        // 캐릭터가 받는 중력 값 반환
        public float Gravity => -20;

        // 특정 상황에서 모든 시스템을 일시적으로 비활성화 여부 설정
        public bool PreventAllSystems { get; set; } = false;

        // 캐릭터 애니메이터에 대한 게터 및 세터
        public Animator Animator
        {
            get
            {
                // animator가 null 값이면 해당 컴포넌트를 가져오고, 그렇지 않으면 기존 값을 반환
                return animator == null ? GetComponent<Animator>() : animator;
            }
            set
            {
                // 새로운 Animator 값을 설정
                animator = value;
            }
        }

        // Root Motion 사용 여부를 제어
        public bool UseRootMotion { get; set; }

        #endregion

        public bool IsCrouching => crouchVal > 0.5f;
        public bool IsDead => Health <= 0;

        public float Health { get; set; } = 10000; // 기본 체력 설정
        public float DamageMultiplier { get; set; } = 1; // 데미지 배율 (1이 기본값)
        public Action<Vector3, float> OnHit { get; set; } // 피격 이벤트 처리
        public IDamageable Parent => this; // 이 클래스가 IDamageable을 구현했음을 나타냄

        private void OnEnable()
        {
            // 이 컴포넌트가 활성화될 때 OnHit 이벤트에 TakeDamage 메서드 등록
            OnHit += TakeDamage;
        }

        private void OnDisable()
        {
            // 이 컴포넌트가 비활성화될 때 OnHit 이벤트에서 TakeDamage 메서드 등록 해제
            OnHit -= TakeDamage;
        }

        void TakeDamage(Vector3 dir, float damage)
        {
            // 데미지를 입었을 때 Health를 감소
            Health = Mathf.Clamp(Health - damage, 0, Mathf.Infinity); // 체력은 0 이하로 떨어지지 않음
        }
    }
}