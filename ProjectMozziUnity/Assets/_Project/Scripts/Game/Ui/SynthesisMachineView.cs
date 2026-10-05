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
    // 합성소 기계 한 대(칸 하나)의 연출 : 깔때기의 재료 둘 · 창 안에서 도는 두 색 반죽 소용돌이 · 서랍 속 결과 그림자.
    // 기계 몸 그림(상태별 스프라이트)은 화면이 갈아 끼우고, 여기는 그 위에 얹는 것만 맡는다.
    // 모션은 전부 _motions 에 모아 두고, 상태가 바뀌거나 화면이 닫히면 StopFx 가 한 번에 끊는다.
    public sealed class SynthesisMachineView
    {
        // 시작 연출 : 깔때기 속으로 쏙 → 창 안 소용돌이가 돌기 시작 + 번쩍 + 반짝이 → 대기.
        private const float DropSec = 0.3f;
        private const float FlashSec = 0.15f;
        private const float FlashStartScale = 0.3f;
        private const float FlashEndScale = 1.2f;
        private const float SparkleSec = 0.3f;
        private const float SparkleRadiusMin = 30f;
        private const float SparkleRadiusMax = 40f;

        // 결과 그림자가 숨 쉬는 주기.
        private const float PulseSec = 0.6f;
        private const float PulseScale = 1.08f;

        // 재료 둘이 같은 색이면 한 덩어리로 보인다. 둘째 반죽을 이만큼 어둡게 한다.
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

        // 창 안 소용돌이 : 가림판 밑 세 겹(첫째 반죽 · 둘째 반죽 · 외곽선과 반짝이)과 겹마다 프레임 그림.
        private readonly GameObject _mask;
        private readonly Image _swirlA;
        private readonly Image _swirlB;
        private readonly Image _swirlTop;
        private readonly Sprite[] _swirlASprites;
        private readonly Sprite[] _swirlBSprites;
        private readonly Sprite[] _swirlTopSprites;
        private readonly Image _flash;
        private readonly List<Image> _sparkles = new List<Image>();
        private readonly List<Vector2> _sparkleEnds = new List<Vector2>();

        private readonly MozziIconView _outlet;
        private readonly GameObject _outletMark;

        private SynthesisMachineMode _mode = SynthesisMachineMode.None;
        private int _slot = -1;

        // 두 반죽의 제 색(넣은 두 마리 몸 색, 같은 색이면 둘째를 어둡게 한 값) · 지금 물든 정도(0 ~ MixBlendMax) · 지금 보이는 프레임.
        private Color _colorA;
        private Color _colorB;
        private float _blend;
        private int _swirlFrame = -1;

        // 넷이 같은 자식 이름을 쓰므로 반드시 기계 뿌리(Machine_i) 밑에서만 찾는다.
        public SynthesisMachineView(Transform machineRoot, string prefabName, IResourceHub resource, IVfxHub vfx)
        {
            _resource = resource;
            // 화면 연출이라 실제 시간으로 돈다 — 디버그 배속을 올려도 연출 빠르기는 같다.
            _scheduler = vfx.Ui;

            _inletA = new MozziIconView(UiBindUtil.FindObject(machineRoot, UiNames.SynthesisInletA, prefabName).transform, prefabName);
            _inletB = new MozziIconView(UiBindUtil.FindObject(machineRoot, UiNames.SynthesisInletB, prefabName).transform, prefabName);
            _inletAHome = _inletA.Root.anchoredPosition;
            _inletBHome = _inletB.Root.anchoredPosition;

            RectTransform window = UiBindUtil.Find<RectTransform>(machineRoot, UiNames.SynthesisWindowFx, prefabName);
            _mask = UiBindUtil.FindObject(window, UiNames.SynthesisWindowMask, prefabName);
            _swirlA = UiBindUtil.Find<Image>(_mask.transform, UiNames.SynthesisSwirlA, prefabName);
            _swirlB = UiBindUtil.Find<Image>(_mask.transform, UiNames.SynthesisSwirlB, prefabName);
            _swirlTop = UiBindUtil.Find<Image>(_mask.transform, UiNames.SynthesisSwirlTop, prefabName);
            _swirlASprites = LoadSwirl(resource, SpriteKeys.FxSwirlAPrefix);
            _swirlBSprites = LoadSwirl(resource, SpriteKeys.FxSwirlBPrefix);
            _swirlTopSprites = LoadSwirl(resource, SpriteKeys.FxSwirlTopPrefix);
            _flash = UiBindUtil.Find<Image>(window, UiNames.SynthesisMergeFlash, prefabName);
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

            Transform outlet = UiBindUtil.FindObject(machineRoot, UiNames.SynthesisOutletResult, prefabName).transform;
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

        // 대기 중. 같은 칸의 시작 연출이나 소용돌이가 이미 돌고 있으면 건드리지 않는다.
        // 화면을 다시 열었을 때는 들어가는 장면 없이 바로 소용돌이부터 돈다.
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
            SetColors(a, b);
            StartSwirl();
        }

        // 합성 진행률 p(0 → 1). 두 반죽이 서로의 색으로 MixBlendMax·p 만큼 물든다. 화면이 매 프레임 부른다.
        public void SetProgress(float progress)
        {
            _blend = Mathf.Clamp01(progress) * SynthesisRoomLayout.MixBlendMax;
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

        // 합성 시작 순간. 깔때기의 두 마리가 제자리에서 아래로 내려가 그릇 속으로 사라지고(가림 칸이 가장자리 아래를 자른다),
        // 다 들어가면 창 안에서 두 색 반죽이 돌기 시작한다. 찹쌀이 그림은 기계 몸통 앞을 지나지 않는다.
        public void PlayMerge(int slot, MozziSpeciesRow a, MozziSpeciesRow b)
        {
            StopFx();
            _mode = SynthesisMachineMode.Merging;
            _slot = slot;
            _blend = 0f;
            SetColors(a, b);
            ShowInlet(_inletA, _inletAHome, a);
            ShowInlet(_inletB, _inletBHome, b);

            Add(LMotion.Create(0f, 1f, DropSec)
                .WithEase(Ease.InQuad)
                .WithOnComplete(BeginSwirl)
                .WithScheduler(_scheduler)
                .Bind(this, ApplyDrop));
        }

        // 화면을 닫을 때 · 풀로 돌아갈 때. 도는 모션을 다 끊고 그림을 치운다.
        public void Clear()
        {
            StopFx();
            _mode = SynthesisMachineMode.None;
            _slot = -1;
        }

        // 두 마리가 다 들어간 순간. 소용돌이를 켜고, 갑자기 나타나는 티가 안 나게 번쩍 + 반짝이를 같이 터뜨린다.
        private void BeginSwirl()
        {
            HideInlets();
            StartSwirl();

            UiBindUtil.SetActive(_flash.gameObject, true);
            Add(LMotion.Create(0f, 1f, FlashSec)
                .WithOnComplete(HideFlash)
                .WithScheduler(_scheduler)
                .Bind(this, ApplyFlash));

            for (int i = 0; i < _sparkles.Count; i++)
            {
                UiBindUtil.SetActive(_sparkles[i].gameObject, true);
            }

            // 반짝이가 다 퍼지면 시작 연출이 끝난다. 소용돌이는 그대로 돈다.
            Add(LMotion.Create(0f, 1f, SparkleSec)
                .WithEase(Ease.OutQuad)
                .WithOnComplete(EndMerge)
                .WithScheduler(_scheduler)
                .Bind(this, ApplySparkles));
        }

        private void EndMerge()
        {
            HideSparkles();
            _mode = SynthesisMachineMode.Running;
        }

        // 소용돌이 세 겹을 켜고 프레임 고리를 돌린다. 값은 0 → 장수로 흐르고 정수 부분이 프레임 번호다.
        private void StartSwirl()
        {
            UiBindUtil.SetActive(_mask, true);
            _swirlFrame = -1;
            ApplySwirl(0f, this);

            int count = SynthesisRoomLayout.SwirlFrameCount;
            Add(LMotion.Create(0f, (float)count, count * SynthesisRoomLayout.SwirlFrameSec)
                .WithLoops(-1, LoopType.Restart)
                .WithScheduler(_scheduler)
                .Bind(this, ApplySwirl));
        }

        // 깔때기 자리에서 가림 칸 높이만큼 곧장 아래로. 자리는 정수 px.
        private static void ApplyDrop(float t, SynthesisMachineView self)
        {
            Vector2 offset = new Vector2(0f, -Mathf.Round(SynthesisRoomLayout.InletClipHeight * t));
            self._inletA.Root.anchoredPosition = self._inletAHome + offset;
            self._inletB.Root.anchoredPosition = self._inletBHome + offset;
        }

        // 프레임이 바뀔 때만 그림을 갈고, 색은 매번 다시 낸다 (진행률이 화면 쪽에서 계속 바뀐다).
        private static void ApplySwirl(float value, SynthesisMachineView self)
        {
            int frame = Mathf.Min((int)value, SynthesisRoomLayout.SwirlFrameCount - 1);
            if (frame != self._swirlFrame)
            {
                self._swirlFrame = frame;
                self._swirlA.sprite = self._swirlASprites[frame];
                self._swirlB.sprite = self._swirlBSprites[frame];
                self._swirlTop.sprite = self._swirlTopSprites[frame];
            }

            self._swirlA.color = Color.Lerp(self._colorA, self._colorB, self._blend);
            self._swirlB.color = Color.Lerp(self._colorB, self._colorA, self._blend);
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

        private static Sprite[] LoadSwirl(IResourceHub resource, string prefix)
        {
            Sprite[] sprites = new Sprite[SynthesisRoomLayout.SwirlFrameCount];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = resource.GetSprite(prefix + i);
            }

            return sprites;
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
            UiBindUtil.SetActive(_mask, false);
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

        private void SetColors(MozziSpeciesRow a, MozziSpeciesRow b)
        {
            _colorA = MozziArtKeys.BodyColorOf(a);
            _colorB = MozziArtKeys.BodyColorOf(b);
            float gap = Mathf.Abs(_colorA.r - _colorB.r) + Mathf.Abs(_colorA.g - _colorB.g) + Mathf.Abs(_colorA.b - _colorB.b);
            if (gap < SameColorThreshold)
            {
                _colorB = new Color(_colorB.r * SameColorDarken, _colorB.g * SameColorDarken, _colorB.b * SameColorDarken, 1f);
            }
        }
    }
}
