using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 이름으로 프리팹 자식을 찾는다. 못 찾으면 프리팹 이름과 자식 이름을 담아 바로 던진다.
    public static class UiBindUtil
    {
        public static T Find<T>(Transform root, string childName, string prefabName) where T : Component
        {
            Transform child = FindDeep(root, childName);
            if (child == null)
            {
                throw new InvalidOperationException(prefabName + " 프리팹에 자식 " + childName + " 이 없다");
            }

            T component = child.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(prefabName + " 의 " + childName + " 에 " + typeof(T).Name + " 이 없다");
            }

            return component;
        }

        public static bool TryFind<T>(Transform root, string childName, out T component) where T : Component
        {
            component = null;
            Transform child = FindDeep(root, childName);
            if (child == null)
            {
                return false;
            }

            component = child.GetComponent<T>();
            return component != null;
        }

        public static void SetText(TMP_Text text, string value)
        {
            if (text.text != value)
            {
                text.text = value;
            }
        }

        // 버튼을 찾아 onClick 을 건다. 옛 리스너는 지우므로 다시 Bind 해도 두 번 안 불린다.
        public static Button BindButton(Transform root, string childName, string prefabName, UnityAction onClick)
        {
            Button button = Find<Button>(root, childName, prefabName);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(onClick);
            return button;
        }

        // 뿌리 아래(꺼진 것 포함) 모든 버튼의 onClick 을 뗀다. 풀로 돌아갈 때 옛 Manager 를 부르는 리스너를 남기지 않는다.
        public static void UnbindAllButtons(Transform root)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
            }
        }

        // 버튼 안 "Label" 글자. 없으면 null.
        public static TMP_Text LabelOf(Button button)
        {
            TMP_Text label;
            TryFind<TMP_Text>(button.transform, UiNames.ButtonLabel, out label);
            return label;
        }

        public static void SetLabel(Button button, string value)
        {
            TMP_Text label = LabelOf(button);
            if (label != null)
            {
                SetText(label, value);
            }
        }

        public static GameObject FindObject(Transform root, string childName, string prefabName)
        {
            Transform child = FindDeep(root, childName);
            if (child == null)
            {
                throw new InvalidOperationException(prefabName + " 프리팹에 자식 " + childName + " 이 없다");
            }

            return child.gameObject;
        }

        public static void SetActive(GameObject go, bool active)
        {
            if (go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }

        // 깊이 우선. 화면 하나의 자식 수는 수십이라 충분하다. Editor 손질기도 같이 쓴다.
        public static Transform FindDeep(Transform root, string childName)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform deeper = FindDeep(child, childName);
                if (deeper != null)
                {
                    return deeper;
                }
            }

            return null;
        }
    }
}
