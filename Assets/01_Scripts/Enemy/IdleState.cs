using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IEnemyState
{
    private bool isWaiting = false;
    public void Enter(EnemyBase enemy)
    {
        isWaiting = false;
    }

    public void Execute(EnemyBase enemy)
    {
        if (enemy.playerDistance < enemy.detectDistance)
        {
            enemy.ChangeState(EnemyStates.Chase);
            return;
        }

        if (!isWaiting)
        {
            isWaiting = true;
            enemy.StartCoroutine(IdleTime(enemy));
        }
    }

    public void Exit(EnemyBase enemy)
    {
        isWaiting = false;
    }

    IEnumerator IdleTime(EnemyBase enemy)
    {
        float waitTime = Random.Range(enemy.minProwlWaitTime, enemy.maxProwlWaitTime);

        yield return new WaitForSeconds(waitTime);

        isWaiting = false;
        enemy.ChangeState(EnemyStates.Prowl);
    }
}
