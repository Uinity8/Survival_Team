using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _01_Scripts.Input;
using _01_Scripts.System;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;


namespace _01_Scripts.Third_Person_Controller
{
    /// <summary>
    /// 카메라의 이동, 회전, 충돌 처리 등 다양한 기능을 담당하는 카메라 컨트롤러 클래스
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // [Inspector에 표시되는 변수들]
        // ─────────────────────────────────────────────────────────────

        [Tooltip("따라갈 대상 Transform (예: 플레이어)")]
        public Transform followTarget;

        [Tooltip("카메라의 기본 설정. 플레이어 상태에 따라 이 설정을 재정의할 수 있음.")] [SerializeField]
        CameraSettings defaultSettings;

        [Tooltip("켜져있으면 플레이어가 좌우로 이동할 때 카메라가 회전함.")] [SerializeField]
        bool advancedCameraRotation = true;

        [Tooltip("충돌 체크할 레이어들")] [SerializeField]
        LayerMask collisionLayers = 1;

        [Tooltip("플레이 모드 시작 전에 설정해야 합니다. 게임 실행 중에는 변경할 수 없음.")] [SerializeField]
        bool lockCursor = true;

        [Tooltip("카메라 거리 변경 시 적용할 부드러운 시간")] [SerializeField]
        float distanceSmoothTime = 0.3f;

        [Tooltip("충돌로 인해 카메라 거리가 변경될 때 적용할 부드러운 시간")] [SerializeField]
        float distanceSmoothTimeWhenOccluded = 0f;

        [Tooltip("프레이밍 오프셋 변경 시 적용할 부드러운 시간")] [SerializeField]
        float framingSmoothTime = 0.5f;

        // 카메라 충돌 시 여분의 공간(padding) 값
        [SerializeField] float collisionPadding = 0.05f;

        [Tooltip("플레이어의 상태에 따라 카메라 설정을 재정의할 때 사용할 설정 목록")] [SerializeField]
        List<OverrideSettings> overrideCameraSettings;

        [Tooltip("플레이 모드 시작 전에 설정해야 합니다. 게임 실행 중에는 변경할 수 없음.")] [SerializeField]
        float nearClipPlane = 0.1f;

        // 카메라 흔들림 효과의 기본 배율 값
        private const float cameraShakeAmount = 0.6f;


        // ─────────────────────────────────────────────────────────────
        // [내부 변수들: 설정, 상태 및 계산 변수]
        // ─────────────────────────────────────────────────────────────

        // 현재 사용 중인 카메라 설정 (기본 설정 또는 재정의된 설정)
        CameraSettings settings;

        // 플레이어가 재정의한 카메라 설정 (없으면 null)
        CameraSettings customSettings;

        // 현재 카메라 상태 (CameraState 열거형, 예: Locomotion, Combat 등)
        CameraState currentState;

        // 현재 따라가고 있는 위치 (부드러운 이동을 위해 사용)
        Vector3 currentFollowPos;

        // 목표 카메라 거리와 현재 카메라 거리 (충돌 처리 및 부드러운 보간에 사용)
        float targetDistance, curDistance;

        // 목표 프레이밍 오프셋과 현재 프레이밍 오프셋 (카메라 프레이밍 보정에 사용)
        Vector3 targetFramingOffset, curFramingOffset;

        // 거리 보간에 사용되는 속도 변수
        float distSmoothVel = 0f;

        // 프레이밍과 팔로우 보간에 사용되는 벡터 속도 변수
        Vector3 framingSmoothVel, followSmoothVel = Vector3.zero;

        // 카메라 회전 관련 변수 (X축: 상하, Y축: 좌우)
        float rotationX;

        float rotationY;

        // 보조 회전 값 (Lerp 처리를 위한 Y값 보관)
        float yRot;

        // 회전 입력에 대한 반전 값 (설정에 따라 반전)
        float invertXVal;
        float invertYVal;

