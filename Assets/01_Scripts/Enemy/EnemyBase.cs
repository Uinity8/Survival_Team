using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyStates
{
    Idle,
    Prowl,
    Chase,
    Attack,
    Dead
}
public class EnemyBase : MonoBehaviour
{
    private IEnemyState currentState;
    private Dictionary<EnemyStates, IEnemyState> states = new Dictionary<EnemyStates, IEnemyState>();

    [Header("Stats")]
    public float health;
    public float moveSpeed;
    public float runSpeed;
    public float damage;
    public float attackSpeed;
    public float attackRange;

    [Header("Idle State")]
    public float minProwlDistance;
    public float maxProwlDistance;
    public float minProwlWaitTime;
    public float maxProwlWaitTime;

    public NavMeshAgent agent;
    public float detectDistance;
    public float playerDistance;

    public Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }
    void Update()
    {
        playerDistance = Vector3.Distance(transform.position, transform.position/*플레이어 포지션*/);
        currentState.Execute(this);
    }

    public EnemyBase()
    {
        currentState = new IdleState();
        ChangeState(EnemyStates.Idle);
    }

    public void ChangeState(EnemyStates newStateKey)
    {
        if (!states.ContainsKey(newStateKey))
        {
            states[newStateKey] = CreateState(newStateKey);
        }

        if (currentState != null)
        {
            currentState.Exit(this);
        }

        currentState = states[newStateKey];

        if (currentState != null)
        {
            currentState.Enter(this);
        }
    }

    IEnemyState CreateState(EnemyStates newStateKey)
    {
        switch(newStateKey)
        {
            case EnemyStates.Idle:
                return new IdleState();
            case EnemyStates.Prowl:
                return new ProwlState();
            case EnemyStates.Chase:
                return new ChaseState();
            case EnemyStates.Attack:
                return new AttackState();
            case EnemyStates.Dead:
                return new DeadState();
            default:
                return null;
        }
    }

    void ChangeStat(IEnemyState newState)
    {
        switch (newState)
        {
            case ProwlState:

                break;
            case ChaseState:

                break;
        }
    }

    //Idle 행동

    //Prowl 행동
    public void SetProwl()
    {
        agent.SetDestination(ProwlLocation());
    }
    Vector3 ProwlLocation()
    {
        int attempt = 0;
        int maxAttempts = 30;
        NavMeshHit hit;

        do
        {
            Vector3 randomLocation = Random.onUnitSphere * Random.Range(minProwlDistance, maxProwlDistance);
            randomLocation += transform.position;

            NavMesh.SamplePosition(randomLocation, out hit, maxProwlDistance, NavMesh.AllAreas);

            if (Vector3.Distance(transform.position, hit.position) >= minProwlDistance)
            {
                return hit.position;
            }

            attempt++;
        }
        while (attempt < maxAttempts);

        return transform.position;

    }

    //Chase 행동

    //Attack 행동

    //Dead 행동
}
