using Framework.Audio;
using Managers;
using UnityEngine;

namespace Framework
{
    public class SceneBootstrapper : MonoBehaviour
    {
        private static string StartingSceneName { get; set; } // 시작 씬 이름 저장

        // 씬이 로드되기 이전 단계에 해당 메서드를 자동으로 호출하도록 지정하는 어노테이션입니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // 시작 씬 이름 저장 (최초 실행 시에만)
            if (string.IsNullOrEmpty(StartingSceneName))
            {
                StartingSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                Debug.Log($"Starting Scene Set: {StartingSceneName}");
            }
            else
            {
                Debug.Log($"Starting Scene Already Set: {StartingSceneName}");
            }

            // 필수 매니저 초기화
            if (StartingSceneName != "Title")
            {
                Debug.Log("Non-Title Scene Detected. Forcing Initialization...");
                InitializeManagers();
            }
            else
            {
                Debug.Log("Title Scene Detected. No Forced Initialization Required.");
            }
        }

        /// <summary>
        /// 필수 매니저 초기화
        /// </summary>
        private static void InitializeManagers()
        {
            // GameManager 강제 생성
            if (!GameManager.Instance)
                CreateManager("GameManager", "Managers/GameManager");
            
            // SoundManager 강제 생성
            if (!SoundManager.Instance)
                CreateManager("SoundManager", "Managers/SoundManager");
            
            // SceneLoader 강제 생성
            if (!SceneLoader.Instance)
                CreateManager("SceneLoader", "Managers/SceneLoader");
            
            // UIManager 강제 생성
            if (!UIManager.Instance)
                CreateManager("UIManager", "Managers/UIManager");
            
            // InputManager 강제 생성
            if (!InputManager.Instance)
                CreateManager("InputManager", "Managers/InputManager");
            

            // 필요한 다른 매니저 추가 가능
        }

        /// <summary>
        /// 매니저 동적 생성
        /// </summary>
        /// <param name="managerName">매니저 이름</param>
        /// <param name="resourcePath">리소스 경로</param>
        private static void CreateManager(string managerName, string resourcePath)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"SceneBootstrapper: {managerName} 프리팹을 {resourcePath} 경로에서 찾을 수 없습니다.");
                return;
            }

            var instance = Instantiate(prefab);
            instance.name = managerName; // 생성된 오브젝트 이름 설정
            DontDestroyOnLoad(instance);
            Debug.Log($"{managerName} dynamically created.");
        }
    }
}