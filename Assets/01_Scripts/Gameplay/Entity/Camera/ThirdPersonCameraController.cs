using DefaultNamespace;
using UnityEngine;

namespace _01_Scripts.Gameplay.Entity.Characters.Player.Humanoid.Camera
{
    public class ThirdPersonCameraController : MonoBehaviour
    {
        [Header("Cinemachine 카메라 설정")]
        [Tooltip("Cinemachine Virtual Camera가 따라갈 대상 오브젝트")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("카메라가 위쪽으로 이동할 수 있는 최대 각도")]
        public float TopClamp = 70.0f;

        [Tooltip("카메라가 아래쪽으로 이동할 수 있는 최대 각도")] 
        public float BottomClamp = -30.0f;

        [Tooltip("카메라의 기본 회전값을 보정하는 추가 각도 (고정된 시점에서 미세 조정 가능)")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("카메라 회전 속도")]
        public float DeltaTimeMultiplier = 1f;

        [Tooltip("카메라의 위치를 모든 축에서 고정할지 여부")] public bool LockCameraPosition = false;
        
        // Cinemachine 관련 변수
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        
        
        private const float _threshold = 0.01f; // 작은 입력값 무시하는 임계값
        

        private void Start()
        {
            // 게임 시작 시 카메라의 초기 (좌우 회전)Yaw 값을 가져와서 설정
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
        }
        
        
        public void CameraRotation(Vector2 look)
        {
            // 입력 값이 존재하고, 카메라 위치가 고정되지 않은 경우 실행
            if (look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {

                // 입력된 값(look)을 기반으로 카메라의 Yaw(좌우 회전) 및 Pitch(상하 회전) 값을 업데이트
                _cinemachineTargetYaw += look.x * DeltaTimeMultiplier;
                _cinemachineTargetPitch -= look.y * DeltaTimeMultiplier;
            }

            // 회전 값이 일정 범위를 넘지 않도록 제한 (Yaw는 제한 없이 회전 가능, Pitch는 위/아래 범위 설정)
            _cinemachineTargetYaw = Util.ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = Util.ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine이 따라갈 대상 오브젝트(CinemachineCameraTarget)의 회전을 설정
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride, // 상하 회전 (Pitch)
                _cinemachineTargetYaw,                         // 좌우 회전 (Yaw)
                0.0f                                           // Z축 회전은 없음
            );
        }
    }
}