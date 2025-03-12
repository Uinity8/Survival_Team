using UnityEngine;

namespace DefaultNamespace
{
    public static class Extension
    {
        // 확장 메서드
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component 
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }
        
    }
}