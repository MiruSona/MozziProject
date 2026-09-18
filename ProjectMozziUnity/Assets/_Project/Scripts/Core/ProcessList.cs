using System;
using System.Collections.Generic;

namespace Mozzi.Core
{
    // 추가는 순회가 끝난 뒤 합류시키고, 삭제는 즉시 dead 표시만 해 순회가 건너뛴다.
    // 인터페이스 검사는 추가할 때 한 번만 하고 세 목록에 나눠 담는다.
    public sealed class ProcessList<T> where T : class
    {
        private readonly List<IProcess> _process = new List<IProcess>();
        private readonly List<IFixedProcess> _fixedProcess = new List<IFixedProcess>();
        private readonly List<ILateProcess> _lateProcess = new List<ILateProcess>();

        private readonly List<T> _pendingAdd = new List<T>();
        private readonly List<T> _pendingRemove = new List<T>();
        private readonly HashSet<object> _dead = new HashSet<object>();

        private int _depth;

        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_depth > 0)
            {
                _pendingAdd.Add(item);
                return;
            }

            AddNow(item);
        }

        public void Remove(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            // 표시가 먼저다. 이번 순회의 남은 항목부터 건너뛴다.
            _dead.Add(item);

            if (_depth > 0)
            {
                _pendingRemove.Add(item);
                return;
            }

            RemoveNow(item);
        }

        public void ProcessAll(float deltaTime)
        {
            _depth++;
            try
            {
                for (int i = 0; i < _process.Count; i++)
                {
                    IProcess item = _process[i];
                    if (_dead.Contains(item)) { continue; }

                    item.Process(deltaTime);
                }
            }
            finally
            {
                _depth--;
            }

            Flush();
        }

        public void FixedProcessAll(float deltaTime)
        {
            _depth++;
            try
            {
                for (int i = 0; i < _fixedProcess.Count; i++)
                {
                    IFixedProcess item = _fixedProcess[i];
                    if (_dead.Contains(item)) { continue; }

                    item.FixedProcess(deltaTime);
                }
            }
            finally
            {
                _depth--;
            }

            Flush();
        }

        public void LateProcessAll(float deltaTime)
        {
            _depth++;
            try
            {
                for (int i = 0; i < _lateProcess.Count; i++)
                {
                    ILateProcess item = _lateProcess[i];
                    if (_dead.Contains(item)) { continue; }

                    item.LateProcess(deltaTime);
                }
            }
            finally
            {
                _depth--;
            }

            Flush();
        }

        private void AddNow(T item)
        {
            _dead.Remove(item);

            IProcess process = item as IProcess;
            if (process != null) { _process.Add(process); }

            IFixedProcess fixedProcess = item as IFixedProcess;
            if (fixedProcess != null) { _fixedProcess.Add(fixedProcess); }

            ILateProcess lateProcess = item as ILateProcess;
            if (lateProcess != null) { _lateProcess.Add(lateProcess); }
        }

        private void RemoveNow(T item)
        {
            IProcess process = item as IProcess;
            if (process != null) { _process.Remove(process); }

            IFixedProcess fixedProcess = item as IFixedProcess;
            if (fixedProcess != null) { _fixedProcess.Remove(fixedProcess); }

            ILateProcess lateProcess = item as ILateProcess;
            if (lateProcess != null) { _lateProcess.Remove(lateProcess); }

            _dead.Remove(item);
        }

        // 순회 중에 추가되고 같은 순회에서 삭제된 항목이 있어 추가를 먼저 처리한다.
        private void Flush()
        {
            if (_depth > 0) { return; }

            for (int i = 0; i < _pendingAdd.Count; i++)
            {
                AddNow(_pendingAdd[i]);
            }
            _pendingAdd.Clear();

            for (int i = 0; i < _pendingRemove.Count; i++)
            {
                RemoveNow(_pendingRemove[i]);
            }
            _pendingRemove.Clear();
        }
    }
}
