using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class InputManager : Singleton<InputManager>
    {
        private PlayerInputActions _playerInput;

        public InputActionMap Player => _playerInput.Player;

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
            _playerInput = new PlayerInputActions();
        }

        protected void Start()
        {
            InitializeInputs();
        }

        private void InitializeInputs()
        {
            _playerInput = new PlayerInputActions();

            _playerInput.Player.Move.performed += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.Player.Move.canceled += ctx => OnMoveInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.Player.Look.performed += ctx => OnLookInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.Player.Look.canceled += ctx => OnLookInput?.Invoke(ctx.ReadValue<Vector2>());
            _playerInput.Player.Jump.started += _ => OnJumpPressed?.Invoke(true);
            _playerInput.Player.Jump.canceled += _ => OnJumpPressed?.Invoke(false);
            _playerInput.Player.Interaction.started += _ => OnInteractionPressed?.Invoke();
            _playerInput.Player.Attack.started += _ => OnAttackPressed?.Invoke();
            _playerInput.Player.Dash.started += _ => OnDashInput?.Invoke(true);
            _playerInput.Player.Dash.canceled += _ => OnDashInput?.Invoke(false);
            _playerInput.Player.Zoom.performed += ctx => OnZoomInput?.Invoke(ctx.ReadValue<float>());

            _playerInput.Shorcut.Inventory.started += _ =>OnInventoryPressed?.Invoke();
        }

        private void OnEnable()
        {
            if (_playerInput == null) return;
            _playerInput.Player.Enable();
            _playerInput.Shorcut.Enable();
        }

        private void OnDisable()
        {
            if (_playerInput == null) return;
            _playerInput.Player.Disable();
            _playerInput.Shorcut.Disable();
        }
        
        
    }
}