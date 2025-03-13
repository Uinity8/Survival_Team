using _01_Scripts.Gameplay.Entity.Characters.Player.Humanoid.Camera;
using Framework.Characters;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* 참고: 애니메이션은 캐릭터와 캡슐에서 컨트롤러를 통해 호출되며,
   애니메이터(Animator)의 null 체크를 수행한 후 실행됩니다.
 */

[RequireComponent(typeof(CharacterController))] // CharacterController 컴포넌트 필수
public class ThirdPersonController : MonoBehaviour
{
#if ENABLE_INPUT_SYSTEM
    private PlayerInput _playerInput;
#endif
    private Animator _animator;
    private CharacterController _controller;
    private InputHandler _input;
    private GameObject _mainCamera;
    
    
    private AnimationController _animationController;
    private GroundChecker _groundChecker;
    private MovementController _movementController;
    private ThirdPersonCameraController _cameraController;
    private bool _hasAnimator;
    

    private void Awake()
    {
        // 메인 카메라 참조 가져오기
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    private void Start()
    {
        _hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputHandler>();
        _playerInput = GetComponent<PlayerInput>();
        
        _animationController = GetComponent<AnimationController>();
        _groundChecker = GetComponent<GroundChecker>();
        _movementController = GetComponent<MovementController>();
        _cameraController = GetComponent<ThirdPersonCameraController>();
        
        _animationController?.Initialize(_animator);
        _movementController?.Initialize(_input, _mainCamera);
    }

    private void Update()
    {
        bool isGrounded = _groundChecker.GroundedCheck();
        _animationController?.SetGroundedState(isGrounded);
        
        _movementController?.JumpAndGravity(isGrounded);
        _animationController?.SetJumpState(_movementController.IsJumping);
        _animationController?.SetFreeFallState(_movementController.IsFreeFalling);
        
        _movementController?.Move();
        _animationController?.UpdateMovementState(
            _movementController.AnimationBlend,
            _movementController.InputMagnitude);
    }

    private void LateUpdate()
    { 
        _cameraController?.CameraRotation(_input.look);
    }
    
}