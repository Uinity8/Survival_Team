using _01_Scripts.Combat;
using UnityEngine;

namespace _01_Scripts.Enemy.Enemies
{
    public class VisionSensor : MonoBehaviour
    {
        [SerializeField] EnemyController enemy;

        private void Awake()
        {
            enemy.VisionSensor = this;
        }

        private void OnTriggerEnter(Collider other)
        {
            var isTarget = other.gameObject.layer == LayerMask.NameToLayer("Player");
            if (isTarget)
            {
                var fighter = other.GetComponent<MeleeFighter>();
                enemy.TargetsInRange.Add(fighter);
                EnemyManager.Instance.AddEnemyInRange(enemy);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var isTarget = other.gameObject.layer == LayerMask.NameToLayer("Player");
            if (isTarget)
            {
                var fighter = other.GetComponent<MeleeFighter>();
                enemy.TargetsInRange.Remove(fighter);
                EnemyManager.Instance.RemoveEnemyInRange(enemy);
            }
        }
    }
}