using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IEnemyState
{
    public void Enter(EnemyBase enemy)
    {
        enemy.animator.SetTrigger("Attack");
    }

    public void Execute(EnemyBase enemy)
    {

    }

    public void Exit(EnemyBase enemy)
    {

    }
}
