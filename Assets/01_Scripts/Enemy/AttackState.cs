using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AttackState : IEnemyState
{
    private int attackType = -1;
    private string anim;

    public void Enter(EnemyBase enemy)
    {
        attackType = GetRandomAttackPattern(enemy);

        anim = enemy.attackAnimName[attackType];
        enemy.animator.SetTrigger(anim);
    }

    public void Execute(EnemyBase enemy)
    {
        if (Time.time - enemy.lastAttackTime > enemy.attackSpeed && enemy.AttackOnSight())
        {
            enemy.lastAttackTime = Time.time;

            enemy.attackAction[attackType]?.Invoke();

            enemy.ChangeState(EnemyStates.Attack);

        }

        if(enemy.playerDistance > enemy.attackRange)
        {
            enemy.ChangeState(EnemyStates.Chase);
        }

        if (enemy.playerDistance > enemy.detectDistance)
        {
            enemy.ChangeState(EnemyStates.Idle);
        }
    }

    public void Exit(EnemyBase enemy)
    {
        attackType = -1;
        anim = "";
    }

    int GetRandomAttackPattern(EnemyBase enemy)
    {
        int totalWeight = enemy.attackPattern.Sum();

        attackType = Random.Range(0,totalWeight);
        int currentSum = 0;

        for(int i = 0; i < enemy.attackPattern.Count; i++)
        {
            currentSum += enemy.attackPattern[i];
            if(attackType < currentSum)
            {
                return i;
            }
        }

        return -1;
    }

}
