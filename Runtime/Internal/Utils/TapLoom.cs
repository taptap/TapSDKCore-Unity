using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

namespace TapSDK.Core.Internal.Utils
{
    public class TapLoom : MonoBehaviour
    {
        public static int maxThreads = 8;
        static int numThreads;

        private static TapLoom _current;
        private int _count;

         private bool isPause = false;
        public static TapLoom Current
        {
            get
            {
                Initialize();
                return _current;
            }
        }

        void Awake()
        {
            _current = this;
            initialized = true;
        }

        static bool initialized;

        public static void Initialize()
        {
            if (!initialized)
            {

                if (!Application.isPlaying)
                    return;
                initialized = true;
                var g = new GameObject("TapLoom");
                DontDestroyOnLoad(g);
                _current = g.AddComponent<TapLoom>();
            }

        }

        private List<Action> _actions = new List<Action>();
        public struct DelayedQueueItem
        {
            public float time;
            public Action action;
        }
        private List<DelayedQueueItem> _delayed = new List<DelayedQueueItem>();

        List<DelayedQueueItem> _currentDelayed = new List<DelayedQueueItem>();

        public static void QueueOnMainThread(Action action)
        {
            QueueOnMainThread(action, 0f);
        }
        public static void QueueOnMainThread(Action action, float time)
        {
            if (time != 0)
            {
                lock (Current._delayed)
                {
                    Current._delayed.Add(new DelayedQueueItem { time = Time.time, action = action });
                }
            }
            else
            {
                lock (Current._actions)
                {
                    Current._actions.Add(action);
                }
            }
        }

        public static Thread RunAsync(Action a)
        {
            Initialize();
            while (numThreads >= maxThreads)
            {
                Thread.Sleep(1);
            }
            Interlocked.Increment(ref numThreads);
            ThreadPool.QueueUserWorkItem(RunAction, a);
            return null;
        }

        private static void RunAction(object action)
        {
            try
            {
                ((Action)action)();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Decrement(ref numThreads);
            }

        }


        void OnDisable()
        {
            if (_current == this)
            {

                _current = null;
            }
        }



        // Use this for initialization  
        void Start()
        {

        }

        List<Action> _currentActions = new List<Action>();

        // Update is called once per frame  
        void Update()
        {
            lock (_actions)
            {
                _currentActions.Clear();
                _currentActions.AddRange(_actions);
                _actions.Clear();
            }
            foreach (var a in _currentActions)
            {
                a();
            }
            lock (_delayed)
            {
                _currentDelayed.Clear();
                _currentDelayed.AddRange(_delayed.Where(d => d.time <= Time.time));
                foreach (var item in _currentDelayed)
                    _delayed.Remove(item);
            }
            foreach (var delayed in _currentDelayed)
            {
                delayed.action();
            }
        }

        private void OnApplicationPause(bool pauseStatus) {
             if (pauseStatus && isPause == false) {
                isPause = true;
                EventManager.TriggerEvent(EventManager.OnApplicationPause, true);
            }
            else if (!pauseStatus && isPause) {
                isPause = false;
                EventManager.TriggerEvent(EventManager.OnApplicationPause, false);
            }
        }

         private void OnApplicationQuit(){
            EventManager.TriggerEvent(EventManager.OnApplicationQuit, true);
        }
    }
}