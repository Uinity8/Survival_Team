using _01_Scripts.UI.Scene;
using Managers;
using UnityEngine;

namespace Framework
{
    public class MainMenuInitializer : SceneInitializer
    {
        public override void Initialize()
        {
            Debug.Log("Main Menu Initialized");

            // 메인 메뉴 UI 표시
            UIManager.Instance.ShowUI<UITitleScene>();

            // 버튼 이벤트 설정 (예시)
            Debug.Log("Main Menu is now running.");
        }
    }
}