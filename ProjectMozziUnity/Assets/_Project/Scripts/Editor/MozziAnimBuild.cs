using Mozzi.Game.View;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mozzi.EditorTools
{
    // 찹쌀이 애니 클립·컨트롤러와 레이어 프리팹을 코드로 굽는다. 손으로 찍지 않는 것이 규칙이라
    // 프레임 표가 여기 상수로 있고 메뉴를 누르면 늘 같은 것이 나온다.
    // 클립은 스프라이트를 안 물고 MozziVisual._bodyFrame 숫자와 Offset 자리만 흔든다 — 12종이 한 벌을 쓴다.
    public static class MozziAnimBuild
    {
        private const string AnimFolder = "Assets/_Project/Art/Anim";
        private const string IdlePath = AnimFolder + "/MozziIdle.anim";
        private const string HopPath = AnimFolder + "/MozziHop.anim";
        private const string ControllerPath = AnimFolder + "/Mozzi.controller";
        private const string PrefabPath = "Assets/_Project/Resources/Prefabs/Mozzi.prefab";

        private const string BodyFrameField = "_bodyFrame";
        private const string PositionX = "m_LocalPosition.x";
        private const string PositionY = "m_LocalPosition.y";

        private const float HopSec = 0.35f;
        private const float IdleSec = 0.5f;
        private const float ClipFrameRate = 60f;

        // 프레임 표. 칸 순서는 hop0 기본 · hop1 납작 · hop2 길쭉 · hop3 살짝 길쭉.
        // Offset 은 px 이고 Unity 처럼 +y 가 위다. 오른볼 x 는 왼볼의 부호 반대다.
        private static readonly float[] FrameBody = { 0f, 1f, 2f, 3f };
        private static readonly float[] FrameFaceY = { 0f, -3f, 3f, 1f };
        private static readonly float[] FrameCheekX = { 0f, -2f, 1f, 0f };
        private static readonly float[] FrameCheekY = { 0f, -3f, 3f, 1f };
        private static readonly float[] FrameAccX = { 0f, 2f, -2f, -1f };
        private static readonly float[] FrameAccY = { 0f, -6f, 5f, 2f };

        // Hop 클립 키 시각(진행도) 과 그 시각에 쓸 프레임 번호.
        private static readonly float[] HopKeyProgress = { 0f, 0.10f, 0.50f, 0.85f, 1f };
        private static readonly int[] HopKeyFrame = { 1, 2, 3, 1, 0 };

        [MenuItem("Tools/찹쌀이/애니 다시 굽기")]
        public static void BuildAnim()
        {
            EnsureFolder();

            AnimationClip idle = LoadOrCreateClip(IdlePath);
            WriteCurves(idle, true);
            SetLoop(idle, true);

            AnimationClip hop = LoadOrCreateClip(HopPath);
            WriteCurves(hop, false);
            SetLoop(hop, false);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // 파라미터·전이는 0 이다. 「언제 뛰나」는 코드가 Play 로 정한다.
            while (controller.parameters.Length > 0)
            {
                controller.RemoveParameter(0);
            }

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            ChildAnimatorState[] old = machine.states;
            for (int i = 0; i < old.Length; i++)
            {
                machine.RemoveState(old[i].state);
            }

            AnimatorState idleState = machine.AddState(ViewNames.AnimIdle, new Vector3(260f, 0f, 0f));
            idleState.motion = idle;
            AnimatorState hopState = machine.AddState(ViewNames.AnimHop, new Vector3(260f, 80f, 0f));
            hopState.motion = hop;
            machine.defaultState = idleState;

            EditorUtility.SetDirty(idle);
            EditorUtility.SetDirty(hop);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("찹쌀이 애니 굽기 끝 : " + IdlePath + " · " + HopPath + " · " + ControllerPath);
        }

        [MenuItem("Tools/찹쌀이/프리팹 다시 짜기")]
        public static void BuildPrefab()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError(ControllerPath + " 가 없다. 먼저 「애니 다시 굽기」를 누른다");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                // 몸은 Visual/Body 가 그린다. BoxCollider2D · MozziObject 는 그대로 둔다.
                SpriteRenderer rootRenderer = root.GetComponent<SpriteRenderer>();
                if (rootRenderer != null)
                {
                    Object.DestroyImmediate(rootRenderer);
                }

                if (root.GetComponent<SortingGroup>() == null)
                {
                    root.AddComponent<SortingGroup>();
                }

                for (int i = root.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                }

                Transform shadow = MakeChild(root.transform, ViewNames.MozziShadow);
                MakeRenderer(shadow, -1);
                shadow.gameObject.SetActive(false);

                Transform visual = MakeChild(root.transform, ViewNames.MozziVisual);
                Animator animator = visual.gameObject.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.speed = 0f;
                visual.gameObject.AddComponent<MozziVisual>();

                Transform border = MakeChild(visual, ViewNames.MozziBorder);
                SpriteRenderer borderRenderer = MakeRenderer(border, 0);
                borderRenderer.color = new Color(1f, 1f, 0.6f, 1f);
                border.gameObject.SetActive(false);

                MakeRenderer(MakeChild(visual, ViewNames.MozziBody), 1);

                Transform cheekL = MakeChain(visual, ViewNames.MozziCheekAnchorL, ViewNames.MozziCheekOffsetL, ViewNames.MozziCheek);
                MakeRenderer(cheekL, 2);
                Transform cheekR = MakeChain(visual, ViewNames.MozziCheekAnchorR, ViewNames.MozziCheekOffsetR, ViewNames.MozziCheek);
                MakeRenderer(cheekR, 2).flipX = true;

                Transform face = MakeChain(visual, ViewNames.MozziFaceAnchor, ViewNames.MozziFaceOffset, ViewNames.MozziFace);
                MakeRenderer(face, 3);
                Transform acc = MakeChain(visual, ViewNames.MozziAccAnchor, ViewNames.MozziAccOffset, ViewNames.MozziAcc);
                MakeRenderer(acc, 4);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.Refresh();
            Debug.Log("찹쌀이 프리팹 다시 짜기 끝 : " + PrefabPath);
        }

        private static void EnsureFolder()
        {
            if (AssetDatabase.IsValidFolder("Assets/_Project/Art") == false)
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Art");
            }

            if (AssetDatabase.IsValidFolder(AnimFolder) == false)
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Anim");
            }
        }

        // GUID 가 안 바뀌게 있는 에셋을 고쳐 쓴다.
        private static AnimationClip LoadOrCreateClip(string path)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            clip.frameRate = ClipFrameRate;
            return clip;
        }

        private static void SetLoop(AnimationClip clip, bool loop)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        // 커브 8개. Idle 은 같은 8개를 전부 0 상수로 둬 상태를 바꿀 때 지난 값이 안 남는다.
        private static void WriteCurves(AnimationClip clip, bool isIdle)
        {
            float unit = 1f / LayoutConst.PixelsPerUnit;
            string cheekL = ViewNames.MozziCheekAnchorL + "/" + ViewNames.MozziCheekOffsetL;
            string cheekR = ViewNames.MozziCheekAnchorR + "/" + ViewNames.MozziCheekOffsetR;
            string face = ViewNames.MozziFaceAnchor + "/" + ViewNames.MozziFaceOffset;
            string acc = ViewNames.MozziAccAnchor + "/" + ViewNames.MozziAccOffset;

            EditorCurveBinding body = EditorCurveBinding.FloatCurve(string.Empty, typeof(MozziVisual), BodyFrameField);
            AnimationUtility.SetEditorCurve(clip, body, MakeCurve(isIdle, FrameBody, 1f));

            SetTransformCurve(clip, face, PositionY, isIdle, FrameFaceY, unit);
            SetTransformCurve(clip, cheekL, PositionX, isIdle, FrameCheekX, unit);
            SetTransformCurve(clip, cheekL, PositionY, isIdle, FrameCheekY, unit);
            SetTransformCurve(clip, cheekR, PositionX, isIdle, FrameCheekX, -unit);
            SetTransformCurve(clip, cheekR, PositionY, isIdle, FrameCheekY, unit);
            SetTransformCurve(clip, acc, PositionX, isIdle, FrameAccX, unit);
            SetTransformCurve(clip, acc, PositionY, isIdle, FrameAccY, unit);
        }

        private static void SetTransformCurve(AnimationClip clip, string path, string property, bool isIdle, float[] perFrame, float scale)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), property);
            AnimationUtility.SetEditorCurve(clip, binding, MakeCurve(isIdle, perFrame, scale));
        }

        private static AnimationCurve MakeCurve(bool isIdle, float[] perFrame, float scale)
        {
            AnimationCurve curve = new AnimationCurve();
            if (isIdle)
            {
                curve.AddKey(new Keyframe(0f, 0f));
                curve.AddKey(new Keyframe(IdleSec, 0f));
            }
            else
            {
                for (int i = 0; i < HopKeyProgress.Length; i++)
                {
                    float time = HopKeyProgress[i] * HopSec;
                    float value = perFrame[HopKeyFrame[i]] * scale;
                    curve.AddKey(new Keyframe(time, value));
                }
            }

            // 계단식이라 프레임 사이가 섞이지 않는다.
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }

            return curve;
        }

        private static Transform MakeChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        // Anchor 는 코드가 놓는 기준 자리, Offset 은 클립만 움직인다. 그림은 Offset 의 자식이 그린다.
        private static Transform MakeChain(Transform parent, string anchorName, string offsetName, string leafName)
        {
            Transform anchor = MakeChild(parent, anchorName);
            Transform offset = MakeChild(anchor, offsetName);
            return MakeChild(offset, leafName);
        }

        private static SpriteRenderer MakeRenderer(Transform target, int order)
        {
            SpriteRenderer renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            return renderer;
        }
    }
}
