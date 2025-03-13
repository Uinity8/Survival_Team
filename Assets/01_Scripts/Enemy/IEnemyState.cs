using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyState
{
    void Enter(EnemyBase enemy);
    void Execute(EnemyBase enemy); 
    void Exit(EnemyBase enemy);    
}
