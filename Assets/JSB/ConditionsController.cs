using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using static UnityEngine.Rendering.DebugUI;

public class ConditionsController : MonoBehaviour // MonoBehaviour
{
    //여기서 컨디션의 연산
    public ConditionHandler Health { get; private set; }
    public ConditionHandler Hunger { get; private set; }
    public ConditionHandler Thirst { get; private set; }
    public ConditionHandler Stemina { get; private set; }

    private void Awake()
    {
        Health = new ConditionHandler(100f); 
        Hunger = new ConditionHandler(100f); 
        Thirst = new ConditionHandler(100f); 
        Stemina = new ConditionHandler(100f); 
    }

    private void Start()
    {
        InvokeRepeating("OnHungry", 5f, 5f);
        InvokeRepeating("OnThirst", 5f, 5f);
    }

    private void Update()
    {
        
    }

    
  
    public void Eat(float value)
    {
        
    }

    public void Drink(float value)
    {

    }

    public void Heal(float value)
    {
        if (Health.Value < Health.MaxValue)
        {
            Health.Value += value;
        }
    }

    public void Die(float value)
    {
        if (Health.Value <= 0)
        {
            CancelInvoke("OnHungry");
            CancelInvoke("OnThirst");
        }
    }

    #region 업데이트용 함수
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
