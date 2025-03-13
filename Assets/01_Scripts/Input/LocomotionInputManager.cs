using System;
using UnityEngine;

namespace _01_Scripts.Input
{
    public class LocomotionInputManager : MonoBehaviour
    {
        [Header("Keys")]
        [SerializeField] KeyCode jumpKey = KeyCode.Space;
        [SerializeField] KeyCode dropKey = KeyCode.E;
        [SerializeField] KeyCode moveType = KeyCode.Tab;
        [SerializeField] KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] KeyCode interactionKey = KeyCode.E;


        [Header("Buttons")]
        [SerializeField] string jumpButton;
        [SerializeField] string dropButton;
        [SerializeField] string moveTypeButton;
        [SerializeField] string sprintButton;
        [SerializeField] string interactionButton;

        public bool JumpKeyDown { get; set; }
        public bool Drop { get; set; }
        public Vector2 DirectionInput { get; set; }
        public Vector2 CameraInput { get; set; }
        public bool ToggleRun { get; set; }
        public bool SprintKey { get; set; }
        public bool Interaction { get; set; }

        public event Action OnInteractionPressed;
        public event Action<float> OnInteractionReleased;

        public float InteractionButtonHoldTime { get; set; } = 0f;
        bool interactionButtonDown;

#if inputsystem
        LocomotionInputAction input;
        private void OnEnable()
        {
            input = new LocomotionInputAction();
            input.Enable();
        }
        private void OnDisable()
        {
            input.Disable();
        }
#endif


        private void Update()
        {
            //Horizontal and Vertical Movement
            HandleDirectionalInput();

            //Camera Movement
            HandlecameraInput();

            //JumpKeyDown
            HandleJumpKeyDown();

            //Drop
            HandleDrop();

            //Walk or Run 
            HandleToggleRun();

            //Sprint
            HandleSprint();

            //Interaction
            HandleInteraction();
        }

        void HandleDirectionalInput()
        {
#if inputsystem
            DirectionInput = input.Locomotion.MoveInput.ReadValue<Vector2>();
#else
            float h = UnityEngine.Input.GetAxisRaw("Horizontal");
            float v = UnityEngine.Input.GetAxisRaw("Vertical");
            DirectionInput = new Vector2(h, v);
#endif
        }

        void HandlecameraInput()
        {
#if inputsystem
            CameraInput = input.Locomotion.CameraInput.ReadValue<Vector2>();
#else
            float x = UnityEngine.Input.GetAxis("Mouse X");
            float y = UnityEngine.Input.GetAxis("Mouse Y");
            CameraInput = new Vector2(x, y);
#endif
        }

        void HandleJumpKeyDown()
        {
#if inputsystem
            JumpKeyDown = input.Locomotion.Jump.WasPressedThisFrame();
#else
            JumpKeyDown = UnityEngine.Input.GetKeyDown(jumpKey) || (String.IsNullOrEmpty(jumpButton) ? false : UnityEngine.Input.GetButtonDown(jumpButton));
#endif
        }

        void HandleDrop()
        {
#if inputsystem
            Drop = input.Locomotion.Drop.inProgress;
#else
            Drop = UnityEngine.Input.GetKey(dropKey) || (String.IsNullOrEmpty(dropButton) ? false : UnityEngine.Input.GetButton(dropButton));
#endif
        }

        void HandleToggleRun()
        {
#if inputsystem
            ToggleRun = input.Locomotion.MoveType.WasPressedThisFrame();
#else
            ToggleRun = UnityEngine.Input.GetKeyDown(moveType) || IsButtonDown(moveTypeButton);
#endif
        }

        void HandleSprint()
        {
#if inputsystem
            SprintKey = input.Locomotion.SprintKey.inProgress;
#else
            SprintKey = UnityEngine.Input.GetKey(sprintKey) || (String.IsNullOrEmpty(sprintButton) ? false : UnityEngine.Input.GetButton(sprintButton));
#endif
        }

        void HandleInteraction()
        {
#if inputsystem
            if (input.Locomotion.Interaction.WasPressedThisFrame())
            {
                interactionButtonDown = true;
                Interaction = true;
            }
            else
            {
                Interaction = false;
            }

            if (interactionButtonDown)
            {
                if (input.Locomotion.Interaction.WasReleasedThisFrame())
                {
                    interactionButtonDown = false;
                    InteractionButtonHoldTime = 0f;
                }

                InteractionButtonHoldTime += Time.deltaTime;
            }

#else

            if (UnityEngine.Input.GetKeyDown(interactionKey) || IsButtonDown(interactionButton))
            {
                interactionButtonDown = true;
                Interaction = true;
            }
            else
            {
                Interaction = false;
            }

            if (interactionButtonDown)
            {
                if (UnityEngine.Input.GetKeyUp(interactionKey) || IsButtonUp(interactionButton))
                {
                    interactionButtonDown = false;
                    InteractionButtonHoldTime = 0f;
                }

                InteractionButtonHoldTime += Time.deltaTime;
            }
#endif
        }


        public bool IsButtonDown(string buttonName)
        {
            if (!String.IsNullOrEmpty(buttonName))
                return UnityEngine.Input.GetButtonDown(buttonName);
            else
                return false;
        }

        public bool IsButtonUp(string buttonName)
        {
            if (!String.IsNullOrEmpty(buttonName))
                return UnityEngine.Input.GetButtonUp(buttonName);
            else
                return false;
        }
    }
}