using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _01_Scripts.Core;
using _01_Scripts.Utilities;
using AnimatorHash;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace _01_Scripts.Combat
{
    // 공격 상태를 나타내는 Enum
    public enum AttackStates
    {
        Idle, // 대기 상태
        Windup, // 공격 준비 상태
        Impact, // 공격 타격 발생
        Cooldown // 공격 후 쿨다운
    }

    // 캐릭터의 전체적인 상태를 나타내는 Enum
    public enum FighterState
    {
        None, // 기본 상태 (아무 동작 없음)
        Attacking, // 공격 중
        Blocking, // 방어 중
        Dodging, // 회피 중
        TakingHit, // 피격 중
        TakingBlockedHit, // 방어 중 피격
        KnockedDown, // 넘어짐 상태
        GettingUp, // 일어나는 상태
        SwitchingWeapon, // 무기 변경 중
        Dead, // 사망 상태
        Taunt, // 도발 상태
        Other // 기타 커스텀 상태
    }

    public class MeleeFighter : MonoBehaviour
    {
        // 전투 상태를 나타내는 bool 값입니다. true면 이동이 멈춥니다.
        public bool StopMovement { get; set; }

        
        [Tooltip("전투 시작 시 사용하는 기본 무기")]
        public WeaponData weapon;
        
        [field: SerializeField]
        [Tooltip("캐릭터의 최대 체력 (초기값: 25)")]
        public float MaxHealth { get; private set; } = 25f;

        // 공격 중 회전 속도
        [SerializeField] [Tooltip("공격 중 캐릭터의 회전 속도")]
        private float rotationSpeedDuringAttack = 500f;

        // 추가적인 선택적 매개변수
        [Header("Optional Parameters")] [Tooltip("기본 반응 애니메이션 세트")]
        public DefaultReactions defaultAnimations = new DefaultReactions();

        // 캐릭터에 부착된 무기 리스트
        [SerializeField] [Tooltip("전투 중 캐릭터에 장착된 무기 리스트")]
        private List<AttachedWeapon> attachedWeapons = new List<AttachedWeapon>();

        // 현재 체력 값입니다. MaxHealth와의 비율을 통해 체력 상태를 나타냅니다.
        public float CurrentHealth { get; private set; }

        // 전투의 최대 공격 범위. 자동 계산됨.
        public float MaxAttackRange { get; private set; }

        
        [Tooltip("현재 전투 상태")]
        public FighterState State { get; private set; }

        // 전투기의 공격 상태를 나타냅니다.
        [Tooltip("현재 공격 상태")] public AttackStates AttackState { get; private set; }

        // 캐릭터가 행동 중인지 나타냅니다. (Idle, None, Blocking 상태가 아니면 true)
        public bool InAction => State != FighterState.None && State != FighterState.Blocking;

        // 캐릭터가 사망했는지 확인합니다.
        public bool IsDead => State == FighterState.Dead;

        // 캐릭터가 작업 중이거나 사망 상태인지 확인합니다.
        public bool IsBusy => InAction || IsDead;

        // 캐릭터가 '기절 상태'에 있는지 확인합니다.
        public bool IsKnockedDown => State == FighterState.KnockedDown ||
                                     (State == FighterState.TakingHit && prevState == FighterState.KnockedDown);

        // 카운터 공격이 가능한지 여부를 나타냅니다.
        public bool IsCountable => AttackState == AttackStates.Windup &&
                                   (!CombatSettings.Instance.OnlyCounterFirstAttackOfCombo || comboCount == 0);

        // 목표에 매칭 중인지 확인합니다.
        public bool IsMatchingTarget { get; private set; }

        // 현재 동기화된 애니메이션 상태인지 확인 (다른 공격 중간 삽입 방지)
         public bool IsInSyncedAnimation { get; private set; }

        // 현재 동기화된 동작 데이터를 저장합니다.
        public AttackData CurrSyncedAction { get; private set; } // 중복 행동 방지용

        // 외부로부터 공격을 받을 수 있는지 여부
        public bool CanTakeHit { get; set; } = true;

        // 무적 상태 여부
        public bool IsInvincible { get; set; }

        // 블로킹 여부 관리
         private bool isBlocking;

        public bool IsBlocking
        {
            get => isBlocking;
            set
            {
                // 이전 상태와 변경될 상태 비교
                bool wasPreviouslyBlocking = isBlocking;
                isBlocking = value;
                HandleBlockingChanged(wasPreviouslyBlocking);
            }
        }

        // 현재 공격 대상으로 설정된 전투 상대입니다.
        public MeleeFighter Target { get; set; }

        // 현재 공격 중인 대상을 나타냅니다.
        private MeleeFighter attackingTarget;

        // 공격받고 있는 상태인지 여부를 확인합니다.
        public bool IsBeingAttacked { get; private set; } = false;

        // 현재 캐릭터를 공격 중인 상대를 나타냅니다.
        public MeleeFighter CurrAttacker { get; private set; }

        // 현재 장착된 무기를 나타냅니다.
        public WeaponData CurrentWeapon { get; set; }

        // 현재 장착된 무기의 게임 오브젝트를 참조합니다.
        public GameObject CurrentWeaponObject { get; set; }

        // 장착된 무기를 다루는 핸들러를 참조합니다.
        public AttachedWeapon CurrentWeaponHandler { get; set; }

        // 현재 목표와의 위치 차이를 나타냅니다.
        public Vector3 MatchingTargetDeltaPos { get; private set; } = Vector3.zero;

        // 캡슐형 충돌체를 나타냅니다 (캐릭터용 충돌 처리).
        [SerializeField] [Tooltip("캐릭터의 기본 캡슐 충돌체")]
        private CapsuleCollider capsuleCollider;

        // 무기 충돌체를 나타냅니다.
        [SerializeField] [Tooltip("무기를 위한 박스 충돌체")]
        private BoxCollider weaponCollider;

        // 신체 부위별 충돌체를 나타냅니다 (손, 발, 팔꿈치, 무릎, 머리 등).
        [SerializeField] [Tooltip("왼손 충돌체")] private BoxCollider leftHandCollider, rightHandCollider;
        [SerializeField] [Tooltip("양발 충돌체")] private BoxCollider leftFootCollider, rightFootCollider;
        [SerializeField] [Tooltip("팔꿈치 충돌체")] private BoxCollider leftElbowCollider, rightElbowCollider;
        [SerializeField] [Tooltip("무릎 충돌체")] private BoxCollider leftKneeCollider, rightKneeCollider;
        [SerializeField] [Tooltip("머리 충돌체")] private BoxCollider headCollider;

        // 현재 활성화된 충돌체를 나타냅니다.
        [Tooltip("현재 활성 상태의 충돌체")] private BoxCollider activeCollider;

        // 이전 충돌체의 위치를 저장합니다.
        [Tooltip("이전 충돌체의 위치")] private Vector3 prevColliderPos;

        // 이전에 활성화된 게임 오브젝트를 추적합니다.
        [Tooltip("이전에 활성화된 게임 오브젝트")] private GameObject prevGameObj;

        // 애니메이션을 제어하는 Animator 컴포넌트를 참조합니다.
        [SerializeField] [Tooltip("Animator 컴포넌트")]
        private Animator animator;

        // 애니메이션 그래프를 나타냅니다.
        [SerializeField] [Tooltip("애니메이션 그래프 관리")]
        private AnimGraph animGraph;

        // 기본 애니메이터 컨트롤러를 참조합니다.
        [SerializeField] [Tooltip("기본 애니메이터 컨트롤러")]
        private AnimatorOverrideController defaultAnimatorController;

        // 캐릭터 움직임을 제어하는 CharacterController를 참조합니다.
        [SerializeField] [Tooltip("캐릭터 움직임 제어 컴포넌트")]
        private CharacterController characterController;

        // 현재 콤보 중인지.
        private bool doCombo;

        // 현재 콤보 횟수를 저장합니다.
         private int comboCount;

        // 캐릭터에게 적이 때린 횟수를 저장합니다.
        private int hitCount;

        // 공격자가 회피할 수 있는지 나타냄
        [Tooltip("Indicates if the fighter can dodge.")] [HideInInspector]
        public bool CanDodge;

        // 회피 동작 데이터를 저장
        [HideInInspector] public DodgeData dodgeData;

        // 전투 모드에서만 회피 가능 여부를 나타냄
        [Tooltip("If true, fighter will only be able to dodge in combat mode.")]
        public bool OnlyDodgeInCombatMode = true;

        // 공격자가 구르기 동작을 할 수 있는지 나타냄
        [Tooltip("Indicates if the fighter can roll.")]
        public bool CanRoll;

        // 구르기 동작 데이터를 저장
        [HideInInspector] public DodgeData rollData;

        // 전투 모드에서만 구르기가 가능한지 여부를 나타냄
        [Tooltip("If true, fighter will only be able to roll in combat mode.")] 
        public bool OnlyRollInCombatMode = true;

        [Space(10)]

        // 무기 장착 시 발생하는 이벤트
        [HideInInspector]
        public UnityEvent<WeaponData, bool> OnWeaponEquipEvent;

        // 무기 해제 시 발생하는 이벤트
        [HideInInspector] public UnityEvent<WeaponData, bool> OnWeaponUnEquipEvent;

        // 무기 장착 시 호출되는 액션
        public Action<WeaponData, bool> OnWeaponEquipAction;

        // 무기 해제 시 호출되는 액션
        public Action<WeaponData, bool> OnWeaponUnEquipAction;

        // 피격 시 발생하는 이벤트
        public event Action<MeleeFighter, Vector3, float, bool> OnGotHit;

        [HideInInspector] public UnityEvent<MeleeFighter, Vector3, float> OnGotHitEvent;

        // 공격 시 발생하는 이벤트
        public event Action<MeleeFighter> OnAttack;

        [HideInInspector] public UnityEvent<MeleeFighter> OnAttackEvent;

        // 반격 사용 실패 시 발생하는 이벤트
        public event Action OnCounterMisused;

        [HideInInspector] public UnityEvent OnCounterMisusedEvent;

        // 행동 시작 시 호출되는 액션
        public Action OnStartAction;

        // 행동 종료 시 호출되는 액션
        public Action OnEndAction;

        // 피격 동작이 완료됐을 때 발생하는 이벤트
        public event Action OnHitComplete;

        // 캐릭터 사망 시 발생하는 이벤트
        public event Action OnDeath;

        [HideInInspector] public UnityEvent OnDeathEvent;

        // 넉다운 상태 발생 시 호출되는 이벤트
        public event Action OnKnockDown;

        [HideInInspector] public UnityEvent OnKnockDownEvent;

        // 일어나기 동작이 시작될 때 발생하는 이벤트
        public event Action OnGettingUp;

        [HideInInspector] public UnityEvent OnGettingUpEvent;

        // 전투자의 상태를 리셋할 때 발생하는 이벤트
        public event Action OnResetFighter;

        // 히트 활성화 시 수행하는 이벤트
        public event Action<AttachedWeapon> OnEnableHit;

        // 현재 공격 데이터를 포함
        public AttackData CurrAttack { get; private set; }

        // 현재 공격 슬롯 리스트
        public List<AttackSlot> CurrAttacksList { get; private set; }

        // 현재 공격 컨테이너
        public AttackContainer CurrAttackContainer { get; private set; }

        // 무기 교체 가능 여부를 나타냄
        public bool CanSwitchWeapon { get; set; } = true;

        // 블록 애니메이션이 이전에 실행 중인지를 나타냄
        public bool PlayingBlockAnimationEarlier { get; set; } = false;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animGraph = GetComponent<AnimGraph>();
            characterController = GetComponent<CharacterController>();
            defaultAnimatorController = new AnimatorOverrideController(animator.runtimeAnimatorController);
            capsuleCollider = GetComponent<CapsuleCollider>();

            Rigidbodies = GetComponentsInChildren<Rigidbody>().ToList();
            SetRagdollState(false);

            CurrentHealth = MaxHealth;
        }

        ///<summary>
        /// Start 메서드 초기화 작업:
        /// 기본 무기를 장비하고, 애니메이터를 통해 캐릭터의
        /// 손, 발, 팔꿈치, 무릎, 머리의 관련된 BoxCollider를 가져옵니다.
        /// 캐릭터 사망 시 OnDeathEvent를 호출합니다.
        ///</summary>
        private void Start()
        {
            // 기본 무기를 장비합니다.
            EquipDefaultWeapon(false);

            // 애니메이터에서 왼손, 오른손의 BoxCollider를 가져옵니다.
            leftHandCollider = animator.GetBoneTransform(HumanBodyBones.LeftHand)
                ?.GetComponentInChildren<BoxCollider>();
            rightHandCollider = animator.GetBoneTransform(HumanBodyBones.RightHand)
                ?.GetComponentInChildren<BoxCollider>();

            // 애니메이터에서 왼발, 오른발의 BoxCollider를 가져옵니다.
            leftFootCollider = animator.GetBoneTransform(HumanBodyBones.LeftFoot)
                ?.GetComponentInChildren<BoxCollider>();
            rightFootCollider = animator.GetBoneTransform(HumanBodyBones.RightFoot)
                ?.GetComponentInChildren<BoxCollider>();

            // 애니메이터에서 왼쪽, 오른쪽 팔꿈치의 BoxCollider를 가져옵니다.
            leftElbowCollider = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm)
                ?.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c => c.name == "LeftElbowCollider");
            rightElbowCollider = animator.GetBoneTransform(HumanBodyBones.RightLowerArm)
                ?.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c => c.name == "RightElbowCollider");

            // 애니메이터에서 왼쪽, 오른쪽 무릎의 BoxCollider를 가져옵니다.
            leftKneeCollider = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg)
                ?.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c => c.name == "LeftKneeCollider");
            rightKneeCollider = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg)
                ?.GetComponentsInChildren<BoxCollider>().FirstOrDefault(c => c.name == "RightKneeCollider");

            // 애니메이터에서 머리의 BoxCollider를 가져옵니다.
            headCollider = animator.GetBoneTransform(HumanBodyBones.Head)?.GetComponentInChildren<BoxCollider>();

            // 캐릭터 사망 시 OnDeathEvent를 호출합니다.
            OnDeath += () => OnDeathEvent.Invoke();
        }

        // For testing, remove later
        public bool IsPlayerForDebug { get; set; }

        ///<summary>
        /// 상대방을 공격 시도하는 메서드.
        /// 기본 공격, 강공격, 카운터, 차지, 특수 공격 등을 처리하며, 
        /// 공격 가능한 상태인지와 무기 상태를 확인하고 공격 동작을 수행합니다.
        ///</summary>
        /// <param name="target">대상 공격자 (null일 수 있음)</param>
        /// <param name="isHeavyAttack">강공격 여부</param>
        /// <param name="isCounter">카운터 공격 여부</param>
        /// <param name="isCharged">차지 공격 여부</param>
        /// <param name="isSpecialAttack">특수 공격 여부</param>
        public void TryToAttack(MeleeFighter target = null, bool isHeavyAttack = false, bool isCounter = false,
            bool isCharged = false, bool isSpecialAttack = false)
        {
            // 막힌 공격(TakingBlockedHit)은 카운터가 가능한 상태의 특수 케이스
            if (State != FighterState.TakingBlockedHit)
            {
                // 이미 다른 동작 중이거나, 동기화된 애니메이션 중이면 반환
                if (InAction && State != FighterState.Attacking) return;
                if (IsInSyncedAnimation || (target != null && target.IsInSyncedAnimation)) return;
            }
            else
            {
                // 카운터 공격이 아니거나 현재 무기로 카운터가 불가능하면 반환
                if (!isCounter || !CurrentWeapon.CanCounter) return;
            }

            // 무기가 없고 장비 가능한 무기가 있으면 장비 후 공격 처리
            if (CurrentWeapon == null && weapon != null)
            {
                EquipWeapon(weapon,
                    onComplete: () => HandleAttack(target, isHeavyAttack, isCounter, isCharged, isSpecialAttack));
            }
            // 무기가 현재 장비된 상태라면 바로 공격 처리
            else if (CurrentWeapon != null)
            {
                HandleAttack(target, isHeavyAttack, isCounter, isCharged, isSpecialAttack);
            }
        }

        ///<summary>
        /// 공격을 처리하는 메서드.
        /// 대상과 공격 유형(강공격, 카운터, 차지, 특수 공격)을 설정하고,
        /// 공격 가능한지를 판단한 뒤, 적절한 공격 동작(콤보, 조롱, 일반 공격 등)을 수행합니다.
        ///</summary>
        /// <param name="target">공격 대상</param>
        /// <param name="isHeavyAttack">강공격 여부</param>
        /// <param name="isCounter">카운터 공격 여부</param>
        /// <param name="isCharged">차지 공격 여부</param>
        /// <param name="isSpecialAttack">특수 공격 여부</param>
        void HandleAttack(MeleeFighter target = null, bool isHeavyAttack = false, bool isCounter = false,
            bool isCharged = false, bool isSpecialAttack = false)
        {
            // 공격 대상을 설정
            Target = target;

            // 공격 선택이 실패한 경우
            if (!ChooseAttacks(target, comboCount, isHeavyAttack: isHeavyAttack, isCounter: isCounter, isSpecialAttack))
            {
                // 카운터 실패 시 동작 처리
                if (isCounter &&
                    CurrentWeapon.PlayActionIfCounterMisused &&
                    CurrentWeapon.CounterMisusedAction != null &&
                    !InAction &&
                    Target != null)
                {
                    // 조롱 동작 실행
                    StartCoroutine(PlayTauntAction(CurrentWeapon.CounterMisusedAction));
                    OnCounterMisused?.Invoke(); // 카운터 실패 액션 호출
                    OnCounterMisusedEvent?.Invoke(); // 카운터 실패 이벤트 호출
                }

                return; // 공격 중단
            }

            // 차지 공격 입력을 설정
            isChargedInput = isCharged;

            // 행동 중이 아니거나, 방어 탄 후의 상태일 경우
            if (!InAction || State == FighterState.TakingBlockedHit)
            {
                // 공격 실행
                StartCoroutine(Attack(Target));
            }
            // 공격 상태가 Impact 또는 Cooldown인 경우
            else if (AttackState == AttackStates.Impact || AttackState == AttackStates.Cooldown)
            {
                // 카운터가 아니라면 콤보 플래그 설정
                if (!isCounter)
                    doCombo = true;
            }
        }


        ///<summary>
        /// 공격을 선택하는 메서드.
        /// 주어진 조건에 맞는 가능한 공격을 필터링하고, 후속 동작(카운터, 일반 공격 등)을 결정합니다.
        ///</summary>
        /// <param name="target">공격 대상</param>
        /// <param name="combo">활성화된 콤보의 순서</param>
        /// <param name="isHeavyAttack">강공격 여부</param>
        /// <param name="isCounter">카운터 공격 여부</param>
        /// <param name="isSpecialAttack">특수 공격 여부</param>
        /// <returns>선택된 공격 가능 여부</returns>
        public bool ChooseAttacks(MeleeFighter target = null, int combo = 0, bool isHeavyAttack = false,
            bool isCounter = false, bool isSpecialAttack = false)
        {
            // 현재 공격 리스트 초기화 또는 유지
            CurrAttacksList ??= new List<AttackSlot>();

            // 대상이 받을 수 없는 상태일 경우 공격 불가
            if (target != null && !target.CanTakeHit) return false;

            // 카운터 선택
            if (isCounter)
            {
                // 무기로 카운터가 불가능하면 공격 불가
                if (!CurrentWeapon.CanCounter) return false;

                // 카운터 가능한지 확인
                bool counterPossible = ChooseCounterAttacks(target);
                if (!counterPossible)
                    CurrAttacksList = new List<AttackSlot>(); // 카운터 불가 시 리스트 초기화

                return counterPossible;
            }

            // 무기의 공격 리스트가 없거나 비어 있으면 현재 공격 리스트 크기에 따라 반환
            if (CurrentWeapon.Attacks == null || CurrentWeapon.Attacks.Count <= 0) return CurrAttacksList.Count > 0;

            // 가능 공격 리스트 복사
            var possibleAttacks = CurrentWeapon.Attacks.ToList();
            if (isHeavyAttack) possibleAttacks = CurrentWeapon.HeavyAttacks.ToList(); // 강공격 필터
            if (isSpecialAttack) possibleAttacks = CurrentWeapon.SpecialAttacks.ToList(); // 특수 공격 필터

            // 일반 공격 필터링
            var normalAttacks = possibleAttacks
                .Where(a => a.AttackType is AttackType.Single or AttackType.Combo)
                .OrderBy(a => a.MinDistance)
                .ToList();

            // 동기화된 리액션과 피니셔가 없는 공격 필터
            var normalAttacksWithoutSyncedAndFinishers = normalAttacks
                .Where(a => a.AttackSlots.Any(s => !s.Attack.IsSyncedReaction && !s.Attack.IsFinisher))
                .ToList();

            // 콤보 상태인지 확인
            bool inCombo = State == FighterState.Attacking
                           && CurrAttacksList.Count > 0
                           && combo != CurrAttacksList.Count - 1;

            if (target != null && !target.IsDead)
            {
                // 대상이 블로킹 상태일 경우 동기화된 리액션 공격 제거
                if (target.IsBlocking)
                    possibleAttacks.RemoveAll(a =>
                        a.AttackSlots.Any(s => s.Attack.IsSyncedReaction && !s.Attack.IsUnblockableAttack));

                // 대상이 발견되지 않은 상태인지 확인
                bool attackerUndetected = target.Target == null;

                // 공격 유형 필터링
                if (attackerUndetected)
                    possibleAttacks = possibleAttacks.Where(a => a.AttackType == AttackType.Stealth).ToList(); // 은신 공격
                else if (target.IsKnockedDown)
                    possibleAttacks =
                        possibleAttacks.Where(a => a.AttackType == AttackType.GroundAttack).ToList(); // 눕혀진 대상 공격
                else
                    possibleAttacks = normalAttacks; // 일반 공격

                // 거리 및 대상 체력 기준 필터링
                float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
                possibleAttacks = possibleAttacks
                    .Where(c => distanceToTarget >= c.MinDistance && distanceToTarget <= c.MaxDistance
                                                                  && (target.CurrentHealth / target.MaxHealth) * 100 <=
                                                                  c.HealthThreshold)
                    .ToList();

                // 체력 기준으로 정렬
                possibleAttacks = possibleAttacks.OrderBy(a => a.HealthThreshold).ToList();
            }
            else
            {
                // If there is no target, choose the closest attack and don't choose synced attacks
                possibleAttacks = normalAttacksWithoutSyncedAndFinishers;
            }

            if (possibleAttacks.Count > 0)
            {
                // If a finisher is possible then choose it
                var possibleFinishers =
                    possibleAttacks.Where(c => c.AttackSlots.Any(a => a.Attack.IsFinisher)).ToList();
                if (possibleFinishers.Count > 0)
                {
                    CurrAttacksList = possibleFinishers[Random.Range(0, possibleFinishers.Count)].AttackSlots;
                    return true;
                }

                // If it's not a combo or if the target went down in the middle of a combo then change the attack
                if (!inCombo || (target != null && target.IsKnockedDown))
                {
                    float lowestHealthThreshold = possibleAttacks.First().HealthThreshold;
                    possibleAttacks = possibleAttacks
                        .Where(a => Mathf.Approximately(a.HealthThreshold, lowestHealthThreshold)).ToList();

                    CurrAttacksList = possibleAttacks[Random.Range(0, possibleAttacks.Count)].AttackSlots;
                    return true;
                }
            }
            else
            {
                // 대상이 없으면 가장 가까운 공격을 선택하며, 동기화된 공격은 제외
                possibleAttacks = normalAttacksWithoutSyncedAndFinishers;
            }

            if (possibleAttacks.Count > 0)
            {
                // 피니셔 공격이 가능하다면 피니셔를 선택
                var possibleFinishers = possibleAttacks
                    .Where(c => c.AttackSlots.Any(a => a.Attack.IsFinisher))
                    .ToList();

                if (possibleFinishers.Count > 0)
                {
                    CurrAttacksList = possibleFinishers[Random.Range(0, possibleFinishers.Count)].AttackSlots;
                    return true;
                }

                // 콤보 상태가 아니거나, 콤보 중 대상이 다운되었다면 공격 변경
                if (!inCombo || (target != null && target.IsKnockedDown))
                {
                    // 가장 낮은 체력 기준의 공격을 필터링
                    float lowestHealthThreshold = possibleAttacks.First().HealthThreshold;
                    possibleAttacks = possibleAttacks
                        .Where(a => Mathf.Approximately(a.HealthThreshold, lowestHealthThreshold))
                        .ToList();

                    CurrAttacksList = possibleAttacks[Random.Range(0, possibleAttacks.Count)].AttackSlots;
                    return true;
                }
            }
            else
            {
                // 콤보 상태가 아니거나, 콤보 중 대상이 다운되었다면 공격 변경 시도
                if (!inCombo || (target != null && target.IsKnockedDown))
                {
                    // 가능한 공격이 없으면 일반 공격을 시도
                    if (normalAttacksWithoutSyncedAndFinishers.Count > 0)
                    {
                        CurrAttacksList = normalAttacks.First().AttackSlots;
                        return true;
                    }
                    else
                    {
                        // 주어진 조건에 해당하는 가능한 공격이 없음을 경고
                        Debug.LogWarning("No possible attacks for the given range!");
                        CurrAttacksList = new List<AttackSlot>();
                        return false;
                    }
                }
            }

            // 현재 선택된 공격 리스트의 유효성 반환
            return CurrAttacksList.Count > 0;
        }

        ///<summary>
        /// 카운터 공격을 선택하는 메서드.
        /// 대상이 공격 가능한 상태인지, 현재 공격이 카운터 가능한지 등의 조건을 체크하여 카운터 공격을 설정합니다.
        ///</summary>
        /// <param name="target">공격 대상</param>
        /// <returns>카운터 공격 가능 여부</returns>
        public bool ChooseCounterAttacks(MeleeFighter target)
        {
            // 대상이 null이거나 이미 죽은 상태라면 카운터 불가
            if (target == null || target.IsDead) return false;

            // 현재 공격받고 있고, 공격자를 카운터 가능하며, (설정에 따라) 방어 중이어야 카운터 가능
            if (IsBeingAttacked && CurrAttacker.IsCountable &&
                (!CombatSettings.Instance.OnlyCounterWhileBlocking || WillBlockAttack(CurrAttacker.CurrAttack)))
            {
                Target = CurrAttacker;

                var currAttack = CurrAttacker.CurrAttack;

                // 현재 공격이 카운터 가능하고, 카운터 공격 리스트가 있을 때
                if (currAttack.CanBeCountered && currAttack.CounterAttacks.Count > 0)
                {
                    // 대상의 체력 기준으로 가능한 카운터 공격 필터링
                    var possibleCounters = currAttack.CounterAttacks
                        .Where(a => (target.CurrentHealth / target.MaxHealth) * 100 <= a.HealthThresholdForCounter)
                        .OrderBy(a => a.HealthThresholdForCounter)
                        .ToList();

                    // 가능한 카운터 공격이 없으면 반환
                    if (possibleCounters.Count == 0)
                        return false;

                    // 가장 낮은 체력 기준의 카운터 공격 선택
                    float lowestHealth = possibleCounters.First().HealthThresholdForCounter;
                    var counterAttack = possibleCounters
                        .Where(a => Mathf.Approximately(a.HealthThresholdForCounter, lowestHealth))
                        .ToList()
                        .GetRandom();

                    // 선택된 카운터 공격이 대상에게 치명적이거나 특정 상황에서는 실행하지 않음
                    if (target.CheckIfAttackKills(counterAttack.Attack, this) &&
                        !counterAttack.Attack.IsFinisher &&
                        !counterAttack.Attack.Reaction.willBeKnockedDown)
                        return false;

                    // 선택된 카운터 공격 슬롯 설정
                    var counterSlot = new AttackSlot()
                    {
                        Attack = counterAttack.Attack,
                        Container = new AttackContainer() { AttackType = AttackType.Single }
                    };

                    CurrAttacksList = new List<AttackSlot>() { counterSlot };
                    attackStartDelay = counterAttack.CounterStartTime;

                    return true;
                }
            }

            return false;
        }

        // 이동 위치
        Vector3 moveToPos = Vector3.zero;

        // 차지 입력 여부
        bool isChargedInput = false;

        // 공격 시작 딜레이
        float attackStartDelay = 0f;


        public float AttackTimeNormalized { get; private set; } // 공격 시간의 정규화된 값 

        public bool IgnoreCollisions { get; private set; } // 충돌 무시 여부

        ///<summary>
        /// 공격 코루틴.
        /// 지정된 대상에 대해 공격 시퀀스를 수행하며, 차지 공격, 블로킹 및 동기화된 리액션 등을 처리합니다.
        ///</summary>
        /// <param name="target">공격 대상</param>
        IEnumerator Attack(MeleeFighter target = null)
        {
            // 움직임 정지 플래그 초기화
            StopMovement = false;

            // 공격 시작 이벤트 호출
            OnStartAction?.Invoke();

            // 상태 변경: 공격 중
            SetState(FighterState.Attacking);

            // 공격 대상을 설정
            attackingTarget = target;

            // 공격 상태 설정: Windup
            AttackState = AttackStates.Windup;

            // 현재 공격 슬롯 및 공격 정보 가져오기
            var attackSlot = CurrAttacksList[comboCount];
            var attack = attackSlot.Attack;

            // 차지 입력 여부에 따라 차지 공격 설정
            if (isChargedInput && attackSlot.CanBeCharged && attackSlot.ChargedAttack != null)
                attack = attackSlot.ChargedAttack;

            // 차지 공격 여부 기록 및 차지 입력 초기화
            bool wasChargedAttack = isChargedInput;
            isChargedInput = false;

            // 현재 공격 및 컨테이너 설정
            CurrAttack = attack;
            CurrAttackContainer = attackSlot.Container;

            // 공격 시작 딜레이 처리
            if (attackStartDelay > 0f && target != null && target.CurrAttack != null)
            {
                // 대상의 공격 시간이 딜레이 시간 이상일 때까지 대기
                yield return new WaitUntil(() => target.AttackTimeNormalized >= attackStartDelay);
                attackStartDelay = 0f;
            }

            // 공격 이벤트 호출
            OnAttack?.Invoke(target);

            // 공격 방향 및 초기 값 설정
            var attackDir = transform.forward;
            Vector3 startPos = transform.position;
            Vector3 targetPos = Vector3.zero;
            Vector3 rootMotionScaleFactor = Vector3.one;

            // 블로킹 여부
            bool willAttackBeBlocked = false;

            // 동기화된 리액션 및 블로킹 관련 플래그
            bool syncedReactionPlayed = false;
            bool shouldStartBlockingEarlier = false;

            if (target != null)
            {
                // 공격이 차단될지 여부 확인
                willAttackBeBlocked = target.WillBlockAttack(attack);

                // 대상이 공격받음을 설정
                target.BeingAttacked(this);

                // 대상과의 벡터 계산 및 공격 방향 설정
                var vecToTarget = target.transform.position - transform.position;
                vecToTarget.y = 0;
                attackDir = vecToTarget.normalized;

                // 대상에게 이동 설정
                if (attack.MoveToTarget)
                {
                    // 공격 방향과 대상 거리 계산
                    targetPos = target.transform.position - attackDir * attack.DistanceFromTarget;

                    // 이동 타입이 RootMotion 스케일 조정인 경우
                    if (attack.MoveType == TargetMatchType.ScaleRootMotion)
                    {
                        // 시작 프레임과 끝 프레임의 위치 계산
                        var endFramePos = attack.RootCurves.GetPositionAtTime(attack.MoveEndTime * attack.Clip.Length);
                        var startFramePos =
                            attack.RootCurves.GetPositionAtTime(attack.MoveStartTime * attack.Clip.Length);

                        // 목적지 이동량 계산
                        var destinationDisplacement = targetPos - transform.position;
                        var rootMotionDisplacement = Quaternion.LookRotation(destinationDisplacement) *
                                                     (endFramePos - startFramePos);

                        // 0 값이 있을 경우 비율 계산에 문제를 일으킬 수 있으므로 보정
                        if (rootMotionDisplacement.x == 0) rootMotionDisplacement.x = 1;
                        if (rootMotionDisplacement.y == 0) rootMotionDisplacement.y = 1;
                        if (rootMotionDisplacement.z == 0) rootMotionDisplacement.z = 1;

                        // RootMotion과 대상 간 비율 계산
                        rootMotionScaleFactor = new Vector3(
                            attack.WeightMask.x * destinationDisplacement.x / rootMotionDisplacement.x,
                            attack.WeightMask.y * destinationDisplacement.y / rootMotionDisplacement.y,
                            attack.WeightMask.z * destinationDisplacement.z / rootMotionDisplacement.z
                        );
                    }

                    // 이동 위치와 충돌 무시 여부 설정
                    moveToPos = targetPos;
                    IgnoreCollisions = attack.IgnoreCollisions;

                    // 동기화 리액션 또는 동기화 블로킹 리액션일 경우 대상의 충돌 무시 설정
                    if (attack.IsSyncedReaction || attack.IsSyncedBlockedReaction)
                        target.IgnoreCollisions = attack.IgnoreCollisions;

                    // 이동 타입이 Snap일 경우
                    if (attack.MoveType == TargetMatchType.Snap)
                    {
                        // Snap 목표가 공격자인 경우
                        if (attack.SnapTarget == SnapTarget.Attacker)
                        {
                            if (attack.SnapType == SnapType.LocalPosition)
                                moveToPos = target.transform.TransformPoint(attack.LocalPosFromTarget);

                            // 공격자의 위치를 설정
                            transform.position = moveToPos;

                            // 물리 업데이트 대기
                            yield return new WaitForFixedUpdate();
                        }
                        // Snap 목표가 피해자인 경우
                        else if (attack.SnapTarget == SnapTarget.Victim)
                        {
                            if (attack.SnapType == SnapType.LocalPosition)
                                moveToPos = transform.TransformPoint(attack.LocalPosFromTarget);
                            else if (attack.SnapType == SnapType.Distance)
                                moveToPos = transform.position + attackDir * attack.DistanceFromTarget;

                            // 대상의 위치를 설정
                            target.transform.position = moveToPos;
                        }
                    }
                }

                // 공격 대상과의 거리가 최소 공격 거리보다 짧고 특정 조건을 만족할 경우
                if (vecToTarget.magnitude < CurrentWeapon.MinAttackDistance &&
                    !attack.IsSyncedReaction &&
                    !attack.IsSyncedBlockedReaction &&
                    !attack.MoveToTarget)
                {
                    // 대상 캐릭터를 후퇴시키는 코루틴 실행
                    var moveDist = CurrentWeapon.MinAttackDistance - vecToTarget.magnitude;
                    StartCoroutine(target.PullBackCharacter(this, moveDist));
                }

                // 동기화된 리액션 처리
                if (target.CheckIfAttackKills(attack, this) &&
                    !attack.IsFinisher &&
                    !attack.Reaction.willBeKnockedDown)
                {
                    // 공격 및 대상의 동기화 애니메이션 상태를 false로 설정
                    IsInSyncedAnimation = target.IsInSyncedAnimation = false;
                }
                else
                {
                    // 공격이 차단될 경우의 동작 정의
                    if (willAttackBeBlocked &&
                        attack.OverrideBlockedReaction &&
                        attack.IsSyncedBlockedReaction)
                    {
                        shouldStartBlockingEarlier = true;
                    }

                    // 공격이 차단되지 않을 경우의 동작 정의
                    if (!willAttackBeBlocked &&
                        attack.OverrideReaction &&
                        attack.IsSyncedReaction)
                    {
                        // 공격 및 대상의 동기화 애니메이션 상태를 true로 설정
                        IsInSyncedAnimation = target.IsInSyncedAnimation = true;
                    }

                    // 동기화 애니메이션 상태일 경우 현재 동기화 액션 설정
                    if (IsInSyncedAnimation)
                        CurrSyncedAction = target.CurrSyncedAction = attack;
                }
            }

            // 공격 클립 재생. 애니메이션 크로스페이드 설정
            animGraph.CrossFade(
                attack.Clip,
                transitionOut: 0.4f,
                animationSpeed: attack.AnimationSpeed,
                clipInfo: attack.Clip
            );

            // 목표 위치 매칭 초기화
            MatchingTargetDeltaPos = Vector3.zero;
            IsMatchingTarget = false;

            // 현재 공격 클립의 총 길이를 가져옴
            float attackLength = animGraph.CurrentClipStateInfo.ClipLength;

            // 공격 클립이 끝날 때까지 진행
            while (animGraph.CurrentClipStateInfo.Timer <= attackLength)
            {
                // 현재 상태가 피격 중 또는 사망 상태라면 루프 종료
                if (State == FighterState.TakingHit || State == FighterState.Dead)
                    break;

                // 현재 클립의 진행 시간 및 정규화된 시간 계산
                float timer = animGraph.CurrentClipStateInfo.Timer;
                float normalizedTime = timer / attackLength;
                AttackTimeNormalized = normalizedTime;

                // 동기화된 리액션 재생
                if (IsInSyncedAnimation && !syncedReactionPlayed)
                {
                    if (!willAttackBeBlocked &&
                        attack.OverrideReaction &&
                        attack.IsSyncedReaction)
                    {
                        // 공격의 히트 타이밍 및 범위 설정
                        var hittingTimer = Mathf.Clamp(
                            attack.HittingTime * attackLength - timer,
                            0,
                            attack.HittingTime * attackLength
                        );

                        // 대상의 이동 멈춤 설정
                        target.StopMovement = true;

                        // 동기화 시작 시간에 도달했을 때 리액션 실행
                        if (normalizedTime >= attack.SyncStartTime)
                        {
                            syncedReactionPlayed = true;
                            target.TakeHit(
                                this,
                                reaction: attack.Reaction,
                                willBeBlocked: false,
                                hittingTime: hittingTimer
                            );
                        }
                    }
                }

                // 차단 애니메이션을 미리 실행해야 하는 경우 처리
                if (shouldStartBlockingEarlier)
                {
                    // 차단 히팅 타이밍 계산
                    var hittingTimer = Mathf.Clamp(
                        attack.BlockedHittingTime * attackLength - timer,
                        0,
                        attack.BlockedHittingTime * attackLength
                    );

                    // 블록 동기화 시작 시간 도달 시
                    if (normalizedTime >= attack.BlockSyncStartTime)
                    {
                        shouldStartBlockingEarlier = false;
                        target.PlayingBlockAnimationEarlier = true;
                        target.TakeHit(
                            this,
                            reaction: attack.BlockedReaction,
                            willBeBlocked: true,
                            hittingTime: hittingTimer
                        );
                    }
                }

                // 공격 수행 중 대상 방향으로 이동 처리
                if (target != null &&
                    attack.MoveType != TargetMatchType.Snap &&
                    attack.MoveToTarget &&
                    normalizedTime >= attack.MoveStartTime &&
                    normalizedTime <= attack.MoveEndTime)
                {
                    // 대상 매칭 상태로 플래그 설정
                    IsMatchingTarget = true;

                    // 움직임 타입이 Linear일 경우
                    if (attack.MoveType == TargetMatchType.Linear)
                    {
                        // 선형 움직임의 델타 계산
                        MatchingTargetDeltaPos =
                            (targetPos - startPos) * Time.deltaTime /
                            ((attack.MoveEndTime - attack.MoveStartTime) * attackLength);
                    }
                    else
                    {
                        // 루트 모션 기반 움직임 계산
                        var destinationDisplacement = Vector3.Scale(
                            animator.deltaPosition,
                            rootMotionScaleFactor
                        );
                        MatchingTargetDeltaPos = destinationDisplacement;
                    }
                }
                else
                {
                    // 매칭 상태 초기화
                    IsMatchingTarget = false;
                }

                // 공격 방향으로 회전 처리
                if (CurrAttack.AlwaysLookAtTheTarget && attackDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        Quaternion.LookRotation(attackDir),
                        rotationSpeedDuringAttack * Time.deltaTime
                    );
                }

                // 공격 상태에 따른 처리
                if (AttackState == AttackStates.Windup) // 공격 준비 상태
                {
                    // 정규화 시간이 임팩트 타이밍 시작 시간 도달 시
                    if (normalizedTime >= attack.ImpactStartTime)
                    {
                        AttackState = AttackStates.Impact; // 상태를 임팩트로 변경
                        EnableActiveCollider(attack); // 활성화된 콜라이더 활성화
                        if (activeCollider)
                        {
                            // 이전 콜라이더의 위치 초기화
                            prevColliderPos = activeCollider.transform.TransformPoint(activeCollider.center);
                            prevGameObj = null;
                        }
                    }
                }
                else if (AttackState == AttackStates.Impact) // 임팩트 상태
                {
                    // 동기화된 애니메이션이 아니면 콜라이더 스윕 처리
                    if (!IsInSyncedAnimation)
                        HandleColliderSweep(target);

                    // 정규화 시간이 임팩트 종료 시간 도달 시
                    if (normalizedTime >= attack.ImpactEndTime)
                    {
                        AttackState = AttackStates.Cooldown; // 상태를 쿨다운으로 변경
                        DisableActiveCollider(); // 활성화된 콜라이더 비활성화
                        prevGameObj = null; // 이전 오브젝트 초기화
                    }
                }
                else if (AttackState == AttackStates.Cooldown) // 쿨다운 상태
                {
                    // 콤보 공격이 가능하며 현재 공격 리스트가 비어있지 않을 때
                    if (doCombo && CurrAttacksList.Count > 0)
                    {
                        // 다음 공격을 대기할 시간이 아직 도달하지 않았으면 대기
                        if (attack.WaitForNextAttack && normalizedTime < attack.WaitForAttackTime)
                        {
                            yield return null;
                            continue;
                        }

                        // 콤보 공격 실행
                        doCombo = false;
                        comboCount = wasChargedAttack ? 0 : (comboCount + 1) % CurrAttacksList.Count;

                        // 대상이 사망하지 않았다면 다음 공격 실행
                        if (target == null || !target.IsDead)
                        {
                            // 동기화된 액션과 관련된 애니메이션 상태 초기화
                            if (CurrSyncedAction == attack &&
                                (attack.IsSyncedReaction || attack.IsSyncedBlockedReaction))
                            {
                                target.IsInSyncedAnimation = IsInSyncedAnimation = false;
                            }

                            // 다음 공격 코루틴 실행
                            StartCoroutine(Attack(target));
                            yield break;
                        }
                    }
                }

                // 프레임 대기
                yield return null;
            }

            // 동기화된 액션 상태 초기화
            if (CurrSyncedAction == attack && (attack.IsSyncedReaction || attack.IsSyncedBlockedReaction))
            {
                target.IsInSyncedAnimation = IsInSyncedAnimation = false;
            }

            // 충돌 무시 상태 초기화
            if (IgnoreCollisions && !IsInSyncedAnimation)
            {
                IgnoreCollisions = false;
                target.IgnoreCollisions = false;
            }

            // 목표 매칭 상태 초기화
            if (IsMatchingTarget)
                IsMatchingTarget = false;
            MatchingTargetDeltaPos = Vector3.zero;

            // 대상의 공격 종료 처리
            if (target != null)
                target.AttackOver(this);

            // 상태 초기화 및 리셋
            AttackState = AttackStates.Idle; // 상태를 Idle로 변경
            comboCount = 0; // 콤보 카운트 초기화
            attackingTarget = null; // 공격 대상 초기화
            CurrAttack = null; // 현재 공격 초기화

            // 상태가 공격 중인 경우 종료 이벤트 호출
            if (State == FighterState.Attacking)
                OnEndAction?.Invoke();

            // 공격 상태를 None으로 리셋
            ResetStateToNone(FighterState.Attacking);
        }

        // 현재 카운터 윈도우 상태 확인
        public bool IsInCounterWindow() =>
            AttackState == AttackStates.Cooldown && !doCombo;

        // 공격을 차단할 수 있는지 여부 확인
        bool WillBlockAttack(AttackData attack) =>
            IsBlocking && !attack.IsUnblockableAttack;

        // 블로킹 상태 변경 처리
        void HandleBlockingChanged(bool wasPreviouslyBlocking)
        {
            // 블로킹 시작 시 상태 전환
            if (isBlocking && !wasPreviouslyBlocking)
            {
                SetState(FighterState.Blocking);
                animGraph.CrossFadeAvatarMaskAnimation(
                    CurrentWeapon.Blocking,
                    mask: CurrentWeapon.BlockMask,
                    transitionInTime: .1f
                );
            }
            // 블로킹 해제 시 상태 복원
            else if (!isBlocking && wasPreviouslyBlocking)
            {
                ResetStateToNone(FighterState.Blocking);
                animGraph.RemoveAvatarMask();
            }
        }

        /// <summary>
        /// 회피 동작을 수행하는 코루틴
        /// </summary>
        /// <param name="dodgeDir">회피 방향</param>
        public IEnumerator Dodge(Vector3 dodgeDir)
        {
            // 회피가 가능한 경우 처리
            if (CanDodge)
            {
                // 현재 무기의 회피 데이터 또는 일반 회피 데이터 선택
                var dodge = CurrentWeapon != null && CurrentWeapon.OverrideDodge
                    ? CurrentWeapon.DodgeData
                    : dodgeData;

                // 회피 방향이 초기값인 경우 회피 방향 계산
                if (dodgeDir == Vector3.zero)
                    dodgeDir = dodge.GetDodgeDirection(transform, Target?.transform);

                // 애니메이션 클립 정보 가져오기
                AnimGraphClipInfo dodgeClip = dodge.GetClip(transform, dodgeDir);

                // 회피 시작 이벤트 처리
                OnStartAction?.Invoke();
                SetState(FighterState.Dodging); // 상태를 Dodging으로 설정
                IsInvincible = true; // 무적 상태 활성화

                // 회피 애니메이션 재생 및 로테이션 갱신 처리
                yield return animGraph.CrossFadeAsync(dodgeClip, onAnimationUpdate: (_, time) =>
                {
                    if (time <= dodgeClip.Length * 0.8f && !dodge.useDifferentClipsForDirections)
                    {
                        // 회피 방향으로 캐릭터의 회전 처리
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(dodgeDir),
                            1000 * Time.deltaTime
                        );
                    }
                }, clipInfo: dodgeClip);

                // 무적 상태 비활성화 및 상태 초기화
                IsInvincible = false;
                ResetStateToNone(FighterState.Dodging);

                // 동작이 바쁘지 않은 경우 종료 이벤트 호출
                if (!IsBusy)
                    OnEndAction?.Invoke();
            }
        }

        /// <summary>
        /// 구르기 동작을 수행하는 코루틴
        /// </summary>
        /// <param name="rollDir">구르기 방향</param>
        public IEnumerator Roll(Vector3 rollDir)
        {
            // 구르기가 가능한 경우 처리
            if (CanRoll)
            {
                // 현재 무기의 구르기 데이터 또는 일반 구르기 데이터 선택
                var roll = CurrentWeapon != null && CurrentWeapon.OverrideRoll
                    ? CurrentWeapon.RollData
                    : rollData;

                // 구르기 방향 초기화
                if (rollDir == Vector3.zero)
                    rollDir = roll.GetDodgeDirection(transform, Target?.transform);

                // 애니메이션 클립 정보 가져오기
                AnimGraphClipInfo rollClip = roll.GetClip(transform, rollDir);

                // 구르기 시작 이벤트 처리
                OnStartAction?.Invoke();
                SetState(FighterState.Dodging); // 상태를 Dodging으로 설정
                IsInvincible = true; // 무적 상태 활성화

                // 구르기 애니메이션 재생
                animGraph.CrossFade(rollClip, clipInfo: rollClip);
                yield return null;

                // 애니메이션 진행 중 방향 회전 처리
                while (animGraph.CurrentClipStateInfo.NormalizedTime <= 0.9f)
                {
                    if (!roll.useDifferentClipsForDirections)
                    {
                        transform.rotation = Quaternion.RotateTowards(
                            transform.rotation,
                            Quaternion.LookRotation(rollDir),
                            1000 * Time.deltaTime
                        );
                    }

                    yield return null;
                }

                // 무적 상태 비활성화 및 상태 초기화
                IsInvincible = false;
                ResetStateToNone(FighterState.Dodging);

                // 동작이 바쁘지 않은 경우 종료 이벤트 호출
                if (!IsBusy)
                    OnEndAction?.Invoke();
            }
        }

        /// <summary>
        /// 콜라이더 스윕을 처리하여 대상과의 충돌을 감지
        /// </summary>
        /// <param name="target">충돌을 감지할 대상</param>
        void HandleColliderSweep(MeleeFighter target)
        {
            // 액티브 박스 콜라이더 참조
            var activeBoxCollider = activeCollider;

            if (activeBoxCollider && target != null)
            {
                // 콜라이더 중심점 및 방향 계산
                Vector3 endPoint = activeBoxCollider.transform.TransformPoint(activeBoxCollider.center);
                Vector3 direction = (endPoint - prevColliderPos).normalized;
                float distance = Vector3.Distance(prevColliderPos, endPoint);

                // 콜라이더의 크기 및 방향 설정
                Vector3 halfExtents = Vector3.Scale(activeBoxCollider.size, activeBoxCollider.transform.localScale) *
                                      0.5f;
                Quaternion orientation = activeBoxCollider.transform.rotation;

                // 중복되지 않도록 충돌 객체 배열 초기화
                Collider[] checkCollision = { };
                int size = Physics.OverlapBoxNonAlloc(
                    prevColliderPos,
                    halfExtents,
                    checkCollision,
                    orientation,
                    1 << target.gameObject.layer,
                    QueryTriggerInteraction.Collide
                );

                // OverlapBox 검출
                if (size > 0 && prevGameObj != checkCollision[0].gameObject)
                {
                    var collidedTarget = checkCollision[0].GetComponentInParent<MeleeFighter>();
                    collidedTarget.OnTriggerEnterAction(activeBoxCollider);
                    prevGameObj = checkCollision[0].gameObject;
                }
                else
                {
                    // BoxCast 검출
                    bool isHit = Physics.BoxCast(
                        prevColliderPos,
                        halfExtents,
                        direction,
                        out var hit,
                        orientation,
                        distance,
                        1 << target.gameObject.layer,
                        QueryTriggerInteraction.Collide
                    );

                    if (isHit && prevGameObj != hit.transform.gameObject)
                    {
                        var collidedTarget = hit.transform.GetComponentInParent<MeleeFighter>();
                        collidedTarget.OnTriggerEnterAction(activeBoxCollider);
                        prevGameObj = hit.transform.gameObject;
                    }
                }
            }

            // 이전 콜라이더의 위치 갱신
            if (activeBoxCollider)
                prevColliderPos = activeBoxCollider.transform.TransformPoint(activeBoxCollider.center);
        }

        /// <summary>
        /// 트리거 콜라이더와의 충돌 처리
        /// </summary>
        /// <param name="other">충돌한 콜라이더</param>
        private void OnTriggerEnterAction(Collider other)
        {
            // 충돌 처리 조건 확인
            if (other.CompareTag("Hitbox") && CanTakeHit && !IsInvincible)
            {
                // 공격자 및 공격 정보 가져오기
                var attacker = other.GetComponentInParent<MeleeFighter>();
                var currAttack = attacker.CurrAttack;
                var attackType = attacker.CurrAttackContainer.AttackType;

                // 동기화된 애니메이션, 일어나는 중, 다운 상태에서의 충돌 무시
                if (IsInSyncedAnimation || State == FighterState.GettingUp ||
                    (State == FighterState.TakingHit && currReaction is { willBeKnockedDown: true }))
                {
                    return;
                }

                // 스탠딩 공격 및 지면 공격 조건 확인
                if ((IsKnockedDown && attackType != AttackType.GroundAttack) ||
                    (!IsKnockedDown && attackType == AttackType.GroundAttack))
                {
                    return;
                }

                // 의도되지 않은 타겟에 대한 공격 무시
                if (attacker.attackingTarget != this && !attacker.CurrAttack.CanHitMultipleTargets)
                {
                    return;
                }

                // 공격 차단 여부 확인
                bool willBeBlocked = WillBlockAttack(currAttack);

                // 차단 애니메이션 중 중복 처리 방지
                if (willBeBlocked && PlayingBlockAnimationEarlier)
                {
                    return;
                }

                // Late Blocking 관련 처리 (주석으로 비활성화된 상태 유지)
                //if (willBeBlocked && currAttack.OverrideBlockedReaction && currAttack.IsSyncedBlockedReaction && !CheckIfAttackKills(currAttack, attacker))
                //    willBeBlocked = false;

                // 충돌 지점 계산
                other.enabled = true;
                var hitPoint = IsBlocking
                    ? other.ClosestPoint(
                        weaponCollider != null ? weaponCollider.transform.position : transform.position)
                    : other.ClosestPoint(transform.position);
                other.enabled = false;

                // 공격 받기 함수 호출
                TakeHit(attacker, hitPoint, willBeBlocked: willBeBlocked, reaction: null);
            }
        }

        Reaction currReaction;

        /// <summary>
        /// 공격에 맞는 처리를 수행
        /// </summary>
        /// <param name="attacker">공격자</param>
        /// <param name="hitPoint">공격이 닿은 지점</param>
        /// <param name="reaction">리액션 데이터</param>
        /// <param name="willBeBlocked">공격이 차단되었는지 여부</param>
        /// <param name="hittingTime">공격 타이밍</param>
        void TakeHit(MeleeFighter attacker, Vector3 hitPoint = new Vector3(), Reaction reaction = null,
            bool willBeBlocked = false, float hittingTime = 0)
        {
            // 이동 중지 플래그 설정
            StopMovement = false;

            // 사망 상태거나 공격을 받을 수 없으면 리턴
            if (State == FighterState.Dead || !CanTakeHit)
                return;

            if (!willBeBlocked)
            {
                // 공격에 따른 데미지 계산
                TakeDamage(attacker.CurrAttack.IsFinisher ? CurrentHealth : attacker.CurrAttack.Damage);

                // 타겟 회전 처리
                if (attacker.CurrAttack.RotateTarget && (reaction != null || attacker.CurrAttack.OverrideReaction))
                    RotateToAttacker(attacker, attacker.CurrAttack.RotationOffset);

                // 리액션 데이터가 없으면 적합한 리액션 데이터 선택
                if (reaction == null)
                {
                    if (attacker.CurrAttack.OverrideReaction)
                    {
                        reaction = attacker.CurrAttack.Reaction;
                    }
                    else
                    {
                        var reactionData = weapon != null ? weapon.ReactionData : defaultAnimations.hitReactionData;
                        reaction = ChooseHitReaction(hitPoint, attacker.CurrAttack, reactionData, attacker);
                    }
                }
            }
            else
            {
                // 차단된 공격에 따른 데미지 계산
                TakeDamage(Mathf.RoundToInt(attacker.CurrAttack.Damage * (attacker.CurrentWeapon.BlockedDamage / 100)));

                // 차단된 상태에서 타겟 회전 처리
                if (attacker.CurrAttack.RotateTargetInBlocked &&
                    (reaction != null || attacker.CurrAttack.OverrideReaction))
                    RotateToAttacker(attacker, attacker.CurrAttack.RotationOffsetInBlocked);

                // 리액션 데이터가 없으면 적합한 차단 리액션 데이터 선택
                if (reaction == null)
                {
                    if (attacker.CurrAttack.OverrideBlockedReaction)
                    {
                        reaction = attacker.CurrAttack.BlockedReaction;
                    }
                    else
                    {
                        var reactionData = CurrentWeapon != null
                            ? CurrentWeapon.BlockReactionData
                            : defaultAnimations.blockedReactionData;
                        reaction = ChooseHitReaction(hitPoint, attacker.CurrAttack, reactionData, attacker);
                    }
                }
            }

            // 전투 모드로 전환
            animator.SetBool(AnimatorParameters.CombatMode, true);

            // 공격에 맞았을 때 발생시키는 이벤트 호출
            OnGotHit?.Invoke(attacker, hitPoint, hittingTime, willBeBlocked);
            OnGotHitEvent?.Invoke(attacker, hitPoint, hittingTime);

            // 현재 리액션 설정
            currReaction = reaction;

            // 체력이 남아있거나 넉다운 또는 피니셔 공격인 경우 리액션 재생
            if (CurrentHealth > 0 ||
                (reaction != null && (reaction.willBeKnockedDown || attacker.CurrAttack.IsFinisher)))
            {
                StartCoroutine(PlayHitReaction(attacker, reaction, willBeBlocked));
            }
            else
            {
                // 사망 애니메이션 재생
                StartCoroutine(PlayDeathAnimation(attacker, reaction?.animationClipInfo));
            }
        }

        /// <summary>
        /// 공격 반응 애니메이션을 재생
        /// </summary>
        /// <param name="attacker">공격자</param>
        /// <param name="reaction">리액션 데이터</param>
        /// <param name="isBlockedReaction">차단된 공격 여부</param>
        /// <param name="getUpAnimation">일어나는 애니메이션</param>
        IEnumerator PlayHitReaction(MeleeFighter attacker, Reaction reaction = null, bool isBlockedReaction = false,
            AnimationClip getUpAnimation = null)
        {
            // 현재 상태 업데이트
            SetState(isBlockedReaction ? FighterState.TakingBlockedHit : FighterState.TakingHit);

            // 액션 시작 이벤트 호출
            OnStartAction?.Invoke();
            ++hitCount;

            // 사망 여부 확인
            bool willBeDead = false;
            if (CurrentHealth == 0 && (reaction is { willBeKnockedDown: true } || attacker.CurrAttack.IsFinisher))
            {
                SetState(FighterState.Dead);
                willBeDead = true;
            }

            // 리액션 애니메이션 클립 유효성 확인
            if (reaction?.animationClip == null && reaction?.animationClipInfo.clip == null)
            {
                Debug.LogError($"Reaction clips are not assigned. Attack - {attacker.CurrAttack.name}");
            }
            else
            {
                // 리액션 애니메이션 재생
                animGraph.CrossFade(reaction.animationClipInfo, transitionBack: CurrentHealth > 0,
                    clipInfo: reaction.animationClipInfo);
                yield return null;
                yield return new WaitUntil(() => animGraph.CurrentClipStateInfo.NormalizedTime >= 0.8f);

                // 넉다운 상태인 경우 넉다운 및 일어나는 애니메이션 처리
                if (!IsDead && reaction.willBeKnockedDown)
                {
                    StartCoroutine(GoToKnockedDownState(reaction));
                }
            }

            // 히트 카운트 감소
            --hitCount;

            if (hitCount == 0)
            {
                // 히트 완료 이벤트 호출
                OnHitComplete?.Invoke();

                // 넉다운 상태에서의 처리
                if (IsKnockedDown)
                    SetState(FighterState.KnockedDown);
                else
                {
                    if (isBlockedReaction && isBlocking)
                        SetState(FighterState.Blocking);
                    else
                        ResetStateToNone(isBlockedReaction ? FighterState.TakingBlockedHit : FighterState.TakingHit);
                }

                // 블록 애니메이션 초기화
                if (isBlockedReaction && PlayingBlockAnimationEarlier)
                    PlayingBlockAnimationEarlier = false;

                // 바쁜 상태가 아니면 액션 종료 이벤트 호출
                if (!IsBusy)
                    OnEndAction?.Invoke();
            }

            // 사망 처리
            if (willBeDead)
                OnDeath?.Invoke();
        }

        /// <summary>
        /// 사망 애니메이션 재생
        /// </summary>
        /// <param name="attacker">공격자</param>
        /// <param name="clipInfo">애니메이션 클립 정보</param>
        /// <returns>코루틴</returns>
        public IEnumerator PlayDeathAnimation(MeleeFighter attacker, AnimGraphClipInfo clipInfo = null)
        {
            // 사망 상태 설정
            SetState(FighterState.Dead);

            // 액션 시작 이벤트 호출
            OnStartAction?.Invoke();

            // Collider 비활성화
            DisableActiveCollider();

            // 공격자를 바라보도록 회전 설정
            var destinationDisplacement = attacker.transform.position - transform.position;
            destinationDisplacement.y = 0;
            transform.rotation = Quaternion.LookRotation(destinationDisplacement);

            // 랜덤 사망 애니메이션 클립 선택
            if (defaultAnimations.deathAnimationClipInfo.Count > 0)
                clipInfo = defaultAnimations.deathAnimationClipInfo[
                    Random.Range(0, defaultAnimations.deathAnimationClipInfo.Count)];
            if (clipInfo != null && clipInfo.clip == null)
                OnDeath?.Invoke();

            // 사망 애니메이션 재생
            yield return animGraph.CrossFadeAsync(clipInfo, transitionBack: false, onComplete: OnDeath,
                clipInfo: clipInfo);
        }

        /// <summary>
        /// 도발 액션 재생
        /// </summary>
        /// <param name="tauntClip">도발 애니메이션 클립</param>
        /// <returns>코루틴</returns>
        public IEnumerator PlayTauntAction(AnimationClip tauntClip)
        {
            // 도발 상태 설정
            SetState(FighterState.Taunt);

            // 액션 시작 이벤트 호출
            OnStartAction?.Invoke();

            // 도발 애니메이션 재생
            animGraph.CrossFade(tauntClip);
            yield return new WaitForSeconds(tauntClip.length);

            // 상태 초기화
            ResetStateToNone(FighterState.Taunt);

            // 액션 종료 이벤트 호출
            if (!IsBusy)
                OnEndAction?.Invoke();
        }

        /// <summary>
        /// 공격자를 바라보도록 회전
        /// </summary>
        /// <param name="attacker">공격자</param>
        /// <param name="rotationOffset">회전 오프셋</param>
        void RotateToAttacker(MeleeFighter attacker, float rotationOffset = 0f)
        {
            var destinationDisplacement = attacker.transform.position - transform.position;
            destinationDisplacement.y = 0;

            // 공격자를 바라보는 방향으로 회전
            transform.rotation = Quaternion.LookRotation(destinationDisplacement) *
                                 Quaternion.Euler(new Vector3(0f, rotationOffset, 0f));
        }

        /// <summary>
        /// 데미지를 적용하여 현재 체력을 감소
        /// </summary>
        /// <param name="damage">적용할 데미지 값</param>
        void TakeDamage(float damage)
        {
            // 현재 체력을 데미지만큼 감소시키되, 0 이하로 내려가지 않도록 제한
            CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, CurrentHealth);
        }

        /// <summary>
        /// 공격이 대상의 체력을 0으로 만들 수 있는지 확인
        /// </summary>
        /// <param name="attack">공격 데이터</param>
        /// <param name="attacker">공격자</param>
        /// <returns>공격으로 인해 체력이 0 이하로 떨어질 경우 true 반환</returns>
        bool CheckIfAttackKills(AttackData attack, MeleeFighter attacker)
        {
            // 공격이 차단되었는지 확인 후 데미지 계산
            var damage = WillBlockAttack(attack)
                ? attack.Damage * (attacker.CurrentWeapon.BlockedDamage / 100)
                : attack.Damage;

            // 현재 체력에서 데미지를 뺀 값이 0 이하인지 반환
            return CurrentHealth - damage <= 0;
        }

        /// <summary>
        /// 히트 리액션을 선택
        /// </summary>
        /// <param name="hitPoint">히트 지점</param>
        /// <param name="currAttack">현재 공격 데이터</param>
        /// <param name="reactionData">리액션 데이터</param>
        /// <param name="attacker">공격자</param>
        /// <returns>선택된 Reaction 객체</returns>
        Reaction ChooseHitReaction(Vector3 hitPoint, AttackData currAttack, ReactionsData reactionData,
            MeleeFighter attacker = null)
        {
            if (reactionData == null) return null;

            // 리액션 리스트 가져오기
            var reactions = reactionData.reactions;
            var hitDirection = currAttack.HitDirection;

            // 공격자를 바라보게 회전
            if (reactionData.rotateToAttacker && attacker != null)
                RotateToAttacker(attacker);

            // 충돌로 인해 결정된 방향의 경우
            if (currAttack.HitDirection == HitDirections.FromCollision)
                hitDirection = GetHitDirection(hitPoint);

            // 뒤에서 공격받았는지 확인
            bool attackedFromBehind = attacker && Vector3.Angle(transform.forward, attacker.transform.forward) <= 90;
            if (!reactionData.rotateToAttacker && attackedFromBehind)
            {
                var reactionsFromBehind = reactions.Where(r => r.attackedFromBehind);
                var reactionContainers = reactionsFromBehind as ReactionContainer[] ?? reactionsFromBehind.ToArray();
                if (reactionContainers.Count() > 0)
                    reactions = reactionContainers.ToList();
            }

            // 공격 방향과 일치하는 리액션 필터링
            var reactionsWithSameDir = reactions.Where(c => c.direction == hitDirection);
            var withSameDir = reactionsWithSameDir as ReactionContainer[] ?? reactionsWithSameDir.ToArray();
            reactions = withSameDir.Count() > 0
                ? withSameDir.ToList()
                : reactions.Where(c => c.direction == HitDirections.Any).ToList();

            // ReactionTag 일치 여부 확인
            if (!String.IsNullOrEmpty(currAttack.ReactionTag))
            {
                var reactionsWithSameTag = reactions.Where(r =>
                    !String.IsNullOrEmpty(r.tag) && r.tag.ToLower() == currAttack.ReactionTag.ToLower());
                var withSameTag = reactionsWithSameTag as ReactionContainer[] ?? reactionsWithSameTag.ToArray();
                if (withSameTag.Count() > 0)
                    reactions = withSameTag.ToList();
                else
                {
                    var reactionsThatContainsTag = reactions.Where(r => !String.IsNullOrEmpty(r.tag)
                                                                        && (r.tag.ToLower()
                                                                                .Contains(currAttack.ReactionTag
                                                                                    .ToLower()) ||
                                                                            currAttack.ReactionTag.ToLower()
                                                                                .Contains(r.tag.ToLower())));

                    var thatContainsTag = reactionsThatContainsTag as ReactionContainer[] ??
                                          reactionsThatContainsTag.ToArray();
                    if (thatContainsTag.Count() > 0)
                        reactions = thatContainsTag.ToList();
                }
            }

            // 랜덤 리액션 선택
            var selectedReactionContainer = reactions.Count > 0 ? reactions.GetRandom() : null;

            return selectedReactionContainer?.reaction;
        }

        /// <summary>
        /// 히트 지점의 방향을 계산하여 HitDirections 반환
        /// </summary>
        /// <param name="hitPoint">히트 지점</param>
        /// <returns>계산된 히트 방향</returns>
        HitDirections GetHitDirection(Vector3 hitPoint)
        {
            // 히트 지점과 현재 위치의 방향 벡터 계산
            var direction = (hitPoint - transform.position + Vector3.up * 0.5f).normalized;

            // 방향 벡터와 오른쪽, 위쪽 축의 점곱 결과
            var right = Vector3.Dot(direction, transform.right);
            var up = Vector3.Dot(direction, transform.up);

            // 히트 방향 결정
            if (Mathf.Abs(right) > Mathf.Abs(up))
                return right > 0 ? HitDirections.Right : HitDirections.Left;
            else if (Mathf.Abs(up) > Mathf.Abs(right))
                return up > 0 ? HitDirections.Top : HitDirections.Bottom;

            return HitDirections.Any;
        }

        // 이전 상태를 저장하기 위한 상태 변수
        FighterState prevState;

        /// <summary>
        /// 전투 상태를 설정
        /// </summary>
        /// <param name="state">변경할 상태</param>
        public void SetState(FighterState state)
        {
            // 상태가 변할 경우 이전 상태를 저장하고 새 상태를 설정
            if (State != state)
            {
                prevState = State;
                State = state;
            }
        }

        /// <summary>
        /// 특정 상태를 초기 상태(FighterState.None)로 리셋
        /// </summary>
        /// <param name="stateToReset">리셋할 상태</param>
        public void ResetStateToNone(FighterState stateToReset)
        {
            // 현재 상태가 리셋 대상 상태와 같을 경우 상태를 None으로 설정
            if (State == stateToReset)
            {
                prevState = State;
                State = FighterState.None;
            }
        }

        /// <summary>
        /// 넉다운 상태로 전환
        /// </summary>
        /// <param name="reaction">넉다운 리액션</param>
        /// <returns>코루틴</returns>
        IEnumerator GoToKnockedDownState(Reaction reaction)
        {
            // 사망 상태가 아닐 때만 넉다운 상태로 전환
            if (State != FighterState.Dead)
            {
                SetState(FighterState.KnockedDown);

                // 넉다운 이벤트 호출
                OnKnockDown?.Invoke();
                OnKnockDownEvent.Invoke();
            }

            // 넉다운 상태에 맞게 Collider 조정
            AdjustColliderForKnockedDownState();

            // 넉다운 및 일어나는 애니메이션 클립 초기화
            AnimationClip lyingDownClip;
            AnimationClip getUpClip;

            if (reaction.overrideLyingDownAnimation)
            {
                // 리액션에서 지정된 눕는 애니메이션 설정
                lyingDownClip = reaction.lyingDownAnimation;
                getUpClip = reaction.getUpAnimation;
            }
            else
            {
                // 넉다운 방향에 따라 기본 눕는 애니메이션 설정
                if (reaction.knockDownDirection == KnockDownDirection.LyingOnBack)
                {
                    lyingDownClip = defaultAnimations.lyingOnBackAnimation;
                    getUpClip = defaultAnimations.getUpFromBackAnimation;
                }
                else
                {
                    lyingDownClip = defaultAnimations.lyingOnFrontAnimation;
                    getUpClip = defaultAnimations.getUpFromFrontAnimation;
                }
            }

            // 눕는 애니메이션 재생 및 대기
            if (lyingDownClip != null)
            {
                animGraph.CrossFadeAndLoop(lyingDownClip, transitionBack: false);
                yield return new WaitForSeconds(Random.Range(reaction.lyingDownTimeRange.x,
                    reaction.lyingDownTimeRange.y));
                yield return new WaitUntil(() => State != FighterState.TakingHit);
            }

            // 사망 상태일 경우 더 이상 진행하지 않음
            if (CurrentHealth <= 0)
                yield break;

            // Collider 설정 복원
            ResetColliderAdjustments();
            animGraph.StopLoopingClip = true;

            // 일어나는 애니메이션이 없을 경우 경고 출력 및 상태 리셋
            if (getUpClip == null)
            {
                Debug.LogWarning("No Get Up Animations is provided to get up from the knocked down state");
                SetState(FighterState.None);
                yield break;
            }

            // GettingUp 상태로 전환 및 이벤트 호출
            SetState(FighterState.GettingUp);
            OnGettingUp?.Invoke();
            OnGettingUpEvent.Invoke();

            // 일어나는 애니메이션 재생 및 상태 초기화
            yield return animGraph.CrossFadeAsync(getUpClip);
            ResetStateToNone(FighterState.GettingUp);
        }


        // CapsuleCollider의 원래 중심 위치를 저장하기 위한 변수
        Vector3 colliderOriginalCenter;

        /// <summary>
        /// 넉다운 상태에 맞게 CapsuleCollider 조정
        /// </summary>
        void AdjustColliderForKnockedDownState()
        {
            // Collider 방향과 중심 위치를 조정 (눕는 상태를 반영)
            capsuleCollider.direction = 2;
            colliderOriginalCenter = capsuleCollider.center;
            capsuleCollider.center = new Vector3(capsuleCollider.center.x, 0, capsuleCollider.center.z);
        }

        /// <summary>
        /// Collider의 설정을 원래 상태로 복원
        /// </summary>
        void ResetColliderAdjustments()
        {
            // Collider 방향과 중심 위치를 원래대로 되돌림
            capsuleCollider.direction = 1;
            capsuleCollider.center = colliderOriginalCenter;
        }

        /// <summary>
        /// 현재 무기의 가장 긴 공격 거리를 계산하여 설정
        /// </summary>
        void FindMaxAttackRange()
        {
            MaxAttackRange = 0f;

            // 현재 무기에 공격 콤보가 있을 경우 최대 공격 거리 계산
            if (CurrentWeapon.Attacks is { Count: > 0 })
            {
                foreach (var combo in CurrentWeapon.Attacks)
                {
                    if (combo.MaxDistance > MaxAttackRange)
                        MaxAttackRange = combo.MaxDistance;
                }
            }
            else
            {
                // 공격 콤보가 없으면 기본 공격 거리 설정
                MaxAttackRange = 3f;
            }
        }

        /// <summary>
        /// 공격 데이터를 기반으로 활성화할 Collider를 설정
        /// </summary>
        /// <param name="attack">공격 데이터</param>
        void EnableActiveCollider(AttackData attack)
        {
            // 공격 데이터의 HitboxToUse에 따라 활성화할 Collider 선택
            switch (attack.HitboxToUse)
            {
                case AttackHitbox.LeftHand:
                    activeCollider = leftHandCollider;
                    CurrentWeaponHandler = leftHandCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.RightHand:
                    activeCollider = rightHandCollider;
                    CurrentWeaponHandler = rightHandCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.LeftFoot:
                    activeCollider = leftFootCollider;
                    CurrentWeaponHandler = leftFootCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.RightFoot:
                    activeCollider = rightFootCollider;
                    CurrentWeaponHandler = rightFootCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.Weapon:
                    activeCollider = weaponCollider;
                    CurrentWeaponHandler = CurrentWeaponObject.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.LeftElbow:
                    activeCollider = leftElbowCollider;
                    CurrentWeaponHandler = leftElbowCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.RightElbow:
                    activeCollider = rightElbowCollider;
                    CurrentWeaponHandler = rightElbowCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.LeftKnee:
                    activeCollider = leftKneeCollider;
                    CurrentWeaponHandler = leftKneeCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.RightKnee:
                    activeCollider = rightKneeCollider;
                    CurrentWeaponHandler = rightKneeCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                case AttackHitbox.Head:
                    activeCollider = headCollider;
                    CurrentWeaponHandler = headCollider.GetComponentInChildren<AttachedWeapon>();
                    break;
                default:
                    // 기본적으로 weaponCollider 초기화
                    weaponCollider = null;
                    break;
            }

            // 현재 활성화된 무기 핸들러를 호출하는 이벤트 실행
            OnEnableHit?.Invoke(CurrentWeaponHandler);
        }

        /// <summary>
        /// 활성화된 Collider 비활성화
        /// </summary>
        void DisableActiveCollider()
        {
            activeCollider = null;
        }

        #region Weapon Controller

        /// <summary>
        /// 무기를 장착
        /// </summary>
        /// <param name="weaponData">장착할 무기 데이터</param>
        /// <param name="playSwitchingAnimation">교체 애니메이션 실행 여부</param>
        /// <param name="onComplete">장착 완료 시 실행할 액션</param>
        public void EquipWeapon(WeaponData weaponData, bool playSwitchingAnimation = true, Action onComplete = null)
        {
            // 무기를 교체 불가능하거나 이미 작업 중일 시 반환
            if (IsBusy || !CanSwitchWeapon) return;

            // 비동기로 무기 장착
            StartCoroutine(EquipWeaponAsync(weaponData, playSwitchingAnimation, onComplete));
        }

        /// <summary>
        /// 비동기로 무기 장착
        /// </summary>
        /// <param name="weaponData">장착할 무기 데이터</param>
        /// <param name="playSwitchingAnimation">교체 애니메이션 실행 여부</param>
        /// <param name="onComplete">장착 완료 시 실행할 액션</param>
        /// <returns>코루틴</returns>
        public IEnumerator EquipWeaponAsync(WeaponData weaponData, bool playSwitchingAnimation = true,
            Action onComplete = null)
        {
            // 무기가 변경되었는지 확인
            var weaponChanged = (CurrentWeapon == null || CurrentWeapon != weaponData);

            if (weaponChanged)
            {
                // 무기 데이터 초기화
                if (weaponData != null)
                    weaponData.InIt();

                // 기존 무기 비장착 실행
                yield return UnEquipWeaponAsync(weaponData, playSwitchingAnimation);

                // 새 무기 데이터 설정
                CurrentWeapon = weaponData;
                CurrAttacksList = new List<AttackSlot>();

                // 무기 장착 관련 이벤트 호출
                OnWeaponEquipAction?.Invoke(CurrentWeapon, playSwitchingAnimation);
                OnWeaponEquipEvent?.Invoke(CurrentWeapon, playSwitchingAnimation);

                // 무기 전환 애니메이션 시작 및 최대 공격 거리 검색
                StartCoroutine(SwitchingWeaponStateAnimation(CurrentWeapon.WeaponEquipAnimation,
                    CurrentWeapon.WeaponActivationTime, playSwitchingAnimation, CurrentWeapon.OverrideController,
                    CurrentWeapon, onComplete));
                FindMaxAttackRange();

                // 무기 오브젝트 설정
                SetWeaponObject();

                if (CurrentWeaponObject != null)
                {
                    // 무기 활성화/비활성화 설정
                    StartCoroutine(EnableAndDisableWeapon(true,
                        playSwitchingAnimation ? CurrentWeapon.WeaponActivationTime : 0));

                    // 무기 Collider 설정
                    weaponCollider = CurrentWeaponObject.GetComponentInChildren<BoxCollider>();
                }
            }
        }

        /// <summary>
        /// 기본 무기를 장착
        /// </summary>
        /// <param name="playSwitchingAnimation">무기를 교체할 때 애니메이션을 재생할지 여부</param>
        /// <param name="onComplete">장착 완료 시 실행할 액션</param>
        public void EquipDefaultWeapon(bool playSwitchingAnimation = true, Action onComplete = null)
        {
            // 작업 중일 경우 종료
            if (IsBusy) return;

            // 기본 무기가 있을 경우 장착
            if (weapon != null)
            {
                EquipWeapon(weapon, playSwitchingAnimation, onComplete);
            }
        }

        /// <summary>
        /// 현재 장착된 무기 오브젝트를 설정
        /// </summary>
        void SetWeaponObject()
        {
            // 현재 무기에 맞는 AttachedWeapon을 찾음
            var foundWeapon = attachedWeapons.FirstOrDefault(w => w.weapon == CurrentWeapon);

            // 현재 무기 오브젝트 초기화
            CurrentWeaponObject = null;

            // 무기를 생성할 필요가 있는 경우
            if (foundWeapon == null && CurrentWeapon.SpawnWeapon && CurrentWeapon.WeaponModel != null)
            {
                var holder = animator.GetBoneTransform(CurrentWeapon.WeaponHolder);

                // 무기 모델을 복제 및 위치, 회전 설정
                CurrentWeaponObject = Instantiate(CurrentWeapon.WeaponModel, holder, true);
                CurrentWeaponObject.transform.localPosition = CurrentWeapon.LocalPosition;
                CurrentWeaponObject.transform.localRotation = CurrentWeapon.LocalRotation;

                // 무기 레이어 설정
                SetWeaponLayer();

                // AttachedWeapon 설정 또는 새로운 Component 추가
                CurrentWeaponHandler = CurrentWeaponObject.GetComponentInChildren<AttachedWeapon>();
                if (CurrentWeaponHandler == null)
                {
                    CurrentWeaponHandler = (AttachedWeapon)CurrentWeaponObject.AddComponent(typeof(AttachedWeapon));
                    CurrentWeaponHandler.weapon = CurrentWeapon;
                }

                // 추가된 무기 목록에 등록
                attachedWeapons.Add(CurrentWeaponHandler);
            }
            else if (foundWeapon != null)
            {
                CurrentWeaponObject = foundWeapon.gameObject;
            }
        }

        /// <summary>
        /// 현재 무기를 빠르게 교체
        /// </summary>
        /// <param name="weaponData">교체할 무기 데이터 (null이면 현재 무기 해제)</param>
        public void QuickSwitchWeapon(WeaponData weaponData = null)
        {
            // 현재 공격 리스트 초기화
            CurrAttacksList = new List<AttackSlot>();

            if (weaponData == null && CurrentWeapon != null)
            {
                // 현재 무기가 있을 경우 해제
                OnWeaponUnEquipAction?.Invoke(CurrentWeapon, false);
                OnWeaponUnEquipEvent?.Invoke(CurrentWeapon, false);

                // 기본 애니메이션 컨트롤러로 전환
                StartCoroutine(animGraph.CrosseFadeOverrideController(defaultAnimatorController, 0));

                // 현재 무기 오브젝트 비활성화
                if (CurrentWeaponObject != null)
                {
                    CurrentWeaponObject.SetActive(false);
                    CurrentWeaponObject = null;
                }

                // 현재 무기 정보 초기화
                CurrentWeapon = null;
                weaponCollider = null;
            }
            else if (weaponData != null)
            {
                // 새로운 무기를 장착
                weaponData.InIt();
                CurrentWeapon = weaponData;

                // 무기 장착 관련 이벤트 호출
                OnWeaponEquipAction?.Invoke(CurrentWeapon, false);
                OnWeaponEquipEvent?.Invoke(CurrentWeapon, false);

                // 최대 공격 범위 재설정 및 무기 설정
                FindMaxAttackRange();
                SetWeaponObject();

                if (CurrentWeaponObject != null)
                {
                    StartCoroutine(EnableAndDisableWeapon(true, 0));

                    // 무기 Collider 설정
                    weaponCollider = CurrentWeaponObject.GetComponentInChildren<BoxCollider>();
                }
            }
        }

        /// <summary>
        /// 현재 무기를 해제
        /// </summary>
        /// <param name="playSwitchingAnimation">해제 시 애니메이션을 재생할지 여부</param>
        /// <param name="onComplete">해제 완료 후 실행할 액션</param>
        public void UnEquipWeapon(bool playSwitchingAnimation = true, Action onComplete = null)
        {
            // 작업 중이거나 무기 교체가 불가능한 경우 종료
            if (IsBusy || !CanSwitchWeapon) return;

            // 비동기적으로 무기 해제 실행
            StartCoroutine(UnEquipWeaponAsync(playSwitchingAnimation: playSwitchingAnimation, onComplete: onComplete));
        }

        /// <summary>
        /// 비동기적으로 무기를 해제
        /// </summary>
        /// <param name="newWeapon">새로 장착할 무기 데이터</param>
        /// <param name="playSwitchingAnimation">해제 시 애니메이션을 재생할지 여부</param>
        /// <param name="onComplete">해제 완료 후 실행할 액션</param>
        /// <returns>코루틴</returns>
        public IEnumerator UnEquipWeaponAsync(WeaponData newWeapon = null, bool playSwitchingAnimation = true,
            Action onComplete = null)
        {
            // 현재 장착된 무기가 없으면 종료
            if (CurrentWeapon == null) yield break;

            // 공격 리스트 초기화 및 이벤트 호출
            CurrAttacksList = new List<AttackSlot>();
            OnWeaponUnEquipAction?.Invoke(CurrentWeapon, playSwitchingAnimation);
            OnWeaponUnEquipEvent?.Invoke(CurrentWeapon, playSwitchingAnimation);

            // 무기 비활성화 실행
            StartCoroutine(EnableAndDisableWeapon(false,
                playSwitchingAnimation ? CurrentWeapon.WeaponDeactivationTime : 0));

            // 애니메이션 컨트롤러 설정
            var overrideController = newWeapon != null && newWeapon.OverrideController != null
                ? newWeapon.OverrideController
                : defaultAnimatorController;

            // 무기 해제 애니메이션 실행
            yield return SwitchingWeaponStateAnimation(CurrentWeapon.WeaponUnEquipAnimation,
                CurrentWeapon.WeaponDeactivationTime, playSwitchingAnimation, overrideController,
                onComplete: onComplete);

            // 무기 오브젝트 및 데이터 초기화
            if (CurrentWeaponObject != null)
                CurrentWeaponObject = null;
            CurrentWeapon = null;

            // 작업 종료 후 이벤트 호출
            if (!IsBusy)
                OnEndAction?.Invoke();
        }

        /// <summary>
        /// 무기 활성화/비활성화 (유예 시간 적용)
        /// </summary>
        /// <param name="enableWeapon">무기 활성화 여부</param>
        /// <param name="time">유예 시간</param>
        /// <returns>코루틴</returns>
        IEnumerator EnableAndDisableWeapon(bool enableWeapon, float time)
        {
            if (CurrentWeaponObject != null)
                yield return new WaitForSeconds(time);

            // 무기 모델 활성화/비활성화 처리
            if (CurrentWeaponHandler != null && CurrentWeaponHandler.unEquippedWeaponModel != null)
                CurrentWeaponHandler.unEquippedWeaponModel.SetActive(!enableWeapon);
            CurrentWeaponObject?.SetActive(enableWeapon);
        }

        /// <summary>
        /// 무기를 전환하는 애니메이션 실행
        /// </summary>
        /// <param name="animationClip">전환 애니메이션</param>
        /// <param name="transitionOut">애니메이션 전환 시간</param>
        /// <param name="playSwitchingAnimation">애니메이션 재생 여부</param>
        /// <param name="overrideController">오버라이드 컨트롤러</param>
        /// <param name="weaponData">무기 데이터</param>
        /// <param name="onComplete">전환 완료 후 실행할 액션</param>
        /// <returns>코루틴</returns>
        IEnumerator SwitchingWeaponStateAnimation(AnimationClip animationClip, float transitionOut,
            bool playSwitchingAnimation = true, AnimatorOverrideController overrideController = null,
            WeaponData weaponData = null, Action onComplete = null)
        {
            // 시작 동작 이벤트 호출
            OnStartAction?.Invoke();

            // 상태를 전환 중으로 설정
            SetState(FighterState.SwitchingWeapon);

            // 전환 애니메이션 실행
            if (animationClip != null && playSwitchingAnimation)
                yield return animGraph.CrossFadeAsync(animationClip, .2f, transitionBack: false, transitionOut);

            // 새로운 컨트롤러 설정
            var newController = overrideController == null ? defaultAnimatorController : overrideController;
            if (!IsDead)
                yield return animGraph.CrosseFadeOverrideController(newController, .2f);

            // 아바타 마스크 처리
            if (weaponData != null)
                animGraph.CrossFadeAvatarMaskAnimation(weaponData.WeaponHoldingClip, mask: weaponData.WeaponHolderMask,
                    transitionInTime: 0.1f);
            else
                animGraph.RemoveAvatarMask();

            // 상태를 초기화
            ResetStateToNone(FighterState.SwitchingWeapon);

            // 작업 완료 후 이벤트 호출
            if (!IsBusy)
                OnEndAction?.Invoke();
            onComplete?.Invoke();
        }

        /// <summary>
        /// 현재 무기 오브젝트의 레이어를 설정
        /// </summary>
        void SetWeaponLayer()
        {
            // 현재 무기 태그 설정
            CurrentWeaponObject.tag = "Hitbox";
        }

        #endregion

        /// <summary>
        /// Gizmos를 이용해 특정 위치를 시각적으로 표시
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red; // Gizmo 색상을 빨간색으로 설정
            if (moveToPos != Vector3.zero)
                Gizmos.DrawSphere(moveToPos, 0.3f); // moveToPos 위치에 구체 그리기
        }

        /// <summary>
        /// 캐릭터를 특정 거리만큼 뒤로 물러나게 함
        /// </summary>
        /// <param name="attacker">공격자 MeleeFighter</param>
        /// <param name="moveDist">이동 거리</param>
        /// <returns>코루틴</returns>
        public IEnumerator PullBackCharacter(MeleeFighter attacker, float moveDist = .75f)
        {
            float timer = 0;
            while (timer < moveDist)
            {
                // 공격자의 방향으로 캐릭터를 뒤로 이동
                characterController.Move(attacker.transform.forward * (moveDist * Time.deltaTime * 2));
                timer += Time.deltaTime * 2;
                yield return null;
            }
        }

        /// <summary>
        /// 공격자의 타격 지점을 계산
        /// </summary>
        /// <param name="attacker">MeleeFighter 공격자</param>
        /// <returns>타격 위치의 Vector3</returns>
        public Vector3 GetHitPoint(MeleeFighter attacker)
        {
            Vector3 hitPoint = Vector3.zero;

            // 공격자의 활성화된 Collider 설정
            attacker.EnableActiveCollider(attacker.CurrAttack);
            if (hitPoint == Vector3.zero)
            {
                attacker.activeCollider.enabled = true;
                hitPoint = IsBlocking
                    ? attacker.activeCollider.ClosestPoint(weaponCollider != null
                        ? weaponCollider.transform.position
                        : transform.position)
                    : attacker.activeCollider.ClosestPoint(transform.position); // 타격 지점 계산
                attacker.activeCollider.enabled = false;
            }

            // 공격자의 Collider 비활성화
            attacker.DisableActiveCollider();
            return hitPoint;
        }

        /// <summary>
        /// 적의 공격을 받는 이벤트
        /// </summary>
        public event Action<MeleeFighter> OnBeingAttacked;

        /// <summary>
        /// 공격자가 현재 캐릭터를 공격함
        /// </summary>
        /// <param name="attacker">공격자 MeleeFighter</param>
        public void BeingAttacked(MeleeFighter attacker)
        {
            OnBeingAttacked?.Invoke(attacker);
            IsBeingAttacked = true; // 현재 공격을 받고 있음을 설정
            CurrAttacker = attacker; // 현재 공격자 설정
        }

        /// <summary>
        /// 공격이 종료되었을 때 호출
        /// </summary>
        /// <param name="attacker">공격자 MeleeFighter</param>
        public void AttackOver(MeleeFighter attacker)
        {
            if (CurrAttacker == attacker)
            {
                IsBeingAttacked = false; // 공격 종료 설정
                CurrAttacker = null; // 공격자 초기화
            }
        }

        /// <summary>
        /// 전투 상태를 초기화
        /// </summary>
        public void ResetFighter()
        {
            // 행동 중인 경우 초기화하지 않음
            if (InAction) return;

            AttackState = AttackStates.Idle; // 공격 상태 초기화
            CurrentHealth = MaxHealth; // 체력 초기화
            attackingTarget = null; // 공격 타겟 초기화
            UnEquipWeapon(false); // 무기 해제
            OnResetFighter?.Invoke(); // 초기화 이벤트 호출
        }

        public List<Rigidbody> Rigidbodies { get; set; }

        /// <summary>
        /// Ragdoll 상태를 설정
        /// </summary>
        /// <param name="state">true일 경우 Ragdoll 활성화, false일 경우 비활성화</param>
        public void SetRagdollState(bool state)
        {
            // Ragdoll 상태 설정 로직 (현재 비활성화된 상태)
            // 활성화/비활성화에 따라 Rigidbody와 Collider 설정 가능
            //if (rigidbodies.Count > 0 && !state)
            //{
            //    var modelTransform = transform.GetComponentsInChildren<Animator>().ToList().FirstOrDefault(a => a != animator);
            //    if (modelTransform != null)
            //    {
            //        transform.position = modelTransform.transform.position;
            //        modelTransform.transform.localPosition = Vector3.zero;
            //    }
            //}
            //foreach (Rigidbody rb in rigidbodies)
            //{
            //    rb.isKinematic = !state;
            //    rb.GetComponent<Collider>().enabled = state;
            //}
            //if(rigidbodies.Count > 0)
            //animator.enabled = !state;
        }

        /// <summary>
        /// Unity Inspector에서 특정 값 변경 시 호출되는 메서드
        /// </summary>
        private void OnValidate()
        {
            // 기본 애니메이션 데이터 정합성 확인 및 ClipInfo 동기화
            for (int i = 0; i < defaultAnimations.deathAnimation.Count; i++)
            {
                if (defaultAnimations.deathAnimation[i] != null)
                {
                    if (defaultAnimations.deathAnimationClipInfo.Count <= i)
                        defaultAnimations.deathAnimationClipInfo.Add(new AnimGraphClipInfo());

                    if (defaultAnimations.deathAnimation[i] != null &&
                        defaultAnimations.deathAnimationClipInfo[i].clip == null)
                        defaultAnimations.deathAnimationClipInfo[i].clip = defaultAnimations.deathAnimation[i];
                }
            }

            // 구르기 및 회피 데이터를 동기화
            SyncDodgeDataClips(rollData);
            SyncDodgeDataClips(dodgeData);
        }

        /// <summary>
        /// DodgeData 내의 Clip과 ClipInfo를 동기화
        /// </summary>
        /// <param name="data">동기화 대상 DodgeData</param>
        void SyncDodgeDataClips(DodgeData data)
        {
            // 기본 클립 동기화
            if (data.clip != null && data.clipInfo.clip == null)
            {
                data.clipInfo.clip = data.clip;
            }

            // 방향에 따라 다른 클립을 사용할 경우 동기화
            if (data.useDifferentClipsForDirections)
            {
                if (data.frontClip != null && data.frontClipInfo.clip == null)
                {
                    data.frontClipInfo.clip = data.frontClip;
                }

                if (data.backClip != null && data.backClipInfo.clip == null)
                {
                    data.backClipInfo.clip = data.backClip;
                }

                if (data.leftClip != null && data.leftClipInfo.clip == null)
                {
                    data.leftClipInfo.clip = data.leftClip;
                }

                if (data.rightClip != null && data.rightClipInfo.clip == null)
                {
                    data.rightClip = data.rightClipInfo.clip;
                }
            }
        }
    }


    [Serializable]
    public class DefaultReactions
    {
        [Tooltip("피격 반응 데이터를 설정합니다.")] public ReactionsData hitReactionData;

        [Tooltip("방어 중 피격 반응 데이터를 설정합니다.")] public ReactionsData blockedReactionData;

        [Header("쓰기와 일어나는 애니메이션")] [Tooltip("등을 대고 쓰러졌을 때 애니메이션입니다.")]
        public AnimationClip lyingOnBackAnimation;

        [Tooltip("등을 대고 쓰러진 상태에서 일어나는 애니메이션입니다.")]
        public AnimationClip getUpFromBackAnimation;

        [Tooltip("앞으로 쓰러졌을 때 애니메이션입니다.")] public AnimationClip lyingOnFrontAnimation;

        [Tooltip("앞으로 쓰러진 상태에서 일어나는 애니메이션입니다.")]
        public AnimationClip getUpFromFrontAnimation;

        [Space(10)] [HideInInspector]
        // 캐릭터 사망 시 애니메이션 클립 리스트
        public List<AnimationClip> deathAnimation = new List<AnimationClip>();

        // 사망 애니메이션 클립 정보 리스트
        [Tooltip("사망 애니메이션의 추가 정보를 담고 있는 리스트입니다.")]
        public List<AnimGraphClipInfo> deathAnimationClipInfo = new List<AnimGraphClipInfo>();
    }

    [Serializable]
    public class DodgeData
    {
        [HideInInspector]
        // 기본 구르기 애니메이션 클립
        public AnimationClip clip;

        [Tooltip("구르기 애니메이션의 추가 정보를 담고 있는 ClipInfo입니다.")]
        public AnimGraphClipInfo clipInfo;

        [Tooltip("기본 구르기 방향을 설정합니다.")] public DodgeDirection defaultDirection;

        [Tooltip("방향별 다른 구르기 애니메이션을 사용할지 여부를 설정합니다.")]
        public bool useDifferentClipsForDirections;

        [HideInInspector]
        // 앞 방향 구르기 애니메이션 클립
        public AnimationClip frontClip;

        [HideInInspector]
        // 뒤 방향 구르기 애니메이션 클립
        public AnimationClip backClip;

        [HideInInspector]
        // 왼쪽 방향 구르기 애니메이션 클립
        public AnimationClip leftClip;

        [HideInInspector]
        // 오른쪽 방향 구르기 애니메이션 클립
        public AnimationClip rightClip;

        [Tooltip("앞 방향 구르기 애니메이션의 ClipInfo입니다.")]
        public AnimGraphClipInfo frontClipInfo;

        [Tooltip("뒤 방향 구르기 애니메이션의 ClipInfo입니다.")]
        public AnimGraphClipInfo backClipInfo;

        [Tooltip("왼쪽 방향 구르기 애니메이션의 ClipInfo입니다.")]
        public AnimGraphClipInfo leftClipInfo;

        [Tooltip("오른쪽 방향 구르기 애니메이션의 ClipInfo입니다.")]
        public AnimGraphClipInfo rightClipInfo;

        /// <summary>
        /// 회피 방향을 계산합니다.
        /// </summary>
        /// <param name="transform">회피를 수행하는 Transform</param>
        /// <param name="target">회피의 대상 Transform</param>
        /// <returns>계산된 회피 방향 벡터</returns>
        public Vector3 GetDodgeDirection(Transform transform, Transform target)
        {
            if (defaultDirection == DodgeDirection.Forward ||
                (defaultDirection == DodgeDirection.TowardsTarget && target == null))
                return transform.forward; // 앞 방향으로 회피

            else if (defaultDirection == DodgeDirection.Backward ||
                     (defaultDirection == DodgeDirection.AwayFromTarget && target == null))
                return -transform.forward; // 뒤 방향으로 회피

            else if (defaultDirection == DodgeDirection.AwayFromTarget)
                return -(target.position - transform.position); // 타겟으로부터 멀어지는 방향으로 회피

            else if (defaultDirection == DodgeDirection.TowardsTarget)
                return (target.position - transform.position); // 타겟을 향한 방향으로 회피

            return -transform.forward; // 기본값: 뒤 방향으로 회피
        }

        /// <summary>
        /// 회피 애니메이션 클립 정보를 가져옵니다.
        /// </summary>
        /// <param name="transform">회피를 수행하는 Transform</param>
        /// <param name="direction">회피 방향 벡터</param>
        /// <returns>적합한 회피 AnimGraphClipInfo</returns>
        public AnimGraphClipInfo GetClip(Transform transform, Vector3 direction)
        {
            if (!useDifferentClipsForDirections)
                return clipInfo; // 방향별 구르기 애니메이션을 사용하지 않을 경우 기본 클립 반환

            var dir = transform.InverseTransformDirection(direction); // 방향 벡터를 로컬로 변환

            float h = dir.x; // 좌/우 방향 값
            float v = dir.z; // 앞/뒤 방향 값

            if (Math.Abs(v) >= Math.Abs(h))
                return (v > 0) ? frontClipInfo : backClipInfo; // 앞/뒤 방향에 따라 ClipInfo 반환
            else
                return (h > 0) ? rightClipInfo : leftClipInfo; // 좌/우 방향에 따라 ClipInfo 반환
        }
    }

    /// <summary>
    /// 회피 방향 Enum
    /// </summary>
    public enum DodgeDirection
    {
        AwayFromTarget, // 타겟에서 멀어지는 방향
        TowardsTarget, // 타겟을 향하는 방향
        Backward, // 뒤쪽 방향
        Forward // 앞쪽 방향
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DodgeData))]
    public class DodgeDataEditor : PropertyDrawer
    {
        /// <summary>
        /// DodgeData를 위한 커스텀 속성 Drawer UI를 제공합니다.
        /// </summary>
        /// <param name="position">그리는 위치 정보를 나타냅니다.</param>
        /// <param name="property">SerializedProperty의 참조입니다.</param>
        /// <param name="label">GUI 라벨 콘텐츠입니다.</param>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // DodgeData의 필드들을 가져옵니다.
            SerializedProperty clip = property.FindPropertyRelative("clipInfo");
            SerializedProperty defaultDirection = property.FindPropertyRelative("defaultDirection");
            SerializedProperty useDifferentClipsForDirections =
                property.FindPropertyRelative("useDifferentClipsForDirections");

            SerializedProperty frontClip = property.FindPropertyRelative("frontClipInfo");
            SerializedProperty backClip = property.FindPropertyRelative("backClipInfo");
            SerializedProperty leftClip = property.FindPropertyRelative("leftClipInfo");
            SerializedProperty rightClip = property.FindPropertyRelative("rightClipInfo");

            // 방향별 다른 클립 사용 여부에 따라 필드 표시를 결정합니다.
            if (!useDifferentClipsForDirections.boolValue)
                EditorGUILayout.PropertyField(clip); // 기본 ClipInfo 필드

            EditorGUILayout.PropertyField(defaultDirection); // 기본 방향 필드
            EditorGUILayout.PropertyField(useDifferentClipsForDirections); // 방향별 클립 사용 여부

            // 방향별 클립을 사용할 경우 해당 필드들을 표시합니다.
            if (useDifferentClipsForDirections.boolValue)
            {
                EditorGUILayout.PropertyField(frontClip); // 앞 방향 클립 필드
                EditorGUILayout.PropertyField(backClip); // 뒤 방향 클립 필드
                EditorGUILayout.PropertyField(leftClip); // 왼쪽 방향 클립 필드
                EditorGUILayout.PropertyField(rightClip); // 오른쪽 방향 클립 필드
            }
        }
    }
#endif
}