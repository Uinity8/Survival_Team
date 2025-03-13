using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProwlState : IEnemyState
{
    public void Enter(EnemyBase enemy)
    {
        enemy.animator.SetBool("Move", true);
        enemy.SetProwl();
    }

    public void Execute(EnemyBase enemy)
    {
        if (enemy.playerDistance < enemy.detectDistance)
        {
            if (enemy.CheckChase())
            {
                enemy.ChangeState(EnemyStates.Chase);
            }
        }

        if (enemy.agent.remainingDistance < 0.1f)
        {
            enemy.ChangeState(EnemyStates.Idle);
        }
    }

    public void Exit(EnemyBase enemy)
    {
        enemy.animator.SetBool("Move", false);
    }
}
