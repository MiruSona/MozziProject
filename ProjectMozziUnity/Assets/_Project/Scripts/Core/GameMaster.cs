using System;
using System.Collections.Generic;
using Mozzi.Events;
using Mozzi.Hubs;
using Mozzi.Resource;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mozzi.Core
{
    // 앱의 부팅 자리. Service 를 만들어 GlobalHub 에 등록하고 GameRunner 를 세운다.
    public static class GameMaster
    {
        private static readonly List<IService> _services = new List<IService>();
        private static readonly object _serviceOwner = new object();

        private static GameRunner _runner;
        private static string _pendingScene;

        public static float TimeScale { get; set; } = 1f;

        internal static GameRunner Runner
        {
            get { return _runner; }
        }

        public static bool IsBooted
        {
            get { return _runner != null; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            ResetStatics();
            _runner = CreateRunner();
            RegisterServices();
        }

        // 씬 요청은 받아만 두고 프레임 끝에 한 번 실행한다.
        public static void RequestScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new ArgumentException("씬 이름이 비었다.", nameof(sceneName));
            }

            _pendingScene = sceneName;
        }

        // GameRunner 가 LateUpdate 끝에 한 번 부른다.
        internal static void FlushSceneRequest()
        {
            if (string.IsNullOrEmpty(_pendingScene)) { return; }

            string next = _pendingScene;
            _pendingScene = null;
            SceneManager.LoadScene(next);
        }

        // Domain Reload 를 끈 에디터에서는 static 이 살아남으므로 직접 비운다.
        private static void ResetStatics()
        {
            TimeScale = 1f;
            _pendingScene = null;
            _runner = null;
            _services.Clear();
            GlobalHub.Clear();
            EventBus.Clear();
        }

        private static GameRunner CreateRunner()
        {
            GameObject host = new GameObject("GameRunner");
            UnityEngine.Object.DontDestroyOnLoad(host);
            return host.AddComponent<GameRunner>();
        }

        // 등록 순서가 곧 실행 순서다. Service 를 더할 때 이 순서를 보고 넣는다.
        private static void RegisterServices()
        {
            ResourceService resource = new ResourceService(new ResourcesPrefabSource());
            _services.Add(resource);

            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].Init();
            }

            // 꺼내 쓰는 타입으로 등록한다.
            GlobalHub.Register<IResourceHub>(resource, _serviceOwner);

            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].PostInit();
            }

            for (int i = 0; i < _services.Count; i++)
            {
                _runner.AddService(_services[i]);
            }
        }
    }
}
