using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DeadState : IEnemyState
{
    public void Enter(EnemyBase enemy)
    {
        enemy.animator.SetTrigger("Die");
        enemy.StartCoroutine(DieTime(enemy));
    }

    public void Execute(EnemyBase enemy)
    {
        
    }

    public void Exit(EnemyBase enemy)
    {

    }

    IEnumerator DieTime(EnemyBase enemy)
    {
        if(enemy.health <= 0)
        {
            yield  return new WaitForSeconds(enemy.dieDestroyTime);

            enemy.Die();
        }
    }
}