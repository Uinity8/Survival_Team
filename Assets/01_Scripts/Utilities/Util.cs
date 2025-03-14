using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DefaultNamespace
{
    public static class Util
    {
        public static GameObject FindChild(GameObject gameObject, string name = null, bool recursive = false)
        {
            var childTransform = FindChild<Transform>(gameObject, name, recursive);
            return childTransform?.gameObject;
        }

        public static T FindChild<T>(GameObject gameObject, string name = null, bool recursive = false)
            where T : Object
        {
            if (gameObject == null) return null;

            if (!recursive)
            {
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    Transform childTransform = gameObject.transform.GetChild(i);
                    bool isMatchingName = string.IsNullOrEmpty(name) || childTransform.name == name;

                    if (isMatchingName)
                    {
                        T foundComponent = childTransform.GetComponent<T>();
                        if (foundComponent != null)
                            return foundComponent;
                    }
                }
            }
            else
            {
                foreach (T foundComponent in gameObject.GetComponentsInChildren<T>(true))
                {
                    bool isMatchingName = string.IsNullOrEmpty(name) || foundComponent.name == name;
                    if (isMatchingName)
                        return foundComponent;
                }
            }

            return null;
        }


        public static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }


        public static IEnumerator RunAfterFrames(int numOfFrames, Action action)
        {
            for (int i = 0; i < numOfFrames; i++)
                yield return null;

            action.Invoke();
        }

        public static IEnumerator RunAfterDelay(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);

            action.Invoke();
        }
    }
}