using UnityEngine;
using System;
using System.Collections.Generic;
using DefaultNamespace;
using Scripts.UI;
using TMPro;
using UnityEngine.UI;

public abstract class UIBase : MonoBehaviour
{
    private readonly Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();
    
    
    /// <summary>
    /// UI 초기화 (1회만 호출)
    /// </summary>
    public virtual void Initialize()
    {
        Debug.Log($"{gameObject.name} Initialized");
    }

    /// <summary>
    /// UI가 활성화
    /// </summary>
    public virtual void Show()
    {
        OnShow();
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// UI가 활성화될 때 호출
    /// </summary>
    protected virtual void OnShow()
    {
        Debug.Log($"{gameObject.name} Shown");
    }

    /// <summary>
    /// UI 비활성화
    /// </summary>
    public virtual void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// UI가 비활성화될 때 호출
    /// </summary>
    protected virtual void OnHide()
    {
        Debug.Log($"{gameObject.name} Hidden");
    }
    
    // 자동 매핑 함수
    protected void AutoBind<T>(Type enumType) where T : UnityEngine.Object
    {
        string[] names = Enum.GetNames(enumType);
        UnityEngine.Object[] mappedObjects = new UnityEngine.Object[names.Length];
        _objects.Add(typeof(T), mappedObjects);

        for (int i = 0; i < names.Length; i++)
        {
            mappedObjects[i] = FindObject<T>(names[i]);

            if (mappedObjects[i] == null)
                Debug.Log($"Failed to bind({names[i]})");
        }
    }

    private T FindObject<T>(string objName) where T : UnityEngine.Object
    {
        return typeof(T) == typeof(GameObject)
            ? Util.FindChild(gameObject, objName, true) as T
            : Util.FindChild<T>(gameObject, objName, true);
    }

    private T Get<T>(int index) where T : UnityEngine.Object
    {
        return _objects.TryGetValue(typeof(T), out var mappedObjects) ? mappedObjects[index] as T : null;
    }

    

    //================================
    // Find 메서드
    //===============================
    public GameObject GetObject(int index) => Get<GameObject>(index);
    public TextMeshProUGUI GetTextFromGameObject(int index) => Get<GameObject>(index).GetComponent<TextMeshProUGUI>();
    public Button GetButtonFromGameObject(int index) => Get<GameObject>(index).GetComponent<Button>();
    public Image GetImageFromGameObject(int index) => Get<GameObject>(index).GetComponent<Image>();
    protected TextMeshProUGUI GetText(int index) => Get<TextMeshProUGUI>(index);
    protected Button GetButton(int index) => Get<Button>(index);
    protected Image GetImage(int index) => Get<Image>(index);
    
    public bool TryGetComponentFromGameObject<T>(int index, out T component) where T : Component
    {
        component = null;

        var gameObject = Get<GameObject>(index);
        if (gameObject != null)
        {
            component = gameObject.GetComponent<T>();
        }

        return component != null;
    }

    public bool TryGetTextFromGameObject(int index, out TextMeshProUGUI text)
    {
        return TryGetComponentFromGameObject(index, out text);
    }

    public bool TryGetButtonFromGameObject(int index, out Button button)
    {
        return TryGetComponentFromGameObject(index, out button);
    }

    public bool TryGetImageFromGameObject(int index, out Image image)
    {
        return TryGetComponentFromGameObject(index, out image);
    }

    public bool TryGetText(int index, out TextMeshProUGUI text)
    {
        text = Get<TextMeshProUGUI>(index);
        return text != null;
    }

    public bool TryGetButton(int index, out Button button)
    {
        button = Get<Button>(index);
        return button != null;
    }

}