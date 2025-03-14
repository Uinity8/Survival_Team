using System.Collections.Generic;
using UnityEngine;

namespace _01_Scripts.Utilities
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
    
    public static class ListExtension
    {
        public static T GetRandom<T>(this List<T> list)
        {
            return list.Count > 0 ? list[Random.Range(0, list.Count)] : default(T);
        }
    }
}