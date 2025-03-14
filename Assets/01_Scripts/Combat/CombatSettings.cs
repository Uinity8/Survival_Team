using UnityEngine;

namespace _01_Scripts.Combat
{
    public class CombatSettings : MonoBehaviour
    {
        [SerializeField] bool onlyCounterWhileBlocking = false;
        [SerializeField] bool onlyCounterFirstAttackOfCombo = true;
        [SerializeField] bool sameInputForAttackAndCounter = false;
        [SerializeField] float holdTimeForChargedAttacks = 0.2f;



        public static CombatSettings Instance { get; private set; }
        private void Awake()
        {
            Instance = this;
        }

        public bool OnlyCounterWhileBlocking => onlyCounterWhileBlocking;
        public bool OnlyCounterFirstAttackOfCombo => onlyCounterFirstAttackOfCombo;
        public bool SameInputForAttackAndCounter => sameInputForAttackAndCounter;
        public float HoldTimeForChargedAttacks => holdTimeForChargedAttacks;
    }
}
