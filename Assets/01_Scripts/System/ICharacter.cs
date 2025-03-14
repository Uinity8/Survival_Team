using System;
using UnityEngine;

namespace _01_Scripts.System
{
    // ICharacter: 캐릭터의 움직임, 액션, 애니메이션 등의 시스템 동작을 정의하는 인터페이스.
    public interface ICharacter
    {
        /// <summary>
        /// 액션 수행 시 적용되는 중력 값
        /// </summary>
        public float Gravity { get; }

        /// <summary>
        /// 캐릭터 컨트롤러의 애니메이터
        /// </summary>
        public Animator Animator { get; set; }

        /// <summary>
        /// true일 경우, 애니메이션의 루트 모션이 캐릭터에 적용됨
        /// </summary>
        public bool UseRootMotion { get; set; }

        /// <summary>
        /// 플레이어의 입력 방향을 반환하는 속성
        /// 점프 또는 특정 액션을 수행할 때 방향을 결정하는 데 사용됨
        /// </summary>
        public Vector3 MoveDir { get; }

        /// <summary>
        /// 플레이어의 발이 지면에 닿아 있을 경우 true를 반환하는 속성
        /// 해당 속성이 true일 때만 액션을 수행할 수 있음
        /// </summary>
        public bool IsGrounded { get; }

        /// <summary>
        /// 캐릭터가 특정 액션을 수행하지 못하도록 막고 싶을 때 true를 반환하는 속성
        /// 공격, 사격, 재장전 등 다른 액션을 수행하는 동안 사용할 수 있음
        /// </summary>
        public bool PreventAllSystems { get; set; }

        /// <summary>
        /// 파쿠르 또는 등반 액션을 시작할 때 호출되는 함수
        /// 이 함수에서 플레이어의 콜라이더를 비활성화하거나,
        /// 걷기 및 달리기 애니메이션 매개변수를 초기화할 수 있음
        /// </summary>
        void OnStartSystem(SystemBase systemBase = null);

        /// <summary>
        /// 액션이 완료되었을 때 호출되는 함수
        /// 이 함수에서 플레이어의 콜라이더를 다시 활성화하는 등의 작업을 수행할 수 있음
        /// </summary>
        void OnEndSystem(SystemBase systemBase = null);
    }

    // IDamageable: 캐릭터의 체력 및 피해 처리 로직을 정의하는 인터페이스.
    public interface IDamageable
    {
        /// <summary>
        /// 캐릭터의 체력 값
        /// </summary>
        public float Health { get; set; }

        /// <summary>
        /// 받는 피해량에 적용되는 배율
        /// </summary>
        public float DamageMultiplier { get; }

        /// <summary>
        /// 캐릭터가 공격을 받을 때 호출되는 이벤트
        /// - Vector3: 공격이 들어온 방향
        /// - float: 피해량
        /// </summary>
        public Action<Vector3, float> OnHit { get; set; }

        /// <summary>
        /// 부모 오브젝트의 IDamageable 인터페이스를 반환 (예: 장갑을 착용한 경우 본체의 체력 시스템을 참조할 수 있음)
        /// </summary>
        public IDamageable Parent { get; }
    }
}