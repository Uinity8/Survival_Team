using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using static UnityEngine.Rendering.DebugUI;

public class ConditionsController : MonoBehaviour // IDamagable
{
    //여기서 컨디션의 연산
    public ConditionHandler Health { get; private set; }
    public ConditionHandler Hunger { get; private set; }
    public ConditionHandler Thirst { get; private set; }
    public ConditionHandler Stamina { get; private set; }

    private void Awake()
    {
        Health = new ConditionHandler(100f); 
        Hunger = new ConditionHandler(100f); 
        Thirst = new ConditionHandler(100f);
        Stamina = new ConditionHandler(100f); 
    }

    private void Start()
    {
        InvokeRepeating("OnHungry", 5f, 5f);//배고픔 지속 감소 
        InvokeRepeating("OnThirst", 5f, 5f);//목마름 지속 감소
    }

    private void Update()
    {
        
    }

    /// <summary>
    /// 음식 먹을때 실행
    /// </summary>
    /// <param name="value"></param>
    public void Eat(float value)//아이템 데이터의값을 받아서 실행
    {
        Health.Value += value;
    }

    /// <summary>
    /// 음료 마실때 실행
    /// </summary>
    /// <param name="value"></param>
    public void Drink(float value)
    {
        Thirst.Value += value;
    }

    /// <summary>
    /// 체력 포션 사용할때
    /// </summary>
    /// <param name="value"></param>
    public void Heal(float value)
    {
        if (Health.Value < Health.MaxValue)
        {
            Health.Value += value;
        }
    }

    /// <summary>
    /// 캐릭터가 죽을때
    /// </summary>
    /// <param name="value"></param>
    public void Die(float value)
    {
        if (Health.Value <= 0)
        {
            CancelInvoke("OnHungry");
            CancelInvoke("OnThirst");
        }
    }
    /// <summary>
    /// 컨디션 지속 감소
    /// </summary>
    #region 반복 실행 함수
    void OnHungry()
    {
        if (Hunger.Value > 0)
        {
            Hunger.Value -= 10f;
        }
    }

    void OnThirst()
    {
        if (Thirst.Value > 0)
        {
            Thirst.Value -= 5f;
        }
    }
    #endregion

    //void TakeDamage(float damage)*/
}
