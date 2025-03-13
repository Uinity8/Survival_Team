using UnityEngine;

namespace Framework.Characters
{
    [RequireComponent(typeof(Animator))]
    public class AnimationController : MonoBehaviour
    {
        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        
        private Animator _animator;

        public void Initialize(Animator animator)
        {
            _animator = animator;
            AssignAnimationIDs();
        }
        
        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }
        

        // 이동 관련 애니메이션 갱신
        public void UpdateMovementState(float speed, float motionSpeed)
        {
            if (!_animator) return;

            _animator.SetFloat(_animIDSpeed, speed);
            _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
        }
        
        // 점프 상태 갱신
        public void SetJumpState(bool isJumping)
        {
            if (!_animator) return;

            _animator.SetBool(_animIDJump, isJumping);
        }
        
        // 착지 상태 갱신
        public void SetGroundedState(bool isGrounded)
        {
            if (!_animator) return;

            _animator.SetBool(_animIDGrounded, isGrounded);
        }
        
        // 낙하 상태 갱신
        public void SetFreeFallState(bool isFalling)
        {
            if (!_animator) return;

            _animator.SetBool(_animIDFreeFall, isFalling);
        }

    }
}