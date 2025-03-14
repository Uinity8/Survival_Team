using System;
using UnityEngine;

namespace _01_Scripts.System
{
    /// <summary>
    /// 시스템의 기본 기능을 제공하는 베이스 클래스.
    /// 이 클래스는 각종 액션(예: 이동, 공격, 상태 전환 등)을 위한 공통 인터페이스 및 기본 동작을 포함하며,
    /// 하위 클래스에서 필요한 동작을 오버라이드하여 구현할 수 있음.
    /// </summary>
    public class SystemBase : MonoBehaviour
    {
        /// <summary>
        /// MonoBehaviour의 Awake() 단계에서 호출될 기본 초기화 메서드.
        /// 하위 클래스에서 필요한 초기화 작업을 위해 오버라이드 할 수 있음.
        /// 기본 구현은 아무런 동작도 수행하지 않음.
        /// </summary>
        public virtual void HandleAwake() { }

        /// <summary>
        /// MonoBehaviour의 Start() 단계에서 호출될 기본 초기화 메서드.
        /// 하위 클래스에서 필요한 초기화 작업을 위해 오버라이드 할 수 있음.
        /// 기본 구현은 아무런 동작도 수행하지 않음.
        /// </summary>
        public virtual void HandleStart() { }

        /// <summary>
        /// MonoBehaviour의 FixedUpdate() 단계에서 호출될 물리 관련 업데이트 메서드.
        /// 하위 클래스에서 물리 연산 또는 관련 동작을 구현할 때 오버라이드 할 수 있음.
        /// </summary>
        public virtual void HandleFixedUpdate() { }

        /// <summary>
        /// MonoBehaviour의 Update() 단계에서 호출될 일반 업데이트 메서드.
        /// 하위 클래스에서 프레임마다 수행할 작업을 구현할 때 오버라이드 할 수 있음.
        /// </summary>
        public virtual void HandleUpdate() { }

        /// <summary>
        /// 애니메이터의 루트 모션(Root Motion) 데이터를 적용하는 메서드.
        /// Animator가 제공하는 deltaPosition(위치 변화)과 deltaRotation(회전 변화)을
        /// 현재 GameObject의 transform에 적용함.
        /// </summary>
        /// <param name="animator">애니메이터 컴포넌트. 루트 모션 데이터 제공원.</param>
        public virtual void HandleOnAnimatorMove(Animator animator)
        {
            // 애니메이터에서 위치 변화 값이 있다면, 이를 현재 오브젝트의 위치에 더함.
            if (animator.deltaPosition != Vector3.zero)
                transform.position += animator.deltaPosition;
            // 애니메이터에서 회전 변화 값을 현재 오브젝트의 회전에 곱하여 적용함.
            transform.rotation *= animator.deltaRotation;
        }

        /// <summary>
        /// 시스템의 우선순위를 나타내는 프로퍼티.
        /// 값이 클수록 높은 우선순위를 의미하며, 시스템 간 업데이트 순서 결정에 사용됨.
        /// 기본값은 0이며, 하위 클래스에서 필요에 따라 오버라이드 가능.
        /// </summary>
        public virtual float Priority { get; set; } = 0;

        /// <summary>
        /// 이 시스템이 현재 포커스(집중) 상태인지 여부를 나타내는 프로퍼티.
        /// true이면 이 시스템만 업데이트되고, 다른 시스템의 업데이트는 무시됨.
        /// </summary>
        public bool IsInFocus { get; set; }

        /// <summary>
        /// 현재 시스템의 상태를 나타내는 프로퍼티.
        /// SerializeField로 표시되어 있어 에디터에서 확인 가능하며, 기본값은 SystemState.Other.
        /// 하위 클래스에서 상태를 변경하여 사용할 수 있음.
        /// </summary>
        public virtual SystemState State { get; } = SystemState.Other;

        /// <summary>
        /// 현재 시스템의 하위 상태를 나타내는 프로퍼티.
        /// 기본값은 SubSystemState.None이며, 필요한 경우 하위 클래스에서 오버라이드하여 사용.
        /// </summary>
        public virtual SubSystemState SubState { get; } = SubSystemState.None;

        /// <summary>
        /// 이 시스템 스크립트만 활성화하고, 다른 스크립트의 업데이트를 무시하도록 설정하는 메서드.
        /// IsInFocus를 true로 설정하여 포커스 상태로 만듦.
        /// </summary>
        public void FocusScript() => IsInFocus = true;

        /// <summary>
        /// 포커스 상태를 해제하여 이 스크립트의 업데이트가 일반 업데이트 흐름에 합류하도록 하는 메서드.
        /// IsInFocus를 false로 설정함.
        /// </summary>
        public void UnFocusScript() => IsInFocus = false;

        /// <summary>
        /// SystemBase 클래스 타입을 저장하는 변수.
        /// 주로 리플렉션을 사용하여 메서드 오버라이드 여부를 확인하는 데 사용됨.
        /// </summary>
        Type SystemBaseType = typeof(SystemBase);

        /// <summary>
        /// 전달받은 메서드 이름이 이 클래스에서 오버라이드되었는지 확인하는 메서드.
        /// SystemBaseType을 사용하여 기본 클래스의 메서드와 비교함.
        /// </summary>
        /// <param name="methodName">확인할 메서드의 이름</param>
        /// <returns>
        /// 오버라이드된 경우 true를 반환하며, 그렇지 않으면 false를 반환.
        /// </returns>
        public bool HasOverrode(string methodName) => SystemBaseType.GetMethod(methodName)?.DeclaringType != SystemBaseType;

        /// <summary>
        /// 시스템이 활성화(시작)될 때 호출되는 메서드.
        /// 하위 클래스에서 시스템 활성화 시 필요한 초기화 또는 상태 변경 작업을 구현할 수 있음.
        /// </summary>
        public virtual void EnterSystem() { }

        /// <summary>
        /// 시스템이 비활성화(종료)될 때 호출되는 메서드.
        /// 하위 클래스에서 시스템 종료 시 필요한 정리 작업을 구현할 수 있음.
        /// </summary>
        public virtual void ExitSystem() { }

        /// <summary>
        /// 시스템 상태가 활성화되었을 때 호출되는 이벤트.
        /// 이 이벤트는 시스템이 시작될 때 외부에서 추가 작업을 실행할 수 있도록 함.
        /// </summary>
        public Action OnStateEntered { get; set; }

        /// <summary>
        /// 시스템 상태가 종료되었을 때 호출되는 이벤트.
        /// 이 이벤트는 시스템이 종료될 때 외부에서 추가 작업을 실행할 수 있도록 함.
        /// </summary>
        public Action OnStateExited { get; set; }
    }
}