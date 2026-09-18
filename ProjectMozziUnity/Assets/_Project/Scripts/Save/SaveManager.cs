using System;
using System.Collections.Generic;
using System.IO;
using Mozzi.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mozzi.Save
{
    // save.json 한 파일을 읽고 쓴다. 30초마다 · 앱 종료 · Dispose 때 저장한다.
    // ISavable 은 Register 때 파일에 칸이 있으면 바로 RestoreJson 을 받는다 (Manager 가 PostInit 에서 등록한다).
    public sealed class SaveManager : IManager, IProcess, ISaveHub
    {
        private const string FileName = "save.json";
        private const float AutoSaveIntervalSec = 30f;

        private readonly List<ISavable> _savables = new List<ISavable>();
        private readonly Dictionary<string, string> _loadedJson = new Dictionary<string, string>();

        private string _filePath;
        private float _autoSaveClock;
        private long _loadedAtUtcTicks;

        // 에디터 정지·앱 종료 때 Application.quitting 과 Dispose 가 잇달아 오므로 한 번만 쓴다.
        private bool _savedOnQuit;

        // DeleteAndRestart 뒤에는 어떤 길로도 다시 쓰지 않는다.
        private bool _deleted;

        public long LoadedAtUtcTicks
        {
            get { return _loadedAtUtcTicks; }
        }

        public bool HasSaveFile
        {
            get { return _loadedAtUtcTicks != 0; }
        }

        public void Init()
        {
            _filePath = Path.Combine(Application.persistentDataPath, FileName);
            LoadFile();
            Application.quitting += SaveOnQuit;
        }

        public void PostInit()
        {
        }

        // 자동저장 시계는 배속(GameMaster.TimeScale)과 무관하게 실제 초로 잰다 — 배속 0 이어도 저장된다.
        public void Process(float deltaTime)
        {
            _autoSaveClock += Time.unscaledDeltaTime;
            if (_autoSaveClock < AutoSaveIntervalSec)
            {
                return;
            }

            _autoSaveClock = 0f;
            SaveNow();
        }

        public void Dispose()
        {
            Application.quitting -= SaveOnQuit;
            if (_savedOnQuit == false && _deleted == false)
            {
                SaveNow();
            }

            _savables.Clear();
            _loadedJson.Clear();
        }

        public void Register(ISavable savable)
        {
            if (savable == null)
            {
                throw new ArgumentNullException(nameof(savable));
            }

            if (_savables.Contains(savable))
            {
                throw new InvalidOperationException("ISavable '" + savable.SaveKey + "' 가 이미 등록돼 있다");
            }

            _savables.Add(savable);

            string json;
            if (_loadedJson.TryGetValue(savable.SaveKey, out json))
            {
                savable.RestoreJson(json);
            }
        }

        public void DeleteAndRestart()
        {
            _deleted = true;
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] 지우기 실패 " + _filePath + " : " + e.Message);
            }

            Debug.Log("[Save] 세이브 지움 · 씬 다시 열기");
            GameMaster.RequestScene(SceneManager.GetActiveScene().name);
        }

        public void SaveNow()
        {
            if (_deleted)
            {
                return;
            }

            SaveFile file = new SaveFile();
            file.savedAtUtcTicks = DateTime.UtcNow.Ticks;

            for (int i = 0; i < _savables.Count; i++)
            {
                SaveEntry entry = new SaveEntry();
                entry.key = _savables[i].SaveKey;
                entry.json = _savables[i].CaptureJson();
                file.entries.Add(entry);
            }

            try
            {
                File.WriteAllText(_filePath, JsonUtility.ToJson(file, true));
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] 쓰기 실패 " + _filePath + " : " + e.Message);
                return;
            }

            Debug.Log("[Save] 저장 · 칸 " + file.entries.Count + " · " + _filePath);
        }

        private void SaveOnQuit()
        {
            SaveNow();
            _savedOnQuit = true;
        }

        // 파일이 없으면 새 게임. 깨진 파일은 경고만 내고 새 게임으로 간다.
        private void LoadFile()
        {
            _loadedAtUtcTicks = 0;
            _loadedJson.Clear();

            if (File.Exists(_filePath) == false)
            {
                Debug.Log("[Save] 세이브 없음 · 새 게임 · " + _filePath);
                return;
            }

            SaveFile file;
            try
            {
                file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(_filePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] 읽기 실패라 새 게임으로 간다 : " + e.Message);
                return;
            }

            if (file == null || file.entries == null)
            {
                Debug.LogWarning("[Save] 파일 꼴이 달라 새 게임으로 간다");
                return;
            }

            _loadedAtUtcTicks = file.savedAtUtcTicks;
            for (int i = 0; i < file.entries.Count; i++)
            {
                _loadedJson[file.entries[i].key] = file.entries[i].json;
            }

            Debug.Log("[Save] 불러옴 · 칸 " + file.entries.Count + " · " + _filePath);
        }
    }
}
