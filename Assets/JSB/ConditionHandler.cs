using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

public class ConditionHandler : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private float _value;
    public float maxValue;

    public float Value
    {
        get => _value;
        set
        {
            if (_value == value) return; //체력, 스테미나,등등 의 변화가 없을때는 변경없음

            _value = value;
            if (_value < 0 )_value = 0;
            if (_value > maxValue) _value = maxValue;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        //this = ConditionHander
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //sender(this) = ConditionHander 자신 인스턴스
        //args(이름, 값)
    }
}
