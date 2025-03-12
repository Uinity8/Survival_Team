using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Managers
{
    /// <summary>
    /// 씬 로딩 및 전환을 관리하는 클래스.
    /// </summary>
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("Fade Settings")] [SerializeField]
        private CanvasGroup fadeCanvasGroup; // 페이드 효과용 CanvasGroup

        [SerializeField] private float fadeDuration = 1f; // 페이드 시간

        [Header("Loading Screen UI")] [SerializeField]
        private GameObject loadingScreen; // 로딩 화면

        [SerializeField] private Slider progressBar; // 로딩 진행률 바
        [SerializeField] private TextMeshProUGUI progressText; // 진행률 텍스트

        // 상수 선언
        private const float SceneActivationThreshold = 0.9f; // 씬 활성화 기준값
        public event Action OnSceneLoadedCallback;
        
        /// <summary>
        /// 씬을 이름을 기준으로 비동기로 로드합니다.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름</param>
        /// <param name="onSceneLoaded">씬 로드 후 실행할 콜백</param>
        public void LoadScene(string sceneName, Action onSceneLoaded = null)
        {
            PrepareSceneLoad(onSceneLoaded);
            StartCoroutine(LoadSceneAsync(sceneName));
        }

        /// <summary>
        /// 씬 인덱스를 기준으로 비동기로 로드합니다.
        /// </summary>
        /// <param name="buildIndex">씬 빌드 인덱스</param>
        /// <param name="onSceneLoaded">씬 로드 후 실행할 콜백</param>
        public void LoadScene(int buildIndex, Action onSceneLoaded = null)
        {
            string sceneName = SceneManager.GetSceneByBuildIndex(buildIndex).name;
            LoadScene(sceneName, onSceneLoaded);
        }

        /// <summary>
        /// 로딩 준비.
        /// </summary>
        /// <param name="onSceneLoaded">씬 완료 후 실행할 동작</param>
        private void PrepareSceneLoad(Action onSceneLoaded)
        {
            OnSceneLoadedCallback = onSceneLoaded;
        }

        /// <summary>
        /// 씬을 비동기적으로 로드하는 코루틴.
        /// </summary>
        /// <param name="sceneName">로드할 씬 이름</param>
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            loadingScreen?.SetActive(true);

            // 페이드 아웃
            if (fadeCanvasGroup != null)
            {
                yield return StartCoroutine(Fade(1f));
            }

            // 비동기 씬 로드
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation != null)
            {
                operation.allowSceneActivation = false;

                // 로딩 진행 업데이트
                while (!operation.isDone)
                {
                    UpdateLoadingProgress(operation);

                    // 씬 로딩 90% 이상 완료 시 활성화
                    if (operation.progress >= SceneActivationThreshold)
                    {
                        operation.allowSceneActivation = true;
                    }

                    yield return null;
                }
            }

            // 페이드 인
            if (fadeCanvasGroup != null)
            {
                yield return StartCoroutine(Fade(0f));
            }

            loadingScreen?.SetActive(false);
            OnSceneLoadedCallback?.Invoke();
        }

        /// <summary>
        /// 로딩 중 진행률 UI를 업데이트합니다.
        /// </summary>
        /// <param name="operation">비동기 오퍼레이션</param>
        private void UpdateLoadingProgress(AsyncOperation operation)
        {
            if (progressBar == null && progressText == null) return;

            float progress = Mathf.Clamp01(operation.progress / SceneActivationThreshold);

            // UI 업데이트
            if (progressBar != null)
                progressBar.value = progress;

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        /// <summary>
        /// CanvasGroup을 사용한 페이드 효과.
        /// </summary>
        /// <param name="targetAlpha">도달할 알파값</param>
        private IEnumerator Fade(float targetAlpha)
        {
            if (fadeCanvasGroup == null) yield break;

            float startAlpha = fadeCanvasGroup.alpha;
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeDuration);
                yield return null;
            }

            fadeCanvasGroup.alpha = targetAlpha;
        }


    }
}