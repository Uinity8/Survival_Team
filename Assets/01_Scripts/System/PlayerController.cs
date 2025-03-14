using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _01_Scripts.Third_Person_Controller; // 3인칭 컨트롤러 관련 스크립트 네임스페이스
using UnityEngine;

namespace _01_Scripts.System
{
    /// <summary>
    /// 플레이어의 상태를 나타내는 주요 시스템 상태 열거형
    /// </summary>
    public enum SystemState
    {
        Locomotion,      // 기본 이동 상태 (걷기, 달리기 등)
        Parkour,         // 파쿠르 상태 (벽 넘기, 장애물 넘기 등)
        Climbing,        // 등반 상태
        Combat,          // 전투 상태 (근접 공격 등)
        GrapplingHook,   // 그래플링 훅 사용 상태
        Swing,           // 그네나 줄타기 상태
        Shooter,         // 사격 상태 (원거리 무기 사용)
        Other            // 기타 상태
    }

    /// <summary>
    /// 하위 시스템 상태 열거형
    /// </summary>
    public enum SubSystemState
    {
       None,      // 하위 시스템 없음
       Combat,    // 전투 관련 하위 상태
       Climbing,  // 등반 관련 하위 상태
       Other      // 기타 하위 상태
    }

    /// <summary>
    /// 플레이어의 여러 시스템(이동, 전투, 등반 등)을 관리하는 컨트롤러 클래스
    /// 이 클래스는 플레이어의 상태 전환, 카메라 제어, 애니메이션 루트 모션 적용 등 여러 기능을 담당함.
    /// </summary>
    [DefaultExecutionOrder(-20)] // 다른 스크립트보다 우선적으로 실행되도록 순서를 지정
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// 관리할 시스템 스크립트들의 목록
        /// 예) 이동 시스템, 전투 시스템, 등반 시스템 등
        /// </summary>
        public List<SystemBase> managedScripts = new List<SystemBase>();

        /// <summary>
        /// 현재 활성화된 시스템 상태 (예: Locomotion, Combat 등)
        /// </summary>
        [field: SerializeField]
        public SystemState CurrentSystemState { get; private set; }

        /// <summary>
        /// 이전에 활성화되었던 시스템 상태 (상태 전환 시 참조됨)
        /// </summary>
        public SystemState PreviousSystemState { get; private set; }

        /// <summary>
        /// 기본 시스템 상태. 일반적으로 Locomotion 상태로 설정됨.
        /// </summary>
        public SystemState DefaultSystemState => SystemState.Locomotion; 

        /// <summary>
        /// 현재 포커스된(우선순위가 높은) 시스템의 상태.
        /// 만약 포커스된 시스템이 없다면 기본 시스템 상태를 반환.
        /// </summary>
        public SystemState FocusedSystemState => FocusedScript == null ? DefaultSystemState : FocusedScript.State;

        /// <summary>
        /// 시스템 상태를 변경하는 메서드.
        /// 상태가 변경되면 이전 상태에 해당하는 스크립트의 OnStateExited 이벤트와
        /// 새 상태에 해당하는 스크립트의 OnStateEntered 이벤트를 실행함.
        /// </summary>
        /// <param name="newState">새로운 시스템 상태</param>
        public void SetSystemState(SystemState newState)
        {
            PreviousSystemState = CurrentSystemState;
            CurrentSystemState = newState;
            if (PreviousSystemState != CurrentSystemState) {
                // 이전 상태에 해당하는 스크립트의 종료 이벤트 호출
                managedScripts.FirstOrDefault((s) => s.State == PreviousSystemState)?.OnStateExited?.Invoke();
                // 새 상태에 해당하는 스크립트의 시작 이벤트 호출
                managedScripts.FirstOrDefault((s) => s.State == CurrentSystemState)?.OnStateEntered?.Invoke();
            }
        }

        /// <summary>
        /// 시스템 상태를 기본 상태(Locomotion)로 초기화하는 메서드.
        /// 상태 전환 시 관련 이벤트를 발생시킴.
        /// </summary>
        public void ResetState()
        {
            PreviousSystemState = CurrentSystemState;
            CurrentSystemState = DefaultSystemState;
            if (PreviousSystemState != CurrentSystemState)
            {
                managedScripts.FirstOrDefault((s) => s.State == PreviousSystemState)?.OnStateExited?.Invoke();
                managedScripts.FirstOrDefault((s) => s.State == CurrentSystemState)?.OnStateEntered?.Invoke();
            }
        }

        /// <summary>
        /// 이전 시스템 상태로 되돌리는 메서드.
        /// </summary>
        public void PreviousState()
        {
            CurrentSystemState = PreviousSystemState;
        }

        /// <summary>
        /// 시스템 시작을 잠시 대기시킬지 여부를 나타내는 속성.
        /// 대기 상태일 경우, 시스템 시작을 지연함.
        /// </summary>
        public bool WaitToStartSystem { get; set; } = false;

        /// <summary>
        /// 새로운 시스템 스크립트를 등록하는 메서드.
        /// 등록 시 중복 등록을 방지하며, 필요하면 우선순위에 따라 정렬함.
        /// </summary>
        /// <param name="script">등록할 시스템 스크립트</param>
        /// <param name="reorderScripts">우선순위에 따라 목록을 재정렬할지 여부</param>
        public void Register(SystemBase script, bool reorderScripts = false)
        {
            if (!managedScripts.Contains(script))
                managedScripts.Add(script);

            if (reorderScripts)
            {
                // Priority가 높은 스크립트부터 실행되도록 내림차순 정렬
                managedScripts = managedScripts.OrderByDescending(x => x.Priority).ToList();
            }
        }

        /// <summary>
        /// 등록된 시스템 스크립트를 목록에서 제거하는 메서드.
        /// </summary>
        /// <param name="script">제거할 시스템 스크립트</param>
        public void Unregister(SystemBase script)
        {
            managedScripts.Remove(script);
        }

        /// <summary>
        /// 카메라의 평면(수평) 회전 값을 반환.
        /// 카메라의 forward 방향 벡터에서 Y축 성분은 제거하여, 수평 회전만 계산.
        /// </summary>
        public Quaternion CameraPlanarRotation { get => Quaternion.LookRotation(Vector3.Scale(CameraGameObject.transform.forward, new Vector3(1, 0, 1))); }

        /// <summary>
        /// 메인 카메라의 게임 오브젝트.
        /// Awake에서 Camera.main을 통해 할당됨.
        /// </summary>
        public GameObject CameraGameObject { get; set; }

        /// <summary>
        /// 플레이어의 Animator 컴포넌트.
        /// ICharacter 인터페이스를 통해 할당됨.
        /// </summary>
        public Animator Animator { get; set; }

        /// <summary>
        /// ICharacter 인터페이스를 구현하는 플레이어 객체.
        /// 플레이어의 애니메이터, 입력 처리 등 여러 기능을 포함.
        /// </summary>
        public ICharacter Player { get; set; }

        /// <summary>
        /// 카메라 흔들림 효과를 시작할 때 사용하는 델리게이트.
        /// 첫 번째 인자와 두 번째 인자는 흔들림 강도 및 지속시간 등으로 해석될 수 있음.
        /// </summary>
        public Action<float, float> OnStartCameraShake;

        /// <summary>
        /// 커스텀 카메라 상태를 설정할 때 사용하는 델리게이트.
        /// CameraSettings 타입의 데이터를 인자로 받음.
        /// </summary>
        public Action<CameraSettings> SetCustomCameraState;

        /// <summary>
        /// 카메라 반동 효과를 적용할 때 사용하는 델리게이트.
        /// RecoilInfo 타입의 데이터를 인자로 받음.
        /// </summary>
        public Action<RecoilInfo> CameraRecoil;

        /// <summary>
        /// 카메라 반동 효과 관련 정보를 담은 클래스.
        /// [Serializable] 어트리뷰트를 통해 에디터에서 값 설정 가능.
        /// </summary>
        [Serializable]
        public class RecoilInfo
        {
            public Vector2 CameraRecoilAmount = new Vector2(0.3f, 2); // 반동의 크기 (X, Y)
            public float CameraRecoilDuration = 0.3f;                  // 반동 효과 지속 시간
            [Range(0.001f, 1)]
            public float recoilPhasePercentage = 0.2f;                // 반동 효과 단계 비율 (예: 초기 반동, 회복 등)
            public Vector2 minRecoilAmount = new Vector2(0.2f, 1);      // 최소 반동 크기
        }

        /// <summary>
        /// 캐릭터가 착지할 때 호출되는 이벤트.
        /// 두 개의 float 인자를 받아, 착지 시 효과를 처리할 수 있음.
        /// </summary>
        public Action<float, float> OnLand;

        /// <summary>
        /// 캐릭터가 공중에 있는지 여부.
        /// 공중 상태이면 IsInAir가 true로 설정됨.
        /// </summary>
        public bool IsInAir { get; set; }

        /// <summary>
        /// 플레이어의 회전을 제한할지 여부.
        /// true일 경우, 회전 관련 처리를 중지함.
        /// </summary>
        public bool PreventRotation { get; set; }

        /// <summary>
        /// 가장자리(ledge)에서 떨어지는 것을 방지할지 여부.
        /// true이면 해당 기능이 활성화됨.
        /// </summary>
        public bool PreventFallingFromLedge { get; set; } = true;


        /// <summary>
        /// MonoBehaviour의 Awake() 메서드.
        /// 컴포넌트 초기화 및 필요한 참조를 할당하고,
        /// 관리되는 모든 시스템 스크립트의 HandleAwake()를 호출함.
        /// </summary>
        void Awake()
        {
            // ICharacter 인터페이스를 구현한 컴포넌트를 가져옴
            Player = GetComponent<ICharacter>();

            // 메인 카메라가 존재하는 경우 카메라 게임 오브젝트를 할당
            if (Camera.main != null)
                CameraGameObject = Camera.main.gameObject;

            // ICharacter 인터페이스에서 Animator를 가져옴
            Animator = Player.Animator;

            // 등록된 모든 시스템 스크립트에 대해 Awake 단계 초기화 함수 호출
            foreach (var script in managedScripts)
            {
                script?.HandleAwake();
            }
        }

