using System.Collections.Generic;
using FS_ThirdPerson;
using UnityEngine;

namespace _01_Scripts.Combat
{
    [CustomIcon(FolderPath.CombatIcons + "Weapon Icon.png")]
    [CreateAssetMenu(menuName = "Combat/Create Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Tooltip("무기를 스폰할지 여부를 나타냅니다.")] [SerializeField]
        bool spawnWeapon;

        [Tooltip("무기가 장착될 본을 지정합니다.")] [SerializeField]
        HumanBodyBones weaponHolder = HumanBodyBones.RightHand;

        [Tooltip("무기 홀더를 기준으로 한 무기의 로컬 위치를 설정합니다.")] [SerializeField]
        Vector3 localPosition;

        [Tooltip("무기 홀더를 기준으로 한 무기의 로컬 회전을 설정합니다.")] [SerializeField]
        Vector3 localRotation;

        [Tooltip("스폰될 무기의 모델을 지정합니다.")] [SerializeField]
        GameObject weaponModel;

        [Tooltip("각 공격과 조건에 대한 데이터를 포함하는 공격 데이터 리스트입니다.")] [SerializeField]
        List<AttackContainer> attacks;

        [Tooltip("각 공격과 조건에 대한 데이터를 포함하는 헤비 공격 데이터 리스트입니다.")] [SerializeField]
        List<AttackContainer> heavyAttacks;

        [Tooltip("각 공격과 조건에 대한 데이터를 포함하는 특수 공격 데이터 리스트입니다.")] [SerializeField]
        List<AttackContainer> specialAttacks;

        [Tooltip("무기와 관련된 리액션 데이터를 지정합니다.")] [SerializeField]
        ReactionsData reactionData;

        [Tooltip("무기를 장착할 때 사용되는 애니메이션 클립입니다.")] [SerializeField]
        AnimationClip weaponEquipAnimation;

        [Tooltip("무기를 활성화하는 데 걸리는 시간입니다.")] [SerializeField]
        float weaponActivationTime;

        [Tooltip("무기를 장착 해제할 때 사용되는 애니메이션 클립입니다.")] [SerializeField]
        AnimationClip weaponUnEquipAnimation;

        [Tooltip("무기를 비활성화하는 데 걸리는 시간입니다.")] [SerializeField]
        float weaponDeactivationTime;

        [Tooltip("무기가 공격을 막을 수 있는지 여부를 나타냅니다.")] [SerializeField]
        bool canBlock;

        [Tooltip("공격을 막을 때 사용할 애니메이션 클립입니다.")] [SerializeField]
        AnimationClip blocking;

        [Tooltip("방어 중 공격을 맞을 때 받는 데미지 비율입니다.")] [Range(0, 100)] [SerializeField]
        float blockedDamage = 25f;

        [Tooltip("무기로 방어할 때 발생하는 리액션 데이터를 지정합니다.")] [SerializeField]
        ReactionsData blockReactionData;

        [Tooltip("방어 시 사용할 아바타 마스크를 지정합니다.")] [SerializeField]
        AvatarMask blockMask;

        [Tooltip("무기가 카운터 공격을 수행할 수 있는지 여부를 나타냅니다.")] [SerializeField]
        bool canCounter = true;

        [Tooltip("적이 공격하지 않을 때 카운터 입력을 눌렀을 경우 도발 동작을 실행할지 여부를 나타냅니다. 이는 카운터 입력의 오용을 방지하는 데 사용할 수 있습니다.")]
        [SerializeField]
        bool playActionIfCounterMisused = false;

        [Tooltip("적이 공격하지 않을 때 카운터가 입력된 경우 실행될 동작의 애니메이션 클립입니다.")] [SerializeField]
        AnimationClip counterMisusedAction;

        [Tooltip("무기에 특화된 다양한 이동 애니메이션을 관리하기 위해 사용하는 애니메이터 오버라이드 컨트롤러입니다.")] [SerializeField]
        AnimatorOverrideController overrideController;

        [Tooltip("움직임에 루트 모션을 사용할지 여부를 나타냅니다.")] [SerializeField]
        bool useRootmotion;

        [Tooltip("이동 속도를 덮어쓸지 여부를 나타냅니다.")] [SerializeField]
        bool overrideMoveSpeed;

        [Tooltip("무기를 장착한 상태에서 걷는 속도입니다.")] [SerializeField]
        float walkSpeed = 2f;

        [Tooltip("무기를 장착한 상태에서 달리는 속도입니다.")] [SerializeField]
        float runSpeed = 4.5f;

        [Tooltip("무기를 장착한 상태에서 전력 질주하는 속도입니다.")] [SerializeField]
        float sprintSpeed = 6.5f;

        [Tooltip("전투 모드(대상을 고정함) 중 이동 속도입니다.")] [SerializeField]
        float combatMoveSpeed = 2f;

        [Tooltip("전사의 회피 기능을 사용할지 여부를 나타냅니다.")] [SerializeField]
        bool overrideDodge;

        [Tooltip("회피 동작 데이터를 지정합니다.")] [SerializeField]
        DodgeData dodgeData;

        [Tooltip("전사의 구르기 기능을 사용할지 여부를 나타냅니다.")] [SerializeField]
        bool overrideRoll;

        [Tooltip("구르기 동작 데이터를 지정합니다.")] [SerializeField]
        DodgeData rollData;

        [Tooltip("전투 모드에서만 구르기가 가능하도록 설정합니다.")]
        public bool OnlyRollInCombatMode = true;

        [Tooltip("무기를 잡고 있는 동안의 포즈를 표현하는 애니메이션 클립입니다. 특정 포즈 레이어로 사용됩니다.")] [SerializeField]
        AnimationClip weaponHoldingClip;

        [Tooltip("무기 잡는 애니메이션에서 사용되는 아바타 마스크입니다.")] [SerializeField]
        AvatarMask weaponHolderMask;

        [Tooltip("무기를 장착할 때 재생되는 효과음입니다.")] [SerializeField]
        AudioClip weaponEquipSound;

        [Tooltip("무기를 장착 해제할 때 재생되는 효과음입니다.")] [SerializeField]
        AudioClip weaponUnEquipSound;

        [Tooltip("무기가 효과적인 공격을 하기 위해 필요한 최소 거리입니다.")] [SerializeField]
        float minAttackDistance = 0;

        public void InIt()
        {
            // 일반 공격 리스트에서 각각의 공격 슬롯에 해당 공격 컨테이너를 설정합니다.
            foreach (var attack in attacks)
                attack.AttackSlots.ForEach(a => a.Container = attack);

            // 헤비 공격 리스트에서 각각의 공격 슬롯에 해당 공격 컨테이너를 설정합니다.
            foreach (var attack in heavyAttacks)
                attack.AttackSlots.ForEach(a => a.Container = attack);

            // 특수 공격 리스트에서 각각의 공격 슬롯에 해당 공격 컨테이너를 설정합니다.
            foreach (var attack in specialAttacks)
                attack.AttackSlots.ForEach(a => a.Container = attack);
        }

        public bool SpawnWeapon => spawnWeapon; // 무기가 스폰될지 여부를 반환합니다.
        public HumanBodyBones WeaponHolder => weaponHolder; // 무기가 장착될 본(Bone)을 반환합니다.
        public Vector3 LocalPosition => localPosition; // 무기의 로컬 위치를 반환합니다.
        public Quaternion LocalRotation => Quaternion.Euler(localRotation); // 무기의 로컬 회전을 반환합니다.
        public GameObject WeaponModel => weaponModel; // 무기 모델을 반환합니다.
        public AnimationClip WeaponEquipAnimation => weaponEquipAnimation; // 무기 장착 애니메이션을 반환합니다.
        public float WeaponActivationTime => weaponActivationTime; // 무기 활성화에 걸리는 시간을 반환합니다.
        public AnimationClip WeaponUnEquipAnimation => weaponUnEquipAnimation; // 무기 장착 해제 애니메이션을 반환합니다.
        public float WeaponDeactivationTime => weaponDeactivationTime; // 무기 비활성화에 걸리는 시간을 반환합니다.
        public List<AttackContainer> Attacks => attacks; // 일반 공격 데이터를 포함한 리스트를 반환합니다.
        public List<AttackContainer> HeavyAttacks => heavyAttacks; // 헤비 공격 데이터를 포함한 리스트를 반환합니다.
        public List<AttackContainer> SpecialAttacks => specialAttacks; // 특수 공격 데이터를 포함한 리스트를 반환합니다.
        public ReactionsData ReactionData => reactionData; // 반응(피격 등) 데이터 객체를 반환합니다.
        public bool CanBlock => canBlock; // 무기가 방어 기능을 가질 수 있는지 여부를 반환합니다.
        public AnimationClip Blocking => blocking; // 방어 애니메이션 클립을 반환합니다.
        public float BlockedDamage => blockedDamage; // 방어 시 감소된 데미지를 반환합니다.
        public ReactionsData BlockReactionData => blockReactionData; // 방어 시 발생하는 반응 데이터를 반환합니다.
        public AvatarMask BlockMask => blockMask; // 방어 시 사용되는 아바타 마스크를 반환합니다.
        public AnimatorOverrideController OverrideController => overrideController; // 무기에 부여된 애니메이터 오버라이드 컨트롤러를 반환합니다.
        public bool CanCounter => canCounter; // 무기가 카운터 공격을 할 수 있는지 여부를 반환합니다.
        public bool PlayActionIfCounterMisused => playActionIfCounterMisused; // 카운터가 잘못 사용되었을 때 액션을 실행할지 여부를 반환합니다.
        public AnimationClip CounterMisusedAction => counterMisusedAction; // 잘못된 카운터 사용 시 실행되는 액션 애니메이션 클립을 반환합니다.
        public bool UseRootmotion => useRootmotion; // 루트 모션을 사용할지 여부를 반환합니다.
        public bool OverrideMoveSpeed => overrideMoveSpeed; // 이동 속도가 무기로 덮어쓰여질지 여부를 반환합니다.
        public float WalkSpeed => walkSpeed; // 걸음 속도를 반환합니다.
        public float RunSpeed => runSpeed; // 달리기 속도를 반환합니다.
        public float SprintSpeed => sprintSpeed; // 전력 질주 속도를 반환합니다.
        public float CombatModeSpeed => combatMoveSpeed; // 전투 모드에서 이동 속도를 반환합니다.
        public bool OverrideDodge => overrideDodge; // 회피 동작을 무기로 덮어쓸지 여부를 반환합니다.
        public DodgeData DodgeData => dodgeData; // 회피 동작 데이터를 포함한 객체를 반환합니다.
        public bool OverrideRoll => overrideRoll; // 구르기 동작을 무기로 덮어쓸지 여부를 반환합니다.
        public DodgeData RollData => rollData; // 구르기 동작 데이터를 포함한 객체를 반환합니다.
        public AnimationClip WeaponHoldingClip => weaponHoldingClip; // 무기를 들고 있을 때의 애니메이션 클립을 반환합니다.
        public AvatarMask WeaponHolderMask => weaponHolderMask; // 무기 들기 애니메이션에서 사용되는 아바타 마스크를 반환합니다.
        public AudioClip WeaponEquipSound => weaponEquipSound; // 무기 장착 시 재생되는 소리를 반환합니다.
        public AudioClip WeaponUnEquipSound => weaponUnEquipSound; // 무기 장착 해제 시 재생되는 소리를 반환합니다.
        public float MinAttackDistance => minAttackDistance; // 무기로 공격할 수 있는 최소 거리를 반환합니다. 

    }
}