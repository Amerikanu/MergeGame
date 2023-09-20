using System;
using UnityEngine;

namespace Leedong
{
    public class InputHandler : MonoBehaviour
    {
        public event Action<Vector2, TouchPhase> OnInputEvent;

        private bool _isMobile = false;

        private void Start()
        {
            _isMobile = Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer;
        }

        private void Update()
        {
            // 모바일 터치 입력 또는 PC 마우스 입력 처리
            if (_isMobile)
            {
                if (Input.touchCount > 0)
                {
                    Touch touch = Input.GetTouch(0);
                    OnInputEvent?.Invoke(touch.position, touch.phase);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    OnInputEvent?.Invoke(Input.mousePosition, TouchPhase.Began);
                }
                else if (Input.GetMouseButton(0))
                {
                    OnInputEvent?.Invoke(Input.mousePosition, TouchPhase.Moved);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    OnInputEvent?.Invoke(Input.mousePosition, TouchPhase.Ended);
                }
            }
        }

    }
}