        /// <summary>
        /// MonoBehaviour의 Start() 메서드.
        /// 관리되는 모든 시스템 스크립트의 HandleStart()를 호출하여 초기화를 수행함.
        /// </summary>
        void Start()
        {
            foreach (var script in managedScripts)
            {
                script?.HandleStart();
            }
        }

        /// <summary>
        /// 현재 포커스된 시스템 스크립트를 반환.
        /// 포커스된 스크립트는 IsInFocus 속성이 true로 설정된 스크립트임.
        /// </summary>
        public SystemBase FocusedScript { get => managedScripts.FirstOrDefault(x => x.IsInFocus); }

        /// <summary>
        /// MonoBehaviour의 FixedUpdate() 메서드.
        /// 물리 연산과 관련된 업데이트를 관리되는 시스템 스크립트에 위임함.
        /// </summary>
        void FixedUpdate()
        {
            // 플레이어가 모든 시스템의 동작을 막은 경우 업데이트를 중단
            if (Player.PreventAllSystems) return;

            // 포커스된 시스템이 있다면 해당 시스템의 FixedUpdate()를 호출,
            // 없으면 모든 활성화된 시스템 스크립트에 대해 FixedUpdate() 호출
            var focusedScript = FocusedScript;
            if (focusedScript)
                focusedScript.HandleFixedUpdate();
            else
                foreach (var script in managedScripts)
                {
                    if (script.enabled)
                        script.HandleFixedUpdate();
                }
        }

        /// <summary>
        /// MonoBehaviour의 Update() 메서드.
        /// 일반 업데이트를 관리되는 시스템 스크립트에 위임함.
        /// </summary>
        void Update()
        {
            // 플레이어가 모든 시스템의 동작을 막은 경우 업데이트를 중단
            if (Player.PreventAllSystems) return;

            // 포커스된 시스템이 있다면 해당 시스템의 Update()를 호출,
            // 없으면 모든 활성화된 시스템 스크립트에 대해 Update() 호출
            var focusedScript = FocusedScript;
            if (focusedScript)
            {
                focusedScript.HandleUpdate();
            }
            else
                foreach (var script in managedScripts)
                {
                    if (script.enabled)
                        script.HandleUpdate();
                }
        }

        /// <summary>
        /// 애니메이터의 루트 모션(Root Motion)을 적용하는 메서드.
        /// 플레이어의 위치와 회전을 애니메이터에서 계산된 delta 값으로 업데이트함.
        /// </summary>
        void OnAnimatorMove()
        {
            // 플레이어가 루트 모션을 사용하도록 설정되어 있다면
            if (Player.UseRootMotion)
            {
                var focusedScript = FocusedScript;
                // 포커스된 시스템이 존재하면 해당 시스템에서 루트 모션 처리
                if (focusedScript)
                {
                    focusedScript.HandleOnAnimatorMove(Animator);
                    return;
                }

                // 포커스된 시스템이 없을 경우, 기본적으로 애니메이터의 deltaPosition과 deltaRotation을 적용
                if (Animator.deltaPosition != Vector3.zero)
                    transform.position += Animator.deltaPosition;
                transform.rotation *= Animator.deltaRotation;
            }
        }

        /// <summary>
        /// 시스템 시작 시 호출되는 코루틴.
        /// Player의 OnStartSystem()을 호출한 후, WaitToStartSystem이 true이면 시작을 대기함.
        /// </summary>
        /// <param name="system">시작할 시스템 스크립트</param>
        /// <returns>IEnumerator 코루틴</returns>
        public IEnumerator OnStartSystem(SystemBase system)
        {
            Player.OnStartSystem(system);
            if (WaitToStartSystem)
                yield return new WaitUntil(() => WaitToStartSystem == false);
        }

        /// <summary>
        /// 모든 시스템의 포커스를 해제하는 메서드.
        /// 각 시스템이 포커스 상태일 경우, Player의 OnEndSystem()을 호출하여 종료 처리함.
        /// </summary>
        public void UnfocusAllSystem()
        {
            foreach (var system in managedScripts)
            {
                if (system.IsInFocus)
                    Player.OnEndSystem(system);
            }
        }

        /// <summary>
        /// OnGUI() 메서드.
        /// 현재 FocusedSystemState를 화면에 표시하는 GUI 코드 
        /// </summary>
        private void OnGUI()
        {
            // 예시: 현재 시스템 상태를 화면에 출력
            // GUILayout.Label(FocusedSystemState.ToString(), new GUIStyle() { fontSize = 24 });
        }
    }
}