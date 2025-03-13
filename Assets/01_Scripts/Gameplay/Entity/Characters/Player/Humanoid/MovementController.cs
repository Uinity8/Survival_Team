using UnityEngine;

namespace Framework.Characters
{
    [RequireComponent(typeof(CharacterController))] // CharacterController 컴포넌트 필수
    public class MovementController : MonoBehaviour
    {
        [Header("플레이어 설정")] [Tooltip("캐릭터의 이동 속도 (m/s)")]
        public float MoveSpeed = 2.0f;
        
        [Tooltip("캐릭터의 질주(Sprint) 속도 (m/s)")] 
        public float SprintSpeed = 5.335f;
        
        [Tooltip("질주시 소모되는 스테미나")]
        public float SprintStaminaCost = 20f;

        [Tooltip("캐릭터가 이동 방향을 바라보는 속도")] [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("가속 및 감속 비율")] 
        public float SpeedChangeRate = 10.0f;

        [Space(10)] [Tooltip("플레이어가 점프할 수 있는 최대 높이")]
        public float JumpHeight = 1.2f;

        [Tooltip("캐릭터가 사용할 자체 중력 값 (엔진 기본값은 -9.81f)")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("점프 후 다시 점프할 수 있기까지 필요한 시간 (0f으로 설정하면 즉시 점프 가능)")]
        public float JumpTimeout = 0.50f;

        [Tooltip("캐릭터가 낙하 상태로 진입하기 전까지 걸리는 시간 (계단 내려갈 때 유용)")]
        public float FallTimeout = 0.15f;

        // 플레이어 이동 관련 변수
        private float _speed;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private readonly float _terminalVelocity = 53.0f;

        //애니메이터로 전달할 값
        public float AnimationBlend { get; private set; }
        public float InputMagnitude { get; private set; }
        public bool IsJumping { get; private set; }     // 점프 중인지 저장
        public bool IsFreeFalling { get; private set; } // 낙하 중인지 저장

        
        // 점프 및 낙하 시간 변수
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private CharacterController _controller;
        private InputHandler _input;
        private GameObject _mainCamera;
        
        
        public  void  Initialize(InputHandler input, GameObject mainCamera)
        {
            _input = input;
            _mainCamera = mainCamera;
            _controller = GetComponent<CharacterController>();
            
            // 점프 및 낙하 타이머 초기화
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        public  void  Move()
        {
            
            bool isSprinting = _input.sprint;
            
            // 간단한 가속 및 감속 시스템 (제거, 교체 또는 변경이 용이함)

            // 참고: Vector2의 == 연산자는 근사 비교를 사용하므로 부동소수점 오류에 안전하며, magnitude보다 성능이 우수함.
            // 입력이 없으면 목표 속도를 0으로 설정
            float targetSpeed = isSprinting ? SprintSpeed : MoveSpeed;
            
        
            if (_input.move == Vector2.zero) targetSpeed = 0f;

            // 플레이어의 현재 수평 이동 속도를 가져옴
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            InputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // 목표 속도에 맞춰 가속 또는 감속
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // 선형 변화보다 부드러운 가속 곡선을 만들기 위해 Lerp 사용
                // Lerp의 T 값은 이미 0~1 범위로 제한되므로 추가적인 클램핑이 필요 없음
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * InputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // 속도를 소수점 3자리까지 반올림
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            // 애니메이션 블렌딩 속도를 보간하여 부드럽게 변경
            AnimationBlend = Mathf.Lerp(AnimationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (AnimationBlend < 0.01f) AnimationBlend = 0f;

            // 입력 방향을 정규화 (magnitude가 1이 되도록)
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // 참고: Vector2의 != 연산자는 근사 비교를 사용하므로 부동소수점 오류에 안전하며, magnitude보다 성능이 우수함.
            // 이동 입력이 있을 경우 플레이어가 이동 방향을 바라보도록 회전
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // 카메라 방향을 기준으로 이동 방향을 바라보도록 회전 적용
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // 목표 이동 방향 설정 (카메라 방향 기준으로 변환)
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // 플레이어 이동 적용
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            
        }
        
        public  void JumpAndGravity(bool grounded)
        {
            if (grounded) // 플레이어가 지면에 있는 경우
            {
                // 낙하 타이머 초기화
                _fallTimeoutDelta = FallTimeout;

                IsJumping = false;
                IsFreeFalling = false;
                

                // 지면에 있을 때, 중력이 무한히 누적되지 않도록 초기화
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f; // 살짝 음수 값을 줘서 지면에 붙어 있게 함
                }

                // 점프 처리
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // 점프 공식: H = 점프 높이, G = 중력 값
                    // H * -2 * G 
                    // 필요한 초기 속도를 계산하여 점프 높이를 설정
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    IsJumping = true;
                }

                // 점프 쿨타임 처리
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else // 공중에 있는 경우 (낙하 상태)
            {
                // 점프 타이머 초기화
                _jumpTimeoutDelta = JumpTimeout;
                

                // 낙하 타이머 처리
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    IsFreeFalling = true;
                }

                // 공중에 있을 때 점프 입력을 초기화 (더블 점프 방지)
                _input.jump = false;
            }

            // 중력 적용 (터미널 속도까지 증가)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }
        
        public void ApplyForceY(float force)
        {
            _verticalVelocity += force;
            _input.jump = true;
        }
        
    }
}