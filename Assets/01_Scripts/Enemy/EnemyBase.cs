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
public abstract class EnemyBase : MonoBehaviour, IDamagable
{
    private IEnemyState currentState;
    private Dictionary<EnemyStates, IEnemyState> states = new Dictionary<EnemyStates, IEnemyState>();

    [Header("Stats")]
    public float health;
    public float moveSpeed;
    public float runSpeed;
    public float dieDestroyTime;
    //public ItemData[] dropItem;

    [Header("Attack")]
    public float damage;
    public float attackSpeed;
    public float attackRange;
    public float lastAttackTime;
    public float attackAngle;
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
    private Rigidbody _rigidbody;

    public Transform player;

    public Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        AutoRegisterAttack();
        ChangeState(EnemyStates.Idle);
    }

    void Update()
    {
        playerDistance = Vector3.Distance(transform.position, player.transform.position);
        currentState.Execute(this);
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

        Debug.Log(currentState.ToString());

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
                agent.speed = moveSpeed;
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
                agent.speed = moveSpeed;
                agent.isStopped = true;
                break;
            case EnemyStates.Dead:
                agent.speed = moveSpeed;
                agent.isStopped = true;
                break;
        }

        animator.speed = agent.speed / moveSpeed;
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
        if (agent.CalculatePath(player.transform.position, path))
        {
            return true;
        }
        return false;
    }

    public void SetChase()
    {
        agent.SetDestination(player.transform.position);
    }

    //Attack 행동
    //AttackPattern Listup (AttackPattern0(basicattack) - 0, attackpattern1 - 1 .....)
    protected virtual void AttackPattern0()
    {
        //PlayerManager.Instance.player.GetComponent<IDamagable>().TakeDamage(damage);
    }

    protected virtual void AttackPattern1()
    {
        //PlayerManager.Instance.player.GetComponent<IDamagable>().TakeDamage(damage);
    }

    public bool AttackOnSight()
    {
        Vector3 playerDirection = player.transform.position - transform.position;
        float angle = Vector3.Angle(transform.position, playerDirection);
        return angle < attackAngle * 0.5f;
    }

    //Dead 행동
    public void Die() //아이템 프리팹 없어서 주석 처리
    {
        //for(int i = 0; i < dropItem.Length, i++)
        //{
        //    GameObject drops = Instantiate(dropItem[i].dropPrefab, transform.position);
        //    Rigidbody rbDrops = drops.GetComponent<Rigidbody>();

        //    rbDrops.AddForce(transform.up * 1f);
        //}
        
        Destroy(this.gameObject);
    }

    //스킬 array 자동 등록
    private void AutoRegisterAttack()
    {
        //GetMethods(BindingFlags.Instance | BindingFlags.NonPublic) - 클래스내 protected, private 메서드 검색
        MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(m => m.Name.StartsWith("AttackPattern")).OrderBy(m => m.Name).ToArray();

        attackAction = new Action[methods.Length];

        for(int i = 0; i < methods.Length; i++)
        {
            attackAction[i] = (Action)Delegate.CreateDelegate(typeof(Action), this, methods[i]);
        }
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        {
            if (health <= 0)
            {
                ChangeState(EnemyStates.Dead);
            }
        }
        Vector3 dir = player.transform.position - transform.position + new Vector3(0, 0.3f, 0);
        _rigidbody.AddForce(dir.normalized * 2f);
    }
}
