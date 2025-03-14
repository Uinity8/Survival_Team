using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DeadState : IEnemyState
{
    public void Enter(EnemyBase enemy)
    {
        enemy.animator.SetTrigger("Die");
        enemy.Die();
    }

    public void Execute(EnemyBase enemy)
    {
        
    }

    public void Exit(EnemyBase enemy)
    {

    }
}