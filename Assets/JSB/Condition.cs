using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Condition : MonoBehaviour
{
    //UI�� �����ؼ� �����ִ°� View
    public Image conditionBar;
    public ConditionHandler conditionHandler;


    private void Start()
    {
        //conditionHandler.PropertyChanged += OnConditionChanged;
        {
           // switch (args.PropertyName)
            {/*
                case nameof(conditionHandler.Health):
                    UpdateProgressbar(conditionBar, conditionHandler.Health.Value, conditionHandler.Health.MaxValue);
                    break;
                case nameof(conditionHandler.Hunger):
                    UpdateProgressbar(conditionBar, conditionHandler.Hunger.Value, conditionHandler.Hunger.MaxValue);
                    break;
                case nameof(conditionHandler.Thirst):
                    UpdateProgressbar(conditionBar, conditionHandler.Thirst.Value, conditionHandler.Thirst.MaxValue);
                    break;
                case nameof(conditionHandler.Stemina):
                    UpdateProgressbar(conditionBar, conditionHandler.Stemina.Value, conditionHandler.Stemina.MaxValue);
                    break;*/
            }
        };
    }
    public void UpdateProgressbar(Image image, float curValue, float maxValue)
    {
        // maxValue�� 0�� ��� fillAmount�� 0���� �����Ͽ� �������� ����
        if (maxValue == 0)
        {
            image.fillAmount = 0;
            return;
        }

        image.fillAmount = Mathf.Clamp01(curValue / maxValue);
    }


}
