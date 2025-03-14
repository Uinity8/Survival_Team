using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class InputManager : Singleton<InputManager>
    {
        private PlayerInputAction _playerInput;

        public InputActionMap Player => _playerInput.LocoMotion;

        public event Action<Vector2> OnMoveInput;
        public event Action<Vector2> OnLookInput;
        public event Action OnInteractionPressed;
        [CanBeNull] public event Action<bool> OnJumpPressed;
        public event Action OnInventoryPressed;
        public event Action OnAttackPressed;
        public event Action<bool> OnDashInput;
        public event Action<float> OnZoomInput;


        protected override void Awake()
        {
            base.Awake();
            _playerInput = new PlayerInputAction();
        }

        private void Start()
        {
            InitializeInputs();
        }

        private void InitializeInputs()
        {
            _playerInput.LocoMotion.Move.performed += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.LocoMotion.Move.canceled += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.Camera.Look.performed += ctx => OnLookInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.Camera.Look.canceled += ctx => OnLookInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.LocoMotion.Jump.started += _ => OnJumpPressed?.Invoke(true);
            _playerInput.LocoMotion.Jump.canceled += _ => OnJumpPressed?.Invoke(false);
            _playerInput.LocoMotion.Interaction.started += _ => OnInteractionPressed?.Invoke();
            _playerInput.Combat.Attack.started += _ => OnAttackPressed?.Invoke();
            _playerInput.LocoMotion.Sprint.started += _ => OnDashInput?.Invoke(true);
            _playerInput.LocoMotion.Sprint.canceled += _ => OnDashInput?.Invoke(false);
            _playerInput.Camera.Zoom.performed += ctx => OnZoomInput?.Invoke(ctx.ReadValue<float>());

            _playerInput.Shorcut.Inventory.started += _ =>OnInventoryPressed?.Invoke();
            
            _playerInput.LocoMotion.Enable();
            _playerInput.Shorcut.Enable();
            _playerInput.Camera.Enable();
        }

        private void OnEnable()
        {
            if (_playerInput == null) return;
            _playerInput.LocoMotion.Enable();
            _playerInput.Shorcut.Enable();
            _playerInput.Camera.Enable();
        }

        private void OnDisable()
        {
            if (_playerInput == null) return;
            _playerInput.LocoMotion.Disable();
            _playerInput.Shorcut.Disable();
            _playerInput.Camera.Disable();
        }
        
        
    }
}