using System;
using System.IO;
using Mozzi.Save;
using NUnit.Framework;
using UnityEngine;

namespace Mozzi.Tests
{
    // 이번 실행에 등록 안 된 매니저의 칸이 다음 저장 때 사라지지 않는지 본다.
    // 진짜 세이브를 안 덮게 임시 폴더의 파일로 읽고 쓴다.
    public sealed class SaveKeepTests
    {
        private const string FarmJson = "{\"day\":7}";

        private string _path;

        [SetUp]
        public void SetUp()
        {
            _path = Path.Combine(Path.GetTempPath(), "mozzi-save-test-" + Guid.NewGuid().ToString("N") + ".json");
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(_path);
            File.Delete(_path + ".tmp");
        }

        [Test]
        public void SaveNow_KeepsEntryOfUnregisteredManager()
        {
            SaveFile before = new SaveFile();
            before.version = SaveFile.CurrentVersion;
            before.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            before.entries.Add(Entry("Clock", "{\"day\":1}"));
            before.entries.Add(Entry("Farm", FarmJson));
            File.WriteAllText(_path, JsonUtility.ToJson(before));

            SaveManager save = new SaveManager();
            save.LoadFrom(_path);
            save.Register(new FakeSavable("Clock", "{\"day\":2}"));
            save.SaveNow();

            SaveFile after = JsonUtility.FromJson<SaveFile>(File.ReadAllText(_path));
            SaveEntry farm = after.entries.Find(e => e.key == "Farm");
            SaveEntry clock = after.entries.Find(e => e.key == "Clock");
            Assert.That(farm, Is.Not.Null);
            Assert.That(farm.json, Is.EqualTo(FarmJson));
            Assert.That(clock.json, Is.EqualTo("{\"day\":2}"));
        }

        private static SaveEntry Entry(string key, string json)
        {
            SaveEntry entry = new SaveEntry();
            entry.key = key;
            entry.json = json;
            return entry;
        }
    }
}
