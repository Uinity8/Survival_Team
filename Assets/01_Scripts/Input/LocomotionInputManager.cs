using System;
using System.Collections;
using _01_Scripts.Third_Person_Controller;
using UnityEngine;

namespace _01_Scripts.Input
{
    public class LocomotionInputManager : MonoBehaviour
    {
        public bool Drop { get; set; }
        public Vector2 DirectionInput { get; set; }
        public Vector2 CameraInput { get; set; }
        public bool ToggleRun { get; set; }
        public bool SprintKey { get; set; }

        public event Action<float> OnInteractionHolding;
        public event Action<float> OnInteractionReleased;

        private float _interactionButtonHoldTime;
        bool interactionButtonDown;

        private LocomotionController _locomotionController;
        
        PlayerInputAction input;

        private void Awake()
        {
            input = new PlayerInputAction();
            InitializeInputSystem();
        }

        private void OnEnable()
        {
            input.Enable();
        }

        private void OnDisable()
        {
            input.Disable();
        }

        
        private void Start()
        {
            _locomotionController = GetComponent<LocomotionController>();   
        }
        

        private void InitializeInputSystem()
        {
            input.LocoMotion.Move.performed += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
            input.LocoMotion.Move.canceled += ctx => OnMoveInput(ctx.ReadValue<Vector2>());
            input.LocoMotion.Sprint.started += _ => SprintKey = true;
            input.LocoMotion.Sprint.canceled += _ => SprintKey = false;
            input.LocoMotion.Jump.started += _ => OnJumpInput();
            input.LocoMotion.Interaction.started += _ => OnInteractionStart();
            input.LocoMotion.Interaction.canceled += _ => OnInteractionCanceled();
            
            //카메라 이동
            input.Camera.Look.performed += ctx => CameraInput = ctx.ReadValue<Vector2>();
            input.Camera.Look.canceled += ctx => CameraInput = ctx.ReadValue<Vector2>();
            
        }

        private void OnMoveInput(Vector2 value)
        {
            DirectionInput = value; 
            _locomotionController.GetInputFromInputManager(value);
        }

        private void OnJumpInput()
        {
            _locomotionController.VerticalJump();
        }
        
        
        private void OnInteractionStart()
        {
            _interactionButtonHoldTime = 0f; // 키를 누른 순간 시간 초기화
            interactionButtonDown = true;
            StartCoroutine(CheckHoldTime());
        }

        private void OnInteractionCanceled()
        {
            interactionButtonDown = false; // 키를 뗐으므로 체크 중단
            OnInteractionReleased?.Invoke(_interactionButtonHoldTime);
        }

        private IEnumerator CheckHoldTime()
        {
            while (interactionButtonDown)
            {
                _interactionButtonHoldTime += Time.deltaTime;
                OnInteractionHolding?.Invoke(_interactionButtonHoldTime);
                yield return new WaitForSeconds(0.3f);
            }
        }
        
    }
}