#if UNITY_EDITOR || MOZZI_DEBUG
using System;
using System.Collections.Generic;
using Mozzi.Core;
using Mozzi.Data;
using Mozzi.Data.Rows;
using Mozzi.Game.Farm;
using Mozzi.Game.Stall;
using Mozzi.Game.Synthesis;
using Mozzi.Game.Wallet;
using Mozzi.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Mozzi.Game.Ui
{
    // 판정용 디버그 패널 (세로조각1판설계 4-7). 오른쪽 위 「D」 로 접었다 편다. 에디터와 MOZZI_DEBUG 정의 빌드에서만 컴파일된다.
    // 배속 ×1/×5/×10 · 20마리 채우기 · 골드 +1,000 · 5분 건너뛰기 · 세이브 지우기.
    public sealed class DebugPanel : ObjectBase
    {
        private const string PrefabName = "DebugPanel";
        private const int GoldAmount = 1000;
        private const double SkipSeconds = 300;

        private IFarmHub _farm;
        private IWalletHub _wallet;
        private ISynthesisHub _synthesis;
        private IStallHub _stall;
        private ISaveHub _save;
        private IStaticDataHub _data;

        private GameObject _body;
        private Action<double> _onSkipped;
        private bool _bound;

        // onSkipped 는 UiManager 가 자원바를 되읽고 돌아왔어요 팝업을 띄우는 자리다.
        public void Bind(Action<double> onSkipped)
        {
            if (_bound)
            {
                return;
            }

            _farm = InGameHub.Get<IFarmHub>();
            _wallet = InGameHub.Get<IWalletHub>();
            _synthesis = InGameHub.Get<ISynthesisHub>();
            _stall = InGameHub.Get<IStallHub>();
            _save = InGameHub.Get<ISaveHub>();
            _data = InGameHub.Get<IStaticDataHub>();
            _onSkipped = onSkipped;

            _body = UiBindUtil.FindObject(transform, UiNames.DebugBody, PrefabName);
            UiBindUtil.BindButton(transform, UiNames.DebugToggleButton, PrefabName, OnToggle);
            UiBindUtil.BindButton(transform, UiNames.DebugSpeed1Button, PrefabName, () => SetSpeed(1f));
            UiBindUtil.BindButton(transform, UiNames.DebugSpeed5Button, PrefabName, () => SetSpeed(5f));
            UiBindUtil.BindButton(transform, UiNames.DebugSpeed10Button, PrefabName, () => SetSpeed(10f));
            UiBindUtil.BindButton(transform, UiNames.DebugFillButton, PrefabName, OnFill);
            UiBindUtil.BindButton(transform, UiNames.DebugGoldButton, PrefabName, OnGold);
            UiBindUtil.BindButton(transform, UiNames.DebugSkipButton, PrefabName, OnSkip);
            UiBindUtil.BindButton(transform, UiNames.DebugDeleteSaveButton, PrefabName, OnDeleteSave);

            UiBindUtil.SetActive(_body, false);
            _bound = true;
        }

        // 풀로 돌아갈 때 배선을 푼다. 「세이브 지우기」 뒤 같은 인스턴스가 새 Manager 로 다시 Bind 된다.
        public override void OnReturn()
        {
            base.OnReturn();
            UiBindUtil.UnbindAllButtons(transform);
            _farm = null;
            _wallet = null;
            _synthesis = null;
            _stall = null;
            _save = null;
            _data = null;
            _onSkipped = null;
            _body = null;
            _bound = false;
        }

        private void OnToggle()
        {
            UiBindUtil.SetActive(_body, _body.activeSelf == false);
        }

        private static void SetSpeed(float scale)
        {
            GameMaster.TimeScale = scale;
            Debug.Log("[Debug] 배속 ×" + scale);
        }

        // 표의 아무 종이나 담장이 찰 때까지 놓는다 (변종도 섞인다 — 테두리 반짝임 확인용).
        private void OnFill()
        {
            IReadOnlyList<MozziSpeciesRow> rows = _data.Species.All;
            int placed = 0;
            while (_farm.MozziCount < _farm.FenceMax)
            {
                MozziSpeciesRow row = rows[UnityEngine.Random.Range(0, rows.Count)];
                if (_farm.Place(row.id) == false)
                {
                    break;
                }

                placed++;
            }

            Debug.Log("[Debug] 농장 채움 +" + placed + " → " + _farm.MozziCount + "/" + _farm.FenceMax);
        }

        private void OnGold()
        {
            _wallet.Earn(GoldAmount);
        }

        // 오프라인 진행과 같은 길. 순서도 PostInit 과 같다 (농장 → 합성 → 가판대).
        private void OnSkip()
        {
            _farm.Skip(SkipSeconds);
            _synthesis.Skip(SkipSeconds);
            _stall.Skip(SkipSeconds);
            Debug.Log("[Debug] " + SkipSeconds + "초 건너뜀 · " + _farm.OfflineReport.Describe()
                + " · 합성 완료 " + _synthesis.OfflineDoneCount + " · " + _stall.OfflineReport.Describe());
            _onSkipped(SkipSeconds);
        }

        private void OnDeleteSave()
        {
            _save.DeleteAndRestart();
        }
    }
}
#endif
