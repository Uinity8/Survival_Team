using System;
using TMPro;
using UnityEngine.UI;

namespace Scripts.UI
{
    public class ConfirmPopup : UIPopup
    {
        enum ButtonType
        {
            ConfirmBtn,
            CancelBtn
        }

        enum TestType
        {
            MessageText
        }

        private Action _onConfirm;
        private Action _onCancel;

        /// <summary>
        /// 팝업 초기화
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            AutoBind<Button>(typeof(ButtonType));
            AutoBind<TextMeshProUGUI>(typeof(TestType));

            if (TryGetButton((int)ButtonType.ConfirmBtn, out var confirmBtn))
                confirmBtn.onClick.AddListener(Confirm);

            if (TryGetButton((int)ButtonType.CancelBtn, out var cancelBtn))
                cancelBtn.onClick.AddListener(Cancel);
        }

        /// <summary>
        /// 팝업 설정
        /// </summary>
        public void Setup(string message, Action onConfirmAction, Action onCancelAction = null)
        {
            if (TryGetText((int)TestType.MessageText, out var messageText))
                messageText.text = message;

            _onConfirm = onConfirmAction;
            _onCancel = onCancelAction;
        }

        /// <summary>
        /// 확인 버튼 동작
        /// </summary>
        private void Confirm()
        {
            _onConfirm?.Invoke();
            OnHide();
        }

        /// <summary>
        /// 취소 버튼 동작
        /// </summary>
        private void Cancel()
        {
            _onCancel?.Invoke();
            OnHide();
        }
    }
}