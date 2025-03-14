using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

public class Condition : MonoBehaviour
{
    //UI�� �����ؼ� �����ִ°� View
    public Image healthBar;
    public Image hungerBar;
    public Image thirstBar;
    public Image staminaBar;


    

    private void Start()
    {
        /* ĳ���� �Ŵ����� ������ �ּ� ����
        characterController = CharacterManager.Player.conditionsController;

        characterController.conditionHandler.PropertyChanged += OnConditionChanged; // ��������Ʈ���� += �� ��������Ʈ �ȿ��ִ� �Լ��� ���� �����Ų�ٴ°�
        UpdateAllBars();
        */
    }

    private void OnConditionChanged(object sender, PropertyChangedEventArgs args)
    {
        /* ĳ���� �Ŵ����� ������ �ּ� ����
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
        // maxValue�� 0�� ��� fillAmount�� 0���� �����Ͽ� �������� ����
        if (maxValue == 0)
        {
            image.fillAmount = 0;
            return;
        }

        image.fillAmount = Mathf.Clamp01(curValue / maxValue);
    }

    private void UpdateAllBars()// ü�¹� �ʱ�ȭ
    {
        /* ĳ���� �Ŵ����� ������ �ּ� ����
        UpdateProgressbar(healthBar, characterController.Health.Value, characterController.Health.MaxValue);
        UpdateProgressbar(hungerBar, characterController.Hunger.Value, characterController.Hunger.MaxValue);
        UpdateProgressbar(thirstBar, characterController.Thirst.Value, characterController.Thirst.MaxValue);
        UpdateProgressbar(staminaBar, characterController.Stamina.Value, characterController.Stamina.MaxValue);
        */
    }

}
