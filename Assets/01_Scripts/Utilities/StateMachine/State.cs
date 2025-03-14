using UnityEngine;

namespace _01_Scripts.Utilities.StateMachine
{

    public class State<T> : MonoBehaviour
    {
        public virtual void Enter(T owner) { }

        public virtual void Execute() { }

        public virtual void Exit() { }
    }
}