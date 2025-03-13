using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using System;

public enum EnemyStates
{
    Idle,
    Prowl,
    Chase,
    Attack,
    Dead
}
public abstract class EnemyBase : MonoBehaviour
{
    private IEnemyState currentState;
    private Dictionary<EnemyStates, IEnemyState> states = new Dictionary<EnemyStates, IEnemyState>();

    [Header("Stats")]
    public float health;
    public float moveSpeed;
    public float runSpeed;
    public float dieDestroyTime;

    [Header("Attack")]
    public float damage;
    public float attackSpeed;
    public float attackRange;
    public float lastAttackTime;
    public List<int> attackPattern;  //AttackPattern Listup (AttackPattern0(basicattack) - 0, attackpattern1 - 1 .....) 
    public string[] attackAnimName;
    public Action[] attackAction;


    [Header("Idle State")]
    public float minProwlDistance;
    public float maxProwlDistance;
    public float minProwlWaitTime;
    public float maxProwlWaitTime;

    public NavMeshAgent agent;
    public float detectDistance;
    public float playerDistance;

    public Transform player;

    public Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        AutoRegisterAttack();
    }

    void Update()
    {
        playerDistance = Vector3.Distance(transform.position, player.position);
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

        ChangeStat(newStateKey);

        if (currentState != null)
        {
            currentState.Enter(this);
        }
    }

    IEnemyState CreateState(EnemyStates newStateKey)
    {
        switch (newStateKey)
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

    void ChangeStat(EnemyStates newStateKey)
    {
        switch (newStateKey)
        {
            case EnemyStates.Idle:
                agent.isStopped = true;
                break;
            case EnemyStates.Prowl:
                agent.speed = moveSpeed;
                agent.isStopped = false;
                break;
            case EnemyStates.Chase:
                agent.speed = runSpeed;
                agent.isStopped = false;
                break;
            case EnemyStates.Attack:
                agent.isStopped = true;
                break;
            case EnemyStates.Dead:
                agent.isStopped = true;
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
            Vector3 randomLocation = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(minProwlDistance, maxProwlDistance);
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

    public bool CheckChase()
    {
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(player.position, path))
        {
            return true;
        }
        return false;
    }

    public void SetChase()
    {
        agent.SetDestination(player.position);
    }

    //Attack 행동
    //AttackPattern Listup (AttackPattern0(basicattack) - 0, attackpattern1 - 1 .....)
    protected virtual void AttackPattern0()
    {

    }

    protected virtual void AttackPattern1()
    {

    }

    //Dead 행동
    public void Die()
    {
        Destroy(this.gameObject);
    }

    //스킬 array 자동 등록
    private void AutoRegisterAttack()
    {
        MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.Name.StartsWith("AttackPattern")).OrderBy(m => m.Name).ToArray();

        attackAction = new Action[methods.Length];

        for(int i = 0; i < methods.Length; i++)
        {
            attackAction[i] = (Action)Delegate.CreateDelegate(typeof(Action), this, methods[i]);
        }
    }
}
