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
    // 옛 판 파일은 읽을 때 SaveMigrator 로 올리고, 못 읽는 파일은 .bak 로 남긴 뒤 새 게임으로 간다.
    public sealed class SaveManager : IManager, IProcess, ISaveHub
    {
        private const string FileName = "save.json";
        // 판을 올리기 직전의 원본 한 장.
        private const string MigrationBackupSuffix = ".bak";
        // 깨졌거나 이 앱보다 새 판이라 못 읽은 원본 한 장.
        private const string UnreadableBackupSuffix = ".unreadable.bak";
        private const float AutoSaveIntervalSec = 30f;

        private readonly List<ISavable> _savables = new List<ISavable>();
        private readonly Dictionary<string, string> _loadedJson = new Dictionary<string, string>();

        private string _filePath;
        private SaveSettings _settings = new SaveSettings();
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

        public SaveSettings Settings
        {
            get { return _settings; }
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
                    WebGlFsUtil.Sync();
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
            file.version = SaveFile.CurrentVersion;
            file.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            file.settings = _settings;

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
                WebGlFsUtil.Sync();
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

        // 파일이 없으면 새 게임. 옛 판은 원본을 .bak 로 남기고 올려서 읽는다.
        // 깨진 파일 · 이 앱보다 새 판 · 올리다 실패한 파일은 원본을 남기고 새 게임으로 간다.
        private void LoadFile()
        {
            _loadedAtUtcTicks = 0;
            _loadedJson.Clear();
            _settings = new SaveSettings();

            if (File.Exists(_filePath) == false)
            {
                Debug.Log("[Save] 세이브 없음 · 새 게임 · " + _filePath);
                return;
            }

            SaveFile file = null;
            try
            {
                file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(_filePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] 읽기 실패 : " + e.Message);
            }

            if (file == null || file.entries == null)
            {
                BackupFile(UnreadableBackupSuffix);
                Debug.LogWarning("[Save] 파일 꼴이 달라 새 게임으로 간다");
                return;
            }

            if (file.version > SaveFile.CurrentVersion)
            {
                BackupFile(UnreadableBackupSuffix);
                Debug.LogWarning("[Save] 파일이 " + file.version + "판인데 이 앱은 " + SaveFile.CurrentVersion + "판까지 읽는다 · 새 게임으로 간다");
                return;
            }

            if (file.version < SaveFile.CurrentVersion)
            {
                int fromVersion = file.version;
                BackupFile(MigrationBackupSuffix);

                string error;
                if (SaveMigrator.TryUpgrade(file, SaveMigrator.CreateSteps(), SaveFile.CurrentVersion, out error) == false)
                {
                    Debug.LogWarning("[Save] 판 올리기 실패라 새 게임으로 간다 : " + error);
                    return;
                }

                Debug.Log("[Save] 판 올림 · " + fromVersion + " → " + file.version);
            }

            _loadedAtUtcTicks = file.savedAtUtcTicks;
            if (file.settings != null)
            {
                _settings = file.settings;
            }

            for (int i = 0; i < file.entries.Count; i++)
            {
                _loadedJson[file.entries[i].key] = file.entries[i].json;
            }

            Debug.Log("[Save] 불러옴 · " + file.version + "판 · 칸 " + file.entries.Count + " · " + _filePath);
        }

        // 원본을 save.json + suffix 로 베껴 둔다. 같은 이름이 있으면 덮는다 (종류마다 한 장만 남긴다).
        private void BackupFile(string suffix)
        {
            string backupPath = _filePath + suffix;
            try
            {
                File.Copy(_filePath, backupPath, true);
                WebGlFsUtil.Sync();
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] 백업 실패 " + backupPath + " : " + e.Message);
                return;
            }

            Debug.Log("[Save] 원본 백업 · " + backupPath);
        }
    }
}
