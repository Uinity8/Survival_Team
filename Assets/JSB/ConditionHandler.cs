using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using static UnityEngine.Rendering.DebugUI;

public class ConditionHandler : INotifyPropertyChanged //INotifyPropertyChanged,IDamagable 넣을 예정
{
    //변화 할 값을 최소 최대값에 비례해서 수정뒤 ui 에게 정보 전달만 하기

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


    public event PropertyChangedEventHandler PropertyChanged;//델리게이트

    public ConditionHandler(float maxValue) //게임 시작시 견디션값 최대로
    {
        MaxValue = maxValue;
        _value = maxValue; 
    }

    protected virtual void OnPropertyChanged(string propertyName) // ui 에게 정보를 보네는곳 마다 호출
    {
        //this = ConditionHander
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //sender(this) = ConditionHander 자신 인스턴스
        //args(이름, 값)
    }

    
}