        // ─────────────────────────────────────────────────────────────
        // [참조 변수들: 컴포넌트, 입력 관리자, 플레이어 컨트롤러 등]
        // ─────────────────────────────────────────────────────────────

        Camera _camera; // 카메라 컴포넌트
        PlayerController playerController; // 플레이어 컨트롤러
        LocomotionController locomotionController; // 플레이어 이동 컨트롤러
        private LocomotionInputManager input;

        // ─────────────────────────────────────────────────────────────
        // Awake() : 초기화 단계에서 호출됨
        // ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            // 현재 GameObject의 Camera 컴포넌트를 가져옴
            _camera = GetComponent<Camera>();
            // 카메라의 nearClipPlane 값을 지정 (카메라에서 볼 수 있는 최소 거리)
            _camera.nearClipPlane = nearClipPlane;

            // followTarget의 부모 객체에서 PlayerController 컴포넌트를 가져옴
            playerController = followTarget.GetComponentInParent<PlayerController>();
            // PlayerController에서 LocomotionController 컴포넌트를 가져옴
            locomotionController = playerController.GetComponent<LocomotionController>();
            // PlayerController에서 LocomotionInputManager 컴포넌트를 가져옴
            input = playerController.GetComponent<LocomotionInputManager>();

            // 이벤트 등록: 기존 이벤트 핸들러 제거 후 재등록 (중복 등록 방지)
            playerController.OnStartCameraShake -= StartCameraShake;
            playerController.OnStartCameraShake += StartCameraShake;

            // 착지 시 카메라 흔들림 효과를 적용하기 위한 이벤트 등록
            playerController.OnLand -= playerController.OnStartCameraShake;
            playerController.OnLand += playerController.OnStartCameraShake;

            // 커스텀 카메라 상태를 설정하는 이벤트 등록
            playerController.SetCustomCameraState += SetCustomCameraState;
            // 카메라 반동 효과 이벤트 등록
            playerController.CameraRecoil += CameraRecoil;
        }


        // ─────────────────────────────────────────────────────────────
        // Start() : 초기화 후 첫 프레임에서 호출됨
        // ─────────────────────────────────────────────────────────────
        private void Start()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            // PC 등에서 커서 잠금 설정 (모바일은 필요 없음)
            if (lockCursor)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
