using System.Collections.Generic;
using _01_Scripts.Utilities;
using DefaultNamespace;
using Scripts.UI;
using UnityEngine;

namespace Managers
{
    public enum UICategory
    {
        SceneUI,
        PopupUI,
        //게임이 커지면 씬별로 나눠도 됌
    }

    public class UIManager : Singleton<UIManager>
    {
        private readonly Dictionary<UICategory, string> _uiPrefixes = new()
        {
            { UICategory.SceneUI, "UI/Scene/" },
            { UICategory.PopupUI, "UI/Popup/" },
        };


        private int _currentOrder = 10; // 현재까지 최근에 사용된 오더
        private readonly Dictionary<string, UIBase> _activeUIs = new();

        public UIScene CurrentSceneUI { get; private set; }

        private GameObject Root
        {
            get
            {
                var root = GameObject.Find("@UI_Root") ?? new GameObject { name = "@UI_Root" };
                return root;
            }
        }

        public void SetCanvas(GameObject go, bool sort = true)
        {
            var canvas = go.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sort ? _currentOrder++ : 0;
        }

        public T ShowUI<T>( UICategory category = UICategory.SceneUI) where T : UIBase
        {
            return ShowUI(typeof(T).Name,category) as T;
        }

        public T ShowPopup<T>( UICategory category = UICategory.PopupUI) where T : UIBase
        {
            return ShowUI(typeof(T).Name,category) as T;
        }
        public UIBase ShowUI(string uiName, UICategory category = UICategory.PopupUI)
        {
            if (_activeUIs.TryGetValue(uiName, out var existingUI))
            {
                existingUI.gameObject.SetActive(true);
                return existingUI;
            }

            string uiPath = _uiPrefixes[category];
            var prefab = LoadUIResource(uiPath, uiName);
            if (prefab == null)
                return null;

            return CreateUIInstance(prefab, uiName);
        }

        private UIBase CreateUIInstance(GameObject prefab, string uiName)
        {
            var instance = Instantiate(prefab, Root.transform);
            instance.name = uiName;
            var uiComponent = EnableUIComponent<UIBase>(instance, uiName);
            

            return uiComponent;
        }

        private GameObject LoadUIResource(string path, string resourceName)
        {
            var resource = Resources.Load<GameObject>($"{path}{resourceName}");
            if (resource == null)
                Debug.LogError($"UI Resource '{resourceName}' not found in path '{path}'");
            return resource;
        }
        
        public void HideUI<T>() where T : UIBase
        {
            string uiName = typeof(T).Name;

            if (_activeUIs.TryGetValue(uiName, out var ui))
            {
                ui.Hide();
            }
            else
            {
                Debug.LogWarning($"UIManager: {uiName} UI가 활성화 상태가 아닙니다.");
            }
        }

        private T EnableUIComponent<T>(GameObject obj,string uiName) where T : UIBase
        {
            var uiComponent = obj.GetOrAddComponent<T>();
            if (uiComponent is UIPopup popup)
            {
                Debug.Log($"Open UI: {uiName}");
                _activeUIs[uiName] = popup;
            }
            else if (uiComponent is UIScene hud)
            {
                CurrentSceneUI = hud;
            }

            uiComponent.Initialize();
            uiComponent.Show();
            return uiComponent;
        }

        public void ClosePopup<T>()
        {
            ClosePopup(typeof(T).Name);
        }
        public void ClosePopup(string uiName)
        {
            // 딕셔너리에서 제거시도
            if (!_activeUIs.TryGetValue(uiName, out var ui))
            {
                Debug.LogWarning($"Close UI Failed: UI '{uiName}' not found.");
                return;
            }

            ClosePopup(ui);
        }
        public void ClosePopup(UIBase ui)
        {
            Debug.Log($"Close UI: {ui.name}");
            _activeUIs.Remove(ui.name);
            Destroy(ui.gameObject);
            _currentOrder--;
        }
        
        public void CloseAllPopup()
        {
            foreach (var key in _activeUIs.Keys)
            {
                if (_activeUIs[key] is UIPopup popup)
                {
                    ClosePopup(popup);
                }
            }

        }
    }
}