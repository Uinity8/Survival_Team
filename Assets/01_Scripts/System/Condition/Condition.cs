using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class Condition : MonoBehaviour
{
    //UI을 연결해서 보여주는곳 View
    public Image healthBar;
    public Image hungerBar;
    public Image thirstBar;
    public Image staminaBar;


    

    private void Start()
    {
        /* 캐릭터 매니져가 생성시 주석 해제
        characterController = CharacterManager.Player.conditionsController;

        characterController.conditionHandler.PropertyChanged += OnConditionChanged; // 델리게이트에서 += 는 델리게이트 안에있는 함수랑 같이 실행시킨다는것
        UpdateAllBars();
        */
    }

    private void OnConditionChanged(object sender, PropertyChangedEventArgs args)
    {
        /* 캐릭터 매니져가 생성시 주석 해제
        switch (args.PropertyName)
        {
            case nameof(characterController.Health):
                UpdateProgressbar(healthBar, characterController.Health.Value, characterController.Health.MaxValue);
                break;
            case nameof(characterController.Hunger):
                UpdateProgressbar(hungerBar, characterController.Hunger.Value, characterController.Hunger.MaxValue);
                break;
            case nameof(characterController.Thirst):
                UpdateProgressbar(thirstBar, characterController.Thirst.Value, characterController.Thirst.MaxValue);
                break;
            case nameof(characterController.Stamina):
                UpdateProgressbar(staminaBar, characterController.Stamina.Value, characterController.Stamina.MaxValue);
                break;
        }
        */
    }

    public void UpdateProgressbar(Image image, float curValue, float maxValue)
    {
        // maxValue가 0일 경우 fillAmount를 0으로 설정하여 나눗셈을 방지
        if (maxValue == 0)
        {
            image.fillAmount = 0;
            return;
        }

        image.fillAmount = Mathf.Clamp01(curValue / maxValue);
    }

    private void UpdateAllBars()// 체력바 초기화
    {
        /* 캐릭터 매니져가 생성시 주석 해제
        UpdateProgressbar(healthBar, characterController.Health.Value, characterController.Health.MaxValue);
        UpdateProgressbar(hungerBar, characterController.Hunger.Value, characterController.Hunger.MaxValue);
        UpdateProgressbar(thirstBar, characterController.Thirst.Value, characterController.Thirst.MaxValue);
        UpdateProgressbar(staminaBar, characterController.Stamina.Value, characterController.Stamina.MaxValue);
        */
    }

}
