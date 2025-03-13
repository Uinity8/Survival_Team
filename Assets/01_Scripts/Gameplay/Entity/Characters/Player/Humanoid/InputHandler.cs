using Managers;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


public class InputHandler : MonoBehaviour
{
    [Header("Character Input Values")] public Vector2 move;
    public Vector2 look;
    public bool jump;
    public bool sprint;

    [Header("Movement Settings")] public bool analogMovement;

    [Header("Mouse Cursor Settings")] public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    
    private bool _isInventoryOpen;
    
    private void OnEnable()
    {
        InputManager.Instance.OnMoveInput += MoveInput;
        InputManager.Instance.OnLookInput += LookInput;
        InputManager.Instance.OnDashInput += SprintInput;
        InputManager.Instance.OnJumpPressed += JumpInput;
        InputManager.Instance.OnInventoryPressed += ToggleInventory;
    }
    
    private void OnDisable()
    {
        if(InputManager.Instance == null) return;
        
        InputManager.Instance.OnMoveInput -= MoveInput;
        InputManager.Instance.OnLookInput -= LookInput;
        InputManager.Instance.OnDashInput -= SprintInput;
        InputManager.Instance.OnJumpPressed -= JumpInput;
        InputManager.Instance.OnInventoryPressed -= ToggleInventory;
    }

    private void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    private void LookInput(Vector2 newLookDirection)
    {
        look = newLookDirection;
    }

    private void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    private void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
    
    void ToggleInventory()
    {
        if (_isInventoryOpen)
        {
            _isInventoryOpen = false;
            InputManager.Instance.Player.Enable();
            SetCursorState(true);
        }
        else
        {
            _isInventoryOpen = true;
            InputManager.Instance.Player.Disable();
            SetCursorState(false);
        }
    }

    private bool IsCursorLocked() => Cursor.lockState == CursorLockMode.Locked;

}