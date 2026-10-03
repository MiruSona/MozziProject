using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mozzi.Game.Ui
{
    // UI 위젯에서 드래그가 시작된 것만 알린다. 그 뒤 손가락 따라가기는 월드 쪽(MozziDragAgent)이 포인터를 직접 읽는다.
    // 코드가 AddComponent 로 붙이고 OnBegin 을 건다 — 프리팹에는 안 넣는다. IDragHandler 가 있어야 EventSystem 이 드래그로 본다.
    public sealed class UiDragSource : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public Action OnBegin;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (OnBegin != null)
            {
                OnBegin();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
        }
    }
}
