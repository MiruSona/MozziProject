using System.Reflection;
using Mozzi.Resource;
using UnityEditor;
using UnityEngine;

namespace Mozzi.EditorTools
{
    // 코드 상수가 가리키는 프리팹이 실제로 있나 본다.
    // 인스펙터가 아니라 메뉴에서 돌린다.
    public static class PrefabKeysValidator
    {
        [MenuItem("Tools/PrefabKeys 검사")]
        public static void Validate()
        {
            FieldInfo[] fields = typeof(PrefabKeys).GetFields(BindingFlags.Public | BindingFlags.Static);

            int total = 0;
            int missing = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.IsLiteral == false)
                {
                    continue;
                }

                if (field.FieldType != typeof(string))
                {
                    continue;
                }

                string value = (string)field.GetValue(null);
                total++;

                GameObject prefab = Resources.Load<GameObject>(value);
                if (prefab == null)
                {
                    missing++;
                    Debug.LogError("PrefabKeys." + field.Name + " = " + value + " 프리팹이 없다");
                }
            }

            Debug.Log("PrefabKeys 검사 : " + total + " 개 중 " + missing + " 개 없음");
        }
    }
}
