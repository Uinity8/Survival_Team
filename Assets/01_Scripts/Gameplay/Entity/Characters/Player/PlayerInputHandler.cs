using Managers;
using UnityEngine;

namespace _01_Scripts.Gameplay.Entity.Characters.Player
{
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")] public bool analogMovement;

        [Header("Mouse Cursor Settings")] public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        private bool _isInventoryOpen;

        private void Start()
        {
            InputManager.Instance.OnMoveInput += ctx => move = ctx;
            InputManager.Instance.OnLookInput += ctx => look = ctx;
            InputManager.Instance.OnDashInput += ctx => sprint = ctx;
            InputManager.Instance.OnJumpPressed += ctx => jump = ctx;
            //InputManager.Instance.OnInventoryPressed += ;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}