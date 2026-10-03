using System.Collections.Generic;
using LitMotion;
using Mozzi.Data;
using Mozzi.Game.Vfx;
using Mozzi.Game.View;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 합성소 화면 가운데의 기계 한 대. 보고 있는 칸의 상태를 그림으로 보여 준다 :
    // 투입구의 재료 둘 · 창 안의 음양 연출 · 배출구 앞의 결과 그림자.
    // 모션은 전부 _motions 에 모아 두고, 상태가 바뀌거나 화면이 닫히면 StopFx 가 한 번에 끊는다.
    public sealed class SynthesisMachineView
    {
        // 시작 연출 : 빨려 들어감 → 두 바퀴 돌며 줄어듦 → 번쩍 + 반짝이 → 대기 회전.
        private const float SuckSec = 0.25f;
        private const float SuckEndScale = 0.25f;
        private const float SpinSec = 0.8f;
        private const float SpinTurns = 2f;
        private const float SpinEndScale = 0.35f;
        private const float FlashSec = 0.15f;
        private const float FlashStartScale = 0.3f;
        private const float FlashEndScale = 1.2f;
        private const float SparkleSec = 0.3f;
        private const float SparkleRadiusMin = 30f;
        private const float SparkleRadiusMax = 40f;
        private const float RecoverSec = 0.3f;

        // 대기 중 한 바퀴 도는 시간 · 결과 그림자가 숨 쉬는 주기.
        private const float LoopTurnSec = 4f;
        private const float PulseSec = 0.6f;
        private const float PulseScale = 1.08f;

        // 재료 둘이 같은 색이면 음양이 한 덩어리로 보인다. 둘째 반쪽을 이만큼 어둡게 한다.
        private const float SameColorDarken = 0.72f;
        private const float SameColorThreshold = 0.06f;

        private static readonly Color SilhouetteColor = new Color(0.17f, 0.11f, 0.19f, 1f);

        private readonly IResourceHub _resource;
        private readonly IMotionScheduler _scheduler;
        private readonly CompositeMotionHandle _motions = new CompositeMotionHandle();

        private readonly MozziIconView _inletA;
        private readonly MozziIconView _inletB;
        private readonly Vector2 _inletAHome;
        private readonly Vector2 _inletBHome;
        private readonly Vector2 _windowPos;

        private readonly RectTransform _yinRoot;
        private readonly Image _halfA;
        private readonly Image _halfB;
        private readonly Image _flash;
        private readonly List<Image> _sparkles = new List<Image>();
        private readonly List<Vector2> _sparkleEnds = new List<Vector2>();

        private readonly MozziIconView _outlet;
        private readonly GameObject _outletMark;

        private SynthesisMachineMode _mode = SynthesisMachineMode.None;
        private int _slot = -1;

        public SynthesisMachineView(Transform scene, string prefabName, IResourceHub resource, IVfxHub vfx)
        {
            _resource = resource;
            // 화면 연출이라 실제 시간으로 돈다 — 디버그 배속을 올려도 연출 빠르기는 같다.
            _scheduler = vfx.Ui;

            Image machine = UiBindUtil.Find<Image>(scene, UiNames.SynthesisMachine, prefabName);
            machine.sprite = resource.GetSprite(SpriteKeys.ScreenSynthesisMachine);

            _inletA = new MozziIconView(UiBindUtil.FindObject(scene, UiNames.SynthesisInletA, prefabName).transform, prefabName);
            _inletB = new MozziIconView(UiBindUtil.FindObject(scene, UiNames.SynthesisInletB, prefabName).transform, prefabName);
            _inletAHome = _inletA.Root.anchoredPosition;
            _inletBHome = _inletB.Root.anchoredPosition;

            RectTransform window = UiBindUtil.Find<RectTransform>(scene, UiNames.SynthesisWindowFx, prefabName);
            _windowPos = window.anchoredPosition;
            _yinRoot = UiBindUtil.Find<RectTransform>(window, UiNames.SynthesisYinRoot, prefabName);
            _halfA = UiBindUtil.Find<Image>(window, UiNames.SynthesisYinHalfA, prefabName);
            _halfB = UiBindUtil.Find<Image>(window, UiNames.SynthesisYinHalfB, prefabName);
            _flash = UiBindUtil.Find<Image>(window, UiNames.SynthesisMergeFlash, prefabName);

            Sprite half = resource.GetSprite(SpriteKeys.FxYinYangHalf);
            _halfA.sprite = half;
            _halfB.sprite = half;
            _flash.sprite = resource.GetSprite(SpriteKeys.FxMergeFlash);

            Sprite sparkle = resource.GetSprite(SpriteKeys.FxSparkle);
            for (int i = 0; i < UiNames.SynthesisSparkleCount; i++)
            {
                Image image = UiBindUtil.Find<Image>(window, UiNames.SynthesisSparklePrefix + i, prefabName);
                image.sprite = sparkle;
                _sparkles.Add(image);

                // 다섯 방향으로 고르게, 반지름은 번갈아 30 · 40.
                float angle = (90f + 360f * i / UiNames.SynthesisSparkleCount) * Mathf.Deg2Rad;
                float radius = SparkleRadiusMin;
                if (i % 2 == 1)
                {
                    radius = SparkleRadiusMax;
                }

                _sparkleEnds.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            Transform outlet = UiBindUtil.FindObject(scene, UiNames.SynthesisOutletResult, prefabName).transform;
            _outlet = new MozziIconView(outlet, prefabName);
            _outletMark = UiBindUtil.FindObject(outlet, UiNames.SynthesisOutletMark, prefabName);

            HideAll();
        }

        // 빈 칸 · 잠긴 칸. 고른 재료(없으면 null)만 투입구에 얹는다.
        public void ShowIdle(int slot, MozziSpeciesRow a, MozziSpeciesRow b)
        {
            if (_mode != SynthesisMachineMode.Idle || _slot != slot)
            {
                StopFx();
            }

            _mode = SynthesisMachineMode.Idle;
            _slot = slot;
            ShowInlet(_inletA, _inletAHome, a);
            ShowInlet(_inletB, _inletBHome, b);
        }

        // 대기 중. 같은 칸의 시작 연출이나 대기 회전이 이미 돌고 있으면 건드리지 않는다.
        public void ShowRunning(int slot, MozziSpeciesRow a, MozziSpeciesRow b)
        {
            bool playing = _mode == SynthesisMachineMode.Merging || _mode == SynthesisMachineMode.Running;
            if (playing && _slot == slot)
            {
                return;
            }

            StopFx();
            _mode = SynthesisMachineMode.Running;
            _slot = slot;
            SetHalfColors(a, b);
            ShowHalves(1f);
            StartLoop();
        }

        // 다 됨. 받기 전이라 무엇이 나왔는지는 그림자로만 보인다.
        public void ShowDone(int slot, MozziSpeciesRow shapeOf)
        {
            if (_mode == SynthesisMachineMode.Done && _slot == slot)
            {
                return;
            }

            StopFx();
            _mode = SynthesisMachineMode.Done;
            _slot = slot;
            _outlet.ShowSilhouette(_resource, shapeOf, SilhouetteColor);
            UiBindUtil.SetActive(_outletMark, true);

            Add(LMotion.Create(1f, PulseScale, PulseSec)
                .WithEase(Ease.InOutSine)
                .WithLoops(-1, LoopType.Yoyo)
                .WithScheduler(_scheduler)
                .Bind(this, ApplyPulse));
        }

        // 합성 시작 순간. 투입구의 두 마리가 창으로 빨려 들어가고 음양이 합쳐진다. 끝나면 대기 회전으로 이어진다.
        public void PlayMerge(int slot, MozziSpeciesRow a, MozziSpeciesRow b)
        {
            StopFx();
            _mode = SynthesisMachineMode.Merging;
            _slot = slot;
            SetHalfColors(a, b);
            ShowInlet(_inletA, _inletAHome, a);
            ShowInlet(_inletB, _inletBHome, b);

            Add(LMotion.Create(0f, 1f, SuckSec)
                .WithEase(Ease.InQuad)
                .WithOnComplete(BeginSpin)
                .WithScheduler(_scheduler)
                .Bind(this, ApplySuck));
        }

        // 화면을 닫을 때 · 풀로 돌아갈 때. 도는 모션을 다 끊고 그림을 치운다.
        public void Clear()
        {
            StopFx();
            _mode = SynthesisMachineMode.None;
            _slot = -1;
        }

        private void BeginSpin()
        {
            HideInlets();
            ShowHalves(1f);

            Add(LMotion.Create(0f, 1f, SpinSec)
                .WithEase(Ease.InQuad)
                .WithOnComplete(BeginFlash)
                .WithScheduler(_scheduler)
                .Bind(this, ApplySpin));
        }

        private void BeginFlash()
        {
            UiBindUtil.SetActive(_flash.gameObject, true);
            Add(LMotion.Create(0f, 1f, FlashSec)
                .WithOnComplete(HideFlash)
                .WithScheduler(_scheduler)
                .Bind(this, ApplyFlash));

            for (int i = 0; i < _sparkles.Count; i++)
            {
                UiBindUtil.SetActive(_sparkles[i].gameObject, true);
            }

            Add(LMotion.Create(0f, 1f, SparkleSec)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(HideSparkles)
                .WithScheduler(_scheduler)
                .Bind(this, ApplySparkles));

            Add(LMotion.Create(SpinEndScale, 1f, RecoverSec)
                .WithEase(Ease.OutBack)
                .WithOnComplete(EndMerge)
                .WithScheduler(_scheduler)
                .Bind(this, ApplyHalfScale));
        }

        private void EndMerge()
        {
            _mode = SynthesisMachineMode.Running;
            StartLoop();
        }

        private void StartLoop()
        {
            _yinRoot.localEulerAngles = Vector3.zero;
            Add(LMotion.Create(0f, -360f, LoopTurnSec)
                .WithLoops(-1, LoopType.Restart)
                .WithScheduler(_scheduler)
                .Bind(this, ApplyRotation));
        }

        private static void ApplySuck(float t, SynthesisMachineView self)
        {
            float scale = Mathf.Lerp(1f, SuckEndScale, t);
            self._inletA.Root.anchoredPosition = Vector2.Lerp(self._inletAHome, self._windowPos, t);
            self._inletB.Root.anchoredPosition = Vector2.Lerp(self._inletBHome, self._windowPos, t);
            self._inletA.Root.localScale = new Vector3(scale, scale, 1f);
            self._inletB.Root.localScale = new Vector3(scale, scale, 1f);
        }

        private static void ApplySpin(float t, SynthesisMachineView self)
        {
            ApplyRotation(-360f * SpinTurns * t, self);
            ApplyHalfScale(Mathf.Lerp(1f, SpinEndScale, t), self);
        }

        private static void ApplyRotation(float degrees, SynthesisMachineView self)
        {
            self._yinRoot.localEulerAngles = new Vector3(0f, 0f, degrees);
        }

        private static void ApplyHalfScale(float scale, SynthesisMachineView self)
        {
            self._yinRoot.localScale = new Vector3(scale, scale, 1f);
        }

        private static void ApplyFlash(float t, SynthesisMachineView self)
        {
            float scale = Mathf.Lerp(FlashStartScale, FlashEndScale, t);
            self._flash.rectTransform.localScale = new Vector3(scale, scale, 1f);
            self._flash.color = new Color(1f, 1f, 1f, 1f - t);
        }

        private static void ApplySparkles(float t, SynthesisMachineView self)
        {
            Color color = new Color(1f, 1f, 1f, 1f - t);
            for (int i = 0; i < self._sparkles.Count; i++)
            {
                self._sparkles[i].rectTransform.anchoredPosition = self._sparkleEnds[i] * t;
                self._sparkles[i].color = color;
            }
        }

        private static void ApplyPulse(float scale, SynthesisMachineView self)
        {
            self._outlet.Root.localScale = new Vector3(scale, scale, 1f);
        }

        private void Add(MotionHandle handle)
        {
            _motions.Add(handle);
        }

        // 도는 모션을 다 끊고 그림을 처음 자리로 되돌려 끈다.
        private void StopFx()
        {
            _motions.Cancel();
            HideAll();
        }

        private void HideAll()
        {
            HideInlets();
            _yinRoot.localEulerAngles = Vector3.zero;
            _yinRoot.localScale = Vector3.one;
            UiBindUtil.SetActive(_yinRoot.gameObject, false);
            HideFlash();
            HideSparkles();
            _outlet.Root.localScale = Vector3.one;
            _outlet.Hide();
            UiBindUtil.SetActive(_outletMark, false);
        }

        private void HideInlets()
        {
            ResetInlet(_inletA, _inletAHome);
            ResetInlet(_inletB, _inletBHome);
        }

        private static void ResetInlet(MozziIconView inlet, Vector2 home)
        {
            inlet.Root.anchoredPosition = home;
            inlet.Root.localScale = Vector3.one;
            inlet.Hide();
        }

        private void ShowInlet(MozziIconView inlet, Vector2 home, MozziSpeciesRow species)
        {
            ResetInlet(inlet, home);
            if (species != null)
            {
                inlet.Show(_resource, species);
            }
        }

        private void HideFlash()
        {
            UiBindUtil.SetActive(_flash.gameObject, false);
        }

        private void HideSparkles()
        {
            for (int i = 0; i < _sparkles.Count; i++)
            {
                _sparkles[i].rectTransform.anchoredPosition = Vector2.zero;
                UiBindUtil.SetActive(_sparkles[i].gameObject, false);
            }
        }

        private void ShowHalves(float scale)
        {
            _yinRoot.localEulerAngles = Vector3.zero;
            _yinRoot.localScale = new Vector3(scale, scale, 1f);
            UiBindUtil.SetActive(_yinRoot.gameObject, true);
        }

        private void SetHalfColors(MozziSpeciesRow a, MozziSpeciesRow b)
        {
            Color colorA = MozziArtKeys.BodyColorOf(a);
            Color colorB = MozziArtKeys.BodyColorOf(b);
            float gap = Mathf.Abs(colorA.r - colorB.r) + Mathf.Abs(colorA.g - colorB.g) + Mathf.Abs(colorA.b - colorB.b);
            if (gap < SameColorThreshold)
            {
                colorB = new Color(colorB.r * SameColorDarken, colorB.g * SameColorDarken, colorB.b * SameColorDarken, 1f);
            }

            _halfA.color = colorA;
            _halfB.color = colorB;
        }
    }
}