#endif
            // 초기 팔로우 위치 설정 (followTarget의 위치에서 약간 뒤쪽으로 오프셋)
            currentFollowPos = followTarget.position - Vector3.forward * 4;
            // 기본 카메라 설정 적용
            settings = defaultSettings;
            // 현재 카메라 거리를 기본 설정의 거리로 초기화
            curDistance = settings.distance;
            // 현재 프레이밍 오프셋을 기본 설정의 프레이밍 오프셋으로 초기화
            curFramingOffset = settings.framingOffset;

            // 카메라의 near clip plane 상의 포인트 계산 (충돌 체크용)
            CalculateNearPlanePoints();
        }

        // ─────────────────────────────────────────────────────────────
        // nearPlanePoints : 카메라의 near clip plane에 해당하는 점들을 저장 (충돌 체크용)
        // ─────────────────────────────────────────────────────────────
        readonly List<Vector3> nearPlanePoints = new List<Vector3>();

        /// <summary>
        /// 카메라의 near clip plane 상의 5개 포인트를 계산하여 nearPlanePoints 리스트에 저장
        /// (네 모서리와 중앙 점)
        /// </summary>
        void CalculateNearPlanePoints()
        {
            // near clip plane의 Z 거리
            float z = _camera.nearClipPlane;
            // FOV를 이용하여 Y값 계산 (카메라의 상단 혹은 하단까지의 거리)
            float y = Mathf.Tan((_camera.fieldOfView / 2) * Mathf.Deg2Rad) * z;
            // 카메라의 종횡비를 적용하여 X값 계산 + 충돌 패딩 추가
            float x = y * _camera.aspect + collisionPadding;

            // 네 모서리와 중앙(0,0,0) 포인트를 리스트에 추가
            nearPlanePoints.Add(new Vector3(x, y, z)); // 우측 상단
            nearPlanePoints.Add(new Vector3(-x, y, z)); // 좌측 상단
            nearPlanePoints.Add(new Vector3(x, -y, z)); // 우측 하단
            nearPlanePoints.Add(new Vector3(-x, -y, z)); // 좌측 하단
            nearPlanePoints.Add(Vector3.zero); // 중앙 (추가 용도)
        }

        // ─────────────────────────────────────────────────────────────
        // SystemToCameraState() : 플레이어 시스템 상태를 카메라 상태로 변환
        // ─────────────────────────────────────────────────────────────
        CameraState SystemToCameraState(SystemState state)
        {
            // 기본 이동 상태일 때, 이동 컨트롤러의 crouching(웅크림) 여부에 따라 카메라 상태 결정
            if (state == SystemState.Locomotion)
                return (locomotionController.IsCrouching) ? CameraState.Crouching : CameraState.Locomotion;

            // 그 외의 상태는 직접 캐스팅하여 사용
            return (CameraState)state;
        }

        // ─────────────────────────────────────────────────────────────
        // SetCustomCameraState() : 플레이어에 의해 커스텀 카메라 설정을 적용하는 함수
        // ─────────────────────────────────────────────────────────────
        private void SetCustomCameraState(CameraSettings cameraSettings = null)
        {
            // 재정의 설정을 전달받으면 customSettings에 저장
            customSettings = cameraSettings;
            if (customSettings == null)
            {
                // customSettings가 없으면 현재 플레이어 상태에 따른 카메라 설정 재정의 확인
                var currPlayerState = SystemToCameraState(playerController.CurrentSystemState);
                var overrideSettings = overrideCameraSettings.FirstOrDefault(x => x.state == currPlayerState);
                settings = overrideSettings != null ? overrideSettings.settings : defaultSettings;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // 전역 카메라 반동 효과 관련 정보 (랜덤 반동 및 지속시간 처리)
        // ─────────────────────────────────────────────────────────────
        PlayerController.RecoilInfo GlobalRecoilInfo { get; set; } =
            new PlayerController.RecoilInfo() { CameraRecoilDuration = 0 };

        // 카메라 반동 효과의 전체 지속 시간을 저장하는 프로퍼티
        public float CameraTotalRecoilDuration { get; private set; }

        /// <summary>
        /// 카메라 반동 효과를 적용하는 함수.
        /// 입력받은 RecoilInfo 값을 이용하여 랜덤 반동량 및 최소 반동량 보정, 지속시간 설정 등을 수행.
        /// </summary>
        /// <param name="recoilInfo">반동 효과 정보</param>
        private void CameraRecoil(PlayerController.RecoilInfo recoilInfo)
        {
            // 랜덤 반동량을 계산 (insideUnitCircle으로 무작위 2D 벡터 생성 후, 설정값과 스케일 적용)
            GlobalRecoilInfo.CameraRecoilAmount = Vector3.Scale(Random.insideUnitCircle, recoilInfo.CameraRecoilAmount);

            // 최소 반동량 보정: 설정된 최소값보다 작으면 보정
            if (recoilInfo.minRecoilAmount != Vector2.zero)
            {
                GlobalRecoilInfo.CameraRecoilAmount.x = Mathf.Sign(GlobalRecoilInfo.CameraRecoilAmount.x) *
                                                        Mathf.Max(Mathf.Abs(GlobalRecoilInfo.CameraRecoilAmount.x),
                                                            recoilInfo.minRecoilAmount.x);
                GlobalRecoilInfo.CameraRecoilAmount.y = Mathf.Max(Mathf.Abs(GlobalRecoilInfo.CameraRecoilAmount.y),
                    recoilInfo.minRecoilAmount.y);
            }

            // 전체 반동 지속시간 설정
            CameraTotalRecoilDuration = GlobalRecoilInfo.CameraRecoilDuration = recoilInfo.CameraRecoilDuration;
            // 반동 진행 단계 비율 설정 (예: 초기 반동과 회복 단계 비율)
            GlobalRecoilInfo.recoilPhasePercentage = recoilInfo.recoilPhasePercentage;
        }

        // ─────────────────────────────────────────────────────────────
        // LateUpdate() : 모든 Update() 호출 후 실행되어 카메라 업데이트에 최적화됨
        // ─────────────────────────────────────────────────────────────
        private void LateUpdate()
        {
            // 현재 플레이어 상태를 카메라 상태로 변환
            var curPlayerState = SystemToCameraState(playerController.CurrentSystemState);

            // customSettings가 설정되어 있으면 해당 설정 적용,
            // 없으면 현재 상태에 따른 재정의 설정 또는 기본 설정 사용
            if (customSettings != null)
                settings = customSettings;
            else if (currentState != curPlayerState)
            {
                var overrideSettings = overrideCameraSettings.FirstOrDefault(x => x.state == curPlayerState);
                settings = overrideSettings != null ? overrideSettings.settings : defaultSettings;
            }

            // 현재 카메라 상태 업데이트
            currentState = curPlayerState;

            // 카메라의 따라갈 대상: 만약 설정 내 followTarget이 null이면 기본 followTarget 사용
            var curFollowTarget = !settings.followTarget ? this.followTarget : settings.followTarget;

            // ─────────────────────────────────────────────────────────
            // 카메라 회전 처리
            // ─────────────────────────────────────────────────────────
            Quaternion targetRotation;
            if (settings.localRotationOffset != Vector2.zero)
            {
                // 로컬 회전 오프셋이 설정되어 있으면,
                // followTarget의 현재 회전 각도에 오프셋을 더해 목표 회전 계산
                var targetEuler = curFollowTarget.rotation.eulerAngles;
                var rotX = targetEuler.x + settings.localRotationOffset.y;
                var rotY = targetEuler.y + settings.localRotationOffset.x;
                targetRotation = Quaternion.Euler(rotX, rotY, 0);
            }
            else
            {
                // 로컬 오프셋이 없을 경우, 입력에 따른 카메라 회전 처리

                // 설정에 따라 입력 반전 값 결정
                invertXVal = (settings.invertX) ? -1 : 1;
                invertYVal = (settings.invertY) ? -1 : 1;

                // 입력받은 카메라 Y축 이동 값에 따라 상하 회전 조정
                rotationX += input.CameraInput.y * invertYVal * settings.sensitivity;
                // 상하 회전은 최소/최대 각도로 제한
                rotationX = Mathf.Clamp(rotationX, settings.minVerticalAngle, settings.maxVerticalAngle);

                // 좌우 회전 처리: 입력이 있을 경우 즉시 반영,
                // 없으면 advancedCameraRotation 옵션에 따라 부드러운 보간 적용
                if (input.CameraInput != Vector2.zero)
                    yRot = rotationY += input.CameraInput.x * invertXVal * settings.sensitivity;
                else if (advancedCameraRotation && playerController.CurrentSystemState == SystemState.Locomotion &&
                         input.CameraInput.x == 0 && input.DirectionInput.y > -.4f)
                {
                    StartCoroutine(CameraRotDelay());
                    rotationY = Mathf.Lerp(rotationY, yRot, Time.deltaTime * 25);
                }

                // 최종 목표 회전 값 계산
                targetRotation = Quaternion.Euler(rotationX, rotationY, 0);
            }

            // ─────────────────────────────────────────────────────────
            // 플레이어의 이동에 따른 카메라 위치 보간 처리
            // ─────────────────────────────────────────────────────────
            currentFollowPos = Vector3.SmoothDamp(currentFollowPos, curFollowTarget.position, ref followSmoothVel,
                settings.followSmoothTime);

            // 카메라 흔들림 효과 적용 (CameraShakeDuration이 남아있는 동안)
            if (CameraShakeDuration > 0)
            {
                // 현재 follow 위치에 랜덤한 구형 벡터를 추가해 흔들림 효과 구현
                currentFollowPos += Random.insideUnitSphere *
                                    (CurrentCameraShakeAmount * cameraShakeAmount * Mathf.Clamp01(CameraShakeDuration));
                // 시간에 따라 흔들림 지속시간 감소
                CameraShakeDuration -= Time.deltaTime;
            }

            // ─────────────────────────────────────────────────────────
            // 프레이밍(화면 구도) 보정 처리: 목표 프레이밍 오프셋과 보간 처리
            // ─────────────────────────────────────────────────────────
            targetFramingOffset = settings.framingOffset;
            curFramingOffset = Vector3.SmoothDamp(curFramingOffset, targetFramingOffset, ref framingSmoothVel,
                framingSmoothTime);

            // ─────────────────────────────────────────────────────────
            // 카메라 충돌 체크 및 거리 조절
            // ─────────────────────────────────────────────────────────
            // forward: 목표 회전 기준 상단 방향 (수평 성분만 남김)
            var forward = targetRotation * Vector3.up;
            forward.y = 0;
            // right: 목표 회전 기준 오른쪽 방향
            //var right = targetRotation * Vector3.right;

            // focusPosition: 카메라가 주시할 위치 (플레이어 위치 + 프레이밍 오프셋)
            var focusPosition = currentFollowPos + Vector3.up * curFramingOffset.y + forward * curFramingOffset.z;

            bool collisionAdjusted = false;
            if (settings.enableCameraCollisions)
            {
                // 충돌 체크를 위해 RaycastHit 변수 선언
                // 초기 가까운 거리 설정 (기본 카메라 거리)
                float closestDistance = settings.distance;

                // nearPlanePoints의 중앙값을 초기화
                nearPlanePoints[4] = Vector3.zero;
                // nearPlanePoints 리스트의 각 점에 대해 Raycast 수행하여 충돌 여부 체크
                foreach (var point in nearPlanePoints)
                {
                    // focusPosition에서 해당 nearPlanePoint 방향으로 레이캐스트 수행
                    if (Physics.Raycast(focusPosition, (transform.TransformPoint(point) - focusPosition), out var hit,
                            settings.distance, collisionLayers))
                    {
                        // 충돌한 거리 중 가장 가까운 거리를 저장
                        if (hit.distance < closestDistance)
                            closestDistance = hit.distance;

                        collisionAdjusted = true;
                        // 디버그용: 충돌 레이 시각화
                        // Debug.DrawRay(focusPosition, (transform.TransformPoint(nearPlanePoints[i]) - focusPosition), Color.red);
                    }
                }

                // 목표 카메라 거리를 충돌 시 최소 거리와 충돌한 거리 중 작은 값으로 설정
                targetDistance = Mathf.Clamp(closestDistance, settings.minDistanceFromTarget, closestDistance);
            }
            else
                targetDistance = settings.distance;

            // 카메라 거리를 부드럽게 보간하여 업데이트
            if (!collisionAdjusted)
                curDistance = Mathf.SmoothDamp(curDistance, targetDistance, ref distSmoothVel, distanceSmoothTime);
            else
            {
                // 만약 충돌로 인해 거리가 조절되면, 별도의 부드러운 시간이 설정되어 있으면 적용
                curDistance = distanceSmoothTimeWhenOccluded > Mathf.Epsilon
                    ? Mathf.SmoothDamp(curDistance, targetDistance, ref distSmoothVel, distanceSmoothTimeWhenOccluded)
                    : targetDistance;
            }

            // ─────────────────────────────────────────────────────────
            // 최종 카메라 위치와 회전 설정
            // ─────────────────────────────────────────────────────────
            // focusPosition에서 targetRotation 방향의 뒤쪽으로 currDistance만큼 떨어진 위치 계산
            transform.position = focusPosition - targetRotation * new Vector3(0, 0, curDistance);
            transform.rotation = targetRotation;
            // 프레이밍 오프셋의 X 성분을 거리 비율에 맞게 추가 (좌우 오프셋)
            transform.position += transform.right * (curFramingOffset.x * curDistance) / settings.distance;

            // ─────────────────────────────────────────────────────────
            // 카메라 반동 효과 적용 (총 지속시간 동안 회복)
            // ─────────────────────────────────────────────────────────
            if (GlobalRecoilInfo.CameraRecoilDuration > 0)
            {
                float normalizedTime = 1 - (GlobalRecoilInfo.CameraRecoilDuration / CameraTotalRecoilDuration);
                float recoilProgress;

                // 반동 진행 단계에 따라 초기 반동과 회복 단계로 나눠서 처리
                if (normalizedTime <= GlobalRecoilInfo.recoilPhasePercentage)
                {
                    recoilProgress = Time.deltaTime /
                                     (CameraTotalRecoilDuration * GlobalRecoilInfo.recoilPhasePercentage);
                    rotationY += GlobalRecoilInfo.CameraRecoilAmount.x * recoilProgress;
                    rotationX -= GlobalRecoilInfo.CameraRecoilAmount.y * recoilProgress;
                }
                else
                {
                    recoilProgress = Time.deltaTime /
                                     (CameraTotalRecoilDuration * (1 - GlobalRecoilInfo.recoilPhasePercentage));
                    rotationY -= GlobalRecoilInfo.CameraRecoilAmount.x * recoilProgress;
                    rotationX += GlobalRecoilInfo.CameraRecoilAmount.y * recoilProgress;
                }

                // 반동 지속시간 감소
                GlobalRecoilInfo.CameraRecoilDuration -= Time.deltaTime;
            }

            // 이전 followTarget의 위치를 저장 (카메라 회전 보간에 사용)
            previousPos = curFollowTarget.transform.position;
        }

        // ─────────────────────────────────────────────────────────────
        // PlanarRotation : 카메라의 수평(평면) 회전만 반환하는 속성
        // ─────────────────────────────────────────────────────────────
        public Quaternion PlanarRotation => Quaternion.Euler(0, rotationY, 0);

        // ─────────────────────────────────────────────────────────────
        // 회전 보간 및 이동 관련 변수들
        // ─────────────────────────────────────────────────────────────
        bool moving;
        Vector3 previousPos;
        bool inDelay;
        float cameraRotSmooth;

        // 카메라 흔들림 효과 관련 공개 속성
        public float CameraShakeDuration { get; private set; }
        public float CurrentCameraShakeAmount { get; private set; }

        /// <summary>
        /// 카메라 흔들림 효과를 시작하는 함수.
        /// 인자로 흔들림 강도와 지속시간을 받아 해당 효과를 적용함.
        /// </summary>
        /// <param name="currentCameraShakeAmount">현재 카메라 흔들림 강도</param>
        /// <param name="shakeDuration">흔들림 지속시간</param>
        private void StartCameraShake(float currentCameraShakeAmount, float shakeDuration)
        {
            CurrentCameraShakeAmount = currentCameraShakeAmount;
            CameraShakeDuration = shakeDuration;
        }

        /// <summary>
        /// 카메라 회전 지연 코루틴.
        /// 플레이어의 이동 거리를 확인하여 일정 시간 동안 회전 속도를 낮추고, 이후 서서히 보간 처리함.
        /// </summary>
        IEnumerator CameraRotDelay()
        {
            // 이전 위치와 현재 followTarget 위치 간의 거리를 계산
            var movDist = Vector3.Distance(previousPos, followTarget.transform.position);
            if (movDist > 0.001f)
            {
                if (!moving)
                {
                    moving = true;
                    inDelay = true;
                    // 1.5초 대기 후 지연 해제
                    yield return new WaitForSeconds(1.5f);
                    inDelay = false;
                }
            }
            else
            {
                moving = false;
                cameraRotSmooth = 0;
            }

            // 회전 보간 속도를 inDelay 여부에 따라 결정 후 적용
            cameraRotSmooth = Mathf.Lerp(cameraRotSmooth, !inDelay ? 25 : 5, Time.deltaTime);
            yRot = Mathf.Lerp(yRot, yRot + input.DirectionInput.x * invertXVal * 2, Time.deltaTime * cameraRotSmooth);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // CameraSettings 클래스
    // 카메라의 기본 설정을 저장하며, 직렬화 후 기본값을 적용할 수 있음.
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class CameraSettings : ISerializationCallbackReceiver
    {
        // 따라갈 대상 (카메라 오프셋 등)
        public Transform followTarget;

        // 기본 카메라 거리
        public float distance = 2.5f;

        // 카메라 프레이밍 오프셋 (예: 플레이어 위쪽, 앞쪽 등)
        public Vector3 framingOffset = new Vector3(0, 1.5f, 0);

        // 따라가는 움직임의 부드러운 시간
        public float followSmoothTime = 0.2f;

        // 로컬 회전 오프셋: followTarget의 회전에 덧셈될 값 (X: 좌우, Y: 상하)
        public Vector2 localRotationOffset;

        // 카메라 회전 민감도 (0~1 사이 값)
        [Range(0, 1)] public float sensitivity = 0.6f;

        // 상하 회전 최소/최대 각도 제한
        public float minVerticalAngle = -45;
        public float maxVerticalAngle = 70;

        // 입력 반전 여부
        public bool invertX;
        public bool invertY = true;

        // 카메라 충돌 여부 및 최소 거리 설정
        public bool enableCameraCollisions = true;
        public float minDistanceFromTarget = 0.2f;

        // 직렬화 관련 변수 (에디터에서 한 번만 기본값 적용)
        [SerializeField, HideInInspector] private bool serialized = false;

        public void OnAfterDeserialize()
        {
            if (serialized == false)
            {
                // 초기 기본값 설정 (한 번만 적용)
                distance = 2.5f;
                framingOffset = new Vector3(0, 1.5f, 0);
                followSmoothTime = 0.2f;
                sensitivity = 0.6f;
                minVerticalAngle = -45;
                maxVerticalAngle = 70;
                invertY = true;
                enableCameraCollisions = true;
                minDistanceFromTarget = 0.2f;
            }
        }

        public void OnBeforeSerialize()
        {
            // 한 번 직렬화되었다면 더 이상 기본값 재설정하지 않음
            if (serialized)
                return;

            serialized = true;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // OverrideSettings 클래스
    // 특정 카메라 상태에 대해 재정의할 카메라 설정을 저장하는 클래스.
    // ─────────────────────────────────────────────────────────────
    [Serializable]
    public class OverrideSettings
    {
        // 재정의할 카메라 상태 (CameraState 열거형)
        public CameraState state;

        // 해당 상태에서 적용할 카메라 설정
        public CameraSettings settings;
    }

    // ─────────────────────────────────────────────────────────────
    // CameraState 열거형
    // 플레이어 시스템 상태와 매칭되는 카메라 상태 (CameraController에서 사용)
    // ─────────────────────────────────────────────────────────────
    public enum CameraState
    {
        Locomotion, // 기본 이동 상태
        Parkour, // 파쿠르 상태
        Climbing, // 등반 상태
        Combat, // 전투 상태
        GrapplingHook, // 그래플링 훅 상태
        Swing, // 그네 또는 줄타기 상태
        Other, // 기타 상태
        Crouching, // 웅크린 상태 (Locomotion 상태에서 웅크림 여부에 따라 구분)
    }
}