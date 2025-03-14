using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.ComponentModel;
using static UnityEngine.Rendering.DebugUI;

public class ConditionsController : MonoBehaviour // IDamagable
{
    //���⼭ ������� ����
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
        InvokeRepeating("OnHungry", 5f, 5f);//����� ���� ���� 
        InvokeRepeating("OnThirst", 5f, 5f);//�񸶸� ���� ����
    }

    private void Update()
    {
        
    }

    /// <summary>
    /// ���� ������ ����
    /// </summary>
    /// <param name="value"></param>
    public void Eat(float value)//������ �������ǰ��� �޾Ƽ� ����
    {
        Health.Value += value;
    }

    /// <summary>
    /// ���� ���Ƕ� ����
    /// </summary>
    /// <param name="value"></param>
    public void Drink(float value)
    {
        Thirst.Value += value;
    }

    /// <summary>
    /// ü�� ���� ����Ҷ�
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
    /// ĳ���Ͱ� ������
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
    /// ����� ���� ����
    /// </summary>
    #region �ݺ� ���� �Լ�
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
