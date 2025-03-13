using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseState : IEnemyState
{
    public void Enter(EnemyBase enemy)
    {
        enemy.animator.SetBool("Move", true);
        enemy.SetChase();
    }

    public void Execute(EnemyBase enemy)
    {
        if(enemy.playerDistance <= enemy.attackRange)
        {
            enemy.ChangeState(EnemyStates.Attack);
        }

        if(enemy.playerDistance > enemy.detectDistance)
        {
            enemy.ChangeState(EnemyStates.Idle);
        }
    }

    public void Exit(EnemyBase enemy)
    {
        enemy.animator.SetBool("Move", false);
    }
}