using _01_Scripts.UI;
using Managers;
using UnityEngine;

namespace Framework
{
    public class GameplayInitializer : SceneInitializer
    {
        public override void Initialize()
        {
            Debug.Log("Gameplay Initialized");

            // 적 생성
           // EnemyManager.Instance.SpawnEnemies();

            // 게임플레이 UI 표시
             UIManager.Instance.ShowUI<UIGameScene>();

            // 게임 로직 실행
            Debug.Log("Gameplay is now running.");
        }
    }
}