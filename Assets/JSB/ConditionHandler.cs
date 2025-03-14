using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using static UnityEngine.Rendering.DebugUI;

public class ConditionHandler : INotifyPropertyChanged //INotifyPropertyChanged,IDamagable ���� ����
{
    //��ȭ �� ���� �ּ� �ִ밪�� ����ؼ� ������ ui ���� ���� ���޸� �ϱ�

    private float _value;
    public float MaxValue { get; private set; }

    public float Value
    {
        get => _value;
        set
        {
            if (_value == value) return;

            Value = value;
            if (_value < 0) _value = 0;
            if (_value > MaxValue) _value = MaxValue;
            OnPropertyChanged(nameof(Value));
        }
    }


    public event PropertyChangedEventHandler PropertyChanged;//��������Ʈ

    public ConditionHandler(float maxValue) //���� ���۽� �ߵ�ǰ� �ִ��
    {
        MaxValue = maxValue;
        _value = maxValue; 
    }

    protected virtual void OnPropertyChanged(string propertyName) // ui ���� ������ ���״°� ���� ȣ��
    {
        //this = ConditionHander
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //sender(this) = ConditionHander �ڽ� �ν��Ͻ�
        //args(�̸�, ��)
    }

    
}



