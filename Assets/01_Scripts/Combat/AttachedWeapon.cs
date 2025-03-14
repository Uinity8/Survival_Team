using UnityEditor;
using UnityEngine;

namespace _01_Scripts.Combat
{
    public class AttachedWeapon : MonoBehaviour
    {
        public WeaponData weapon;
        public GameObject trail;

        [HideInInspector]
        public GameObject unEquippedWeaponModel;
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(AttachedWeapon))]
    public class AttachedWeaponEditor : Editor
    {
        SerializedProperty unEquippedWeapon;
        bool foldOutExpanded = false;

        private void OnEnable()
        {
            unEquippedWeapon = serializedObject.FindProperty("unEquippedWeaponModel");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            foldOutExpanded = EditorGUILayout.Foldout(foldOutExpanded, "Advanced");
            if (foldOutExpanded)
            {
                EditorGUILayout.PropertyField(unEquippedWeapon);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}