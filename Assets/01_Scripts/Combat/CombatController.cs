using System.Collections.Generic;
using System.Linq;
using _01_Scripts.Core;
using _01_Scripts.Enemy.Enemies;
using _01_Scripts.Input;
using _01_Scripts.System;
using _01_Scripts.Third_Person_Controller;
using AnimatorHash;
using DefaultNamespace;
using UnityEngine;

namespace AnimatorHash
{
    public static partial class AnimatorParameters
    {
        public static readonly int CombatMode = Animator.StringToHash("combatMode");
        public static readonly int StrafeSpeed = Animator.StringToHash("strafeSpeed");
    }
}


namespace _01_Scripts.Combat
{
    public enum TargetSelectionCriteria { DirectionAndDistance, Direction, Distance }

    public class CombatController : SystemBase
    {
        [SerializeField] float moveSpeed = 2f;
        [SerializeField] float rotationSpeed = 500f;

        [Tooltip("Criteria used for selecting the target enemy the player should attack")]
        [SerializeField] TargetSelectionCriteria targetSelectionCriteria;

        [Tooltip("Increase this value if direction should be given more weight than distance for selecting the target. If distance should be given more weight, then decrease it.")]
        [HideInInspector] public float directionScaleFactor = 0.1f;

        public MeleeFighter MeleeFighter { get; private set; }
        public Vector3 InputDir { get; private set; }

        Animator animator;
        AnimGraph animGraph;
        CharacterController characterController;
        CombatInputManager inputManager;
        LocomotionController locomotionController;
        ICharacter player;
        PlayerController playerController;
        EnemyController targetEnemy;

        List<Collider> colliders = new List<Collider>();
        bool isGrounded;
        float ySpeed;
        Quaternion targetRotation;
        bool combatMode;
        bool prevCombatMode;
        float _moveSpeed;
        private Camera _camera;

        public EnemyController TargetEnemy
        {
            get => targetEnemy;
            set
            {
                if (targetEnemy != value)
                {
                    targetEnemy?.OnRemovedAsTarget?.Invoke();
                    targetEnemy = value;
                    MeleeFighter.Target = targetEnemy?.MeleeFighter;
                    targetEnemy?.OnSelectedAsTarget?.Invoke();
                }

                if (targetEnemy == null)
                {
                    CombatMode = false;
                }
            }
        }

        public override SystemState State => SystemState.Combat;

        public bool CombatMode
        {
            get => combatMode;
            set
            {
                combatMode = value;
                if (TargetEnemy == null)
                    combatMode = false;
                if (prevCombatMode != combatMode)
                {
                    if (combatMode)
                    {
                        player.OnStartSystem(this);
                    }
                    else if (!MeleeFighter.IsBusy)
                    {
                        player.OnEndSystem(this);
                    }
                    prevCombatMode = combatMode;
                }


                animator?.SetBool(AnimatorParameters.CombatMode, combatMode);
            }
        }
        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            player = GetComponent<ICharacter>();
            characterController = GetComponent<CharacterController>();
            MeleeFighter = GetComponent<MeleeFighter>();
            inputManager = GetComponent<CombatInputManager>();
            locomotionController = GetComponent<LocomotionController>();
            animGraph = GetComponent<AnimGraph>();

            _moveSpeed = moveSpeed;
        }
        private void Start()
        {
            _camera = Camera.main;
            animator = player.Animator;

            inputManager.OnAttackPressed += OnAttackPressed;

            MeleeFighter.IsPlayerForDebug = true;

            MeleeFighter.OnGotHit += (attacker, hitPoint, hittingTime, isBlockedHit) =>
            {
                CombatMode = true;
                if (attacker != TargetEnemy.MeleeFighter)
                    TargetEnemy = attacker.GetComponent<EnemyController>();
            };
            MeleeFighter.OnAttack += (target) => { if (target != null) CombatMode = true; };

            locomotionController.OnStateExited += () =>
            {
                if (playerController.CurrentSystemState != SystemState.Combat)
                {
                    playerController.WaitToStartSystem = true;

                    MeleeFighter.QuickSwitchWeapon();

                    StartCoroutine(Util.RunAfterFrames(1, () => playerController.WaitToStartSystem = false));

                    locomotionController.HandleTurningAnimation(true);
                    locomotionController.ResetMoveSpeed();
                    MeleeFighter.CanTakeHit = false;
                }
                MeleeFighter.CanSwitchWeapon = false;
            };
            locomotionController.OnStateEntered += () =>
            {
                MeleeFighter.CanTakeHit = true;
                MeleeFighter.CanSwitchWeapon = true;
            };


            MeleeFighter.OnDeath += Death;
            MeleeFighter.OnWeaponEquipAction += (weaponData, playSwitchingAnimation) =>
            {
                locomotionController.HandleTurningAnimation(false);

                if (weaponData.OverrideMoveSpeed)
                {
                    locomotionController.ChangeMoveSpeed(weaponData.WalkSpeed, weaponData.RunSpeed, weaponData.SprintSpeed);
                    moveSpeed = weaponData.CombatModeSpeed;
                }

            };
            MeleeFighter.OnWeaponUnEquipAction += (weaponData, playSwitchingAnimation) =>
            {
                locomotionController.HandleTurningAnimation(true);
                locomotionController.ResetMoveSpeed();
                moveSpeed = _moveSpeed;
            };

            MeleeFighter.OnStartAction += () => { player.OnStartSystem(this); player.UseRootMotion = true; };
            MeleeFighter.OnEndAction += () =>
            {
                if (!combatMode) player.OnEndSystem(this);
                player.UseRootMotion = false;
            };
            MeleeFighter.OnResetFighter += () =>
            {
                player.PreventAllSystems = false;
                characterController.enabled = true;
                colliders.ForEach(c => c.enabled = true);
            };
        }

        void OnAttackPressed(float holdTime, bool isHeavyAttack, bool isCounter, bool isCharged, bool isSpecialAttack)
        {
            if (playerController.FocusedSystemState != SystemState.Locomotion && playerController.FocusedSystemState != SystemState.Combat) return;
            if (TargetEnemy == null && isCounter && !CombatSettings.Instance.SameInputForAttackAndCounter) return;

            if (isGrounded && !MeleeFighter.IsDead)
            {
                var dirToAttack = player.MoveDir == Vector3.zero ? transform.forward : player.MoveDir;

                var enemyToAttack = EnemyManager.Instance?.GetEnemyToTarget(dirToAttack);
                if (enemyToAttack != null)
                    TargetEnemy = enemyToAttack;

                if (CombatSettings.Instance.SameInputForAttackAndCounter)
                {
                    if (MeleeFighter.IsBeingAttacked && MeleeFighter.CurrAttacker.AttackState == AttackStates.Windup)
                        isCounter = true;
                    else
                        isCounter = false;
                }

                MeleeFighter.TryToAttack(enemyToAttack?.MeleeFighter, isHeavyAttack: isHeavyAttack, isCounter: isCounter, isCharged: isCharged, isSpecialAttack: isSpecialAttack);
            }
        }

        void Death()
        {
            characterController.enabled = false;
            colliders = GetComponentsInChildren<Collider>().ToList().Where(c => c.enabled).ToList();
            foreach (var col in colliders)
                col.enabled = false;

            player.UseRootMotion = false;
            player.PreventAllSystems = true;
            player.OnEndSystem(this);
        }

        public Vector3 GetTargetingDir()
        {
            if (CombatMode) return transform.forward;
            if (!_camera) return transform.forward;
            
            var vecFromCam = transform.position - _camera.transform.position;
            vecFromCam.y = 0f;
            return vecFromCam.normalized;

        }


        public override void HandleUpdate()
        {
            if (IsInFocus)
                GroundCheck();
            else
                isGrounded = player.IsGrounded;

            if (isGrounded)
            {
                ySpeed = -0.5f;
            }
            else
            {
                ySpeed += Physics.gravity.y * Time.deltaTime;
            }


            if (MeleeFighter.CurrentWeapon == null || MeleeFighter.CurrentHealth <= 0)
            {
                CombatMode = false;
                return;
            }

            //if (CombatMode && !isInFocus)
            //    player.OnStartAction(this);
            //else if (!CombatMode && isInFocus && !meleeFighter.IsBusy)
            //    player.OnEndAction(this);

            if (inputManager.CombatMode && !MeleeFighter.IsBusy)
                CombatMode = !combatMode;

            MeleeFighter.IsBlocking = inputManager.Block && (!MeleeFighter.IsBusy || MeleeFighter.State == FighterState.TakingBlockedHit) && MeleeFighter.CurrentWeapon.CanBlock;

            if (MeleeFighter.IsBlocking && !CombatMode)
                CombatMode = true;

            if (MeleeFighter.CanDodge && inputManager.Dodge && !MeleeFighter.IsBusy)
            {
                if (MeleeFighter.OnlyDodgeInCombatMode && !IsInFocus) return;

                StartCoroutine(MeleeFighter.Dodge(player.MoveDir));
                return;
            }

            if (MeleeFighter.CanRoll && inputManager.Roll && !MeleeFighter.IsBusy)
            {
                if (MeleeFighter.OnlyRollInCombatMode && !IsInFocus) return;

                StartCoroutine(MeleeFighter.Roll(player.MoveDir));
                return;
            }

            if (!CombatMode)
            {
                ApplyAnimationGravity();
                return;
            }

            if (MeleeFighter != null && (MeleeFighter.InAction || MeleeFighter.CurrentHealth <= 0))
            {
                targetRotation = transform.rotation;
                animator.SetFloat(AnimatorParameters.MoveAmount, 0f);
                ApplyAnimationGravity();
                return;
            }

            float h = player.MoveDir.x;
            float v = player.MoveDir.z;

            float moveAmount = Mathf.Clamp01(Mathf.Abs(h) + Mathf.Abs(v));

            var moveDir = player.MoveDir;
            InputDir = player.MoveDir;

            var velocity = moveDir * moveSpeed;

            // Rotate and face the target enemy
            Vector3 targetVec = transform.forward;
            if (TargetEnemy != null)
            {
                targetVec = TargetEnemy.transform.position - transform.position;
                targetVec.y = 0;
            }

            if (moveAmount > 0)
            {
                targetRotation = Quaternion.LookRotation(targetVec);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation,
                    rotationSpeed * Time.deltaTime);
            }


            // Split the velocity into it's forward and sideward component and set it into the forwardSpeed and strafeSpeed
            float forwardSpeed = Vector3.Dot(velocity, transform.forward);
            animator.SetFloat(AnimatorParameters.MoveAmount, forwardSpeed / moveSpeed, 0.2f, Time.deltaTime);

            float angle = Vector3.SignedAngle(transform.forward, velocity, Vector3.up);
            float strafeSpeed = Mathf.Sin(angle * Mathf.Deg2Rad);
            animator.SetFloat(AnimatorParameters.StrafeSpeed, strafeSpeed, 0.2f, Time.deltaTime);


            if (MeleeFighter.CurrentWeapon.UseRootmotion)
            {
                velocity = animator.deltaPosition;
                transform.rotation *= animator.deltaRotation;
            }
            else
                velocity = velocity * Time.deltaTime;

            velocity.y = ySpeed * Time.deltaTime;

            (_, velocity) = locomotionController.LedgeMovement(moveDir, velocity);
            if (!MeleeFighter.StopMovement)
                characterController.Move(velocity);
        }
        public void ApplyAnimationGravity()
        {
            if (animGraph.CurrentClipStateInfo.IsPlayingAnimation && animGraph.CurrentClipStateInfo.CurrentClipInfo.useGravity && !MeleeFighter.IsMatchingTarget)
            {
                ySpeed += Physics.gravity.y * Time.deltaTime;
                characterController.Move(Vector3.up * (ySpeed * animGraph.CurrentClipInfo.gravityModifier.GetValue(animGraph.CurrentClipStateInfo.NormalizedTime) * Time.deltaTime));
            }
        }

        public override void HandleOnAnimatorMove(Animator anim)
        {
            if (player.UseRootMotion && !MeleeFighter.StopMovement)
            {
                //if (meleeFighter.IsDead)
                //    Debug.Log("Using root motion for death - Matching target - " + meleeFighter.IsMatchingTarget);
                transform.rotation *= anim.deltaRotation;

                var deltaPos = anim.deltaPosition;

                if (MeleeFighter.IsMatchingTarget)
                    deltaPos = MeleeFighter.MatchingTargetDeltaPos;
                var (newDeltaDir, newDelta) = locomotionController.LedgeMovement(deltaPos.normalized, deltaPos);
                if (locomotionController.IsOnLedge) return;

                if (MeleeFighter.IgnoreCollisions)
                    transform.position += newDelta;
                else
                    characterController.Move(newDelta);
            }
        }
        
        void GroundCheck()
        {
            var spherePosition = new Vector3(transform.position.x, transform.position.y - locomotionController.GroundCheckOffset,
                transform.position.z);
            isGrounded = Physics.CheckSphere(spherePosition, locomotionController.GroundCheckRadius, locomotionController.groundLayer,
                QueryTriggerInteraction.Ignore);

              // isGrounded = Physics.CheckSphere(transform.TransformPoint(locomotionController.GroundCheckOffset),
              //     locomotionController.GroundCheckRadius, locomotionController.groundLayer);
              
            animator.SetBool(AnimatorParameters.IsGrounded, isGrounded);
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.5f);
            //Gizmos.DrawSphere(transform.TransformPoint(locomotionController.GroundCheckOffset), locomotionController.GroundCheckRadius);
        }

        public override void EnterSystem()
        {
            MeleeFighter.CanSwitchWeapon = true;
            playerController.WaitToStartSystem = true;
        }

        //private void OnGUI()
        //{
            //GUI.color = Color.black;
            //if (targetEnemy != null)
            //    GUI.Label(new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height),Vector3.Distance(transform.position, targetEnemy.transform.position).ToString());
        //}

        public TargetSelectionCriteria TargetSelectionCriteria => targetSelectionCriteria;
    }
}