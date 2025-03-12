using Scripts.UI;
using UnityEngine;

namespace _01_Scripts.UI
{
    public class UIGameScene : UIScene
    {
        public enum HudType
        {
            Conditions,
            DamageIndicator,
            PromptText
        }

        protected void Awake()
        {
            AutoBind<GameObject>(typeof(HudType));
        }
    }
}