using System;
using Mozzi.Core;
using Mozzi.Events;
using Mozzi.Save;
using UnityEngine;

namespace Mozzi.Game.Clock
{
    // 날짜 · 낮/밤 · 안 켠 초. 밤은 상태일 뿐 다른 시계를 안 건드린다.
    // OfflineSeconds 는 PostInit 에서 정해지므로 뒤 Manager 의 PostInit 에서 읽는다.
    public sealed class ClockManager : IManager, ISavable, IClockHub
    {
        private const string Key = "Clock";

        private ClockState _state = new ClockState();
        private ISaveHub _save;
        private double _offlineSeconds;

        public string SaveKey
        {
            get { return Key; }
        }

        public int Day
        {
            get { return _state.day; }
        }

        public bool IsNight
        {
            get { return _state.isNight; }
        }

        public double OfflineSeconds
        {
            get { return _offlineSeconds; }
        }

        public void Init()
        {
        }

        public void PostInit()
        {
            _save = InGameHub.Get<ISaveHub>();
            _save.Register(this);

            _offlineSeconds = 0;
            if (_save.HasSaveFile)
            {
                long gap = DateTime.UtcNow.Ticks - _save.LoadedAtUtcTicks;
                _offlineSeconds = Math.Max(0, gap / (double)TimeSpan.TicksPerSecond);
            }

            Debug.Log("[Clock] 날짜 " + _state.day + " · 밤 " + _state.isNight
                + " · 안 켠 초 " + _offlineSeconds.ToString("F1"));
        }

        public void Dispose()
        {
        }

        public void FallNight()
        {
            if (_state.isNight)
            {
                return;
            }

            _state.isNight = true;
            EventBus.Publish(new NightFellEvent(_state.day));
        }

        // 밤이 아니어도 넘어간다. 「다음날」은 실제 시간에 안 묶인다 (세로조각1판설계 2절).
        public void AdvanceDay()
        {
            _state.day++;
            _state.isNight = false;
            EventBus.Publish(new DayStartedEvent(_state.day));
        }

        public string CaptureJson()
        {
            return JsonUtility.ToJson(_state);
        }

        public void RestoreJson(string json)
        {
            ClockState loaded = JsonUtility.FromJson<ClockState>(json);
            if (loaded == null)
            {
                return;
            }

            _state = loaded;
            if (_state.day < 1)
            {
                _state.day = 1;
            }
        }
    }
}
