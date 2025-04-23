using System;
using System.Threading.Tasks;
using System.Linq;
using TapSDK.Core.Internal;
using System.Collections.Generic;

using UnityEngine;
using System.Reflection;
using TapSDK.Core.Internal.Init;
using TapSDK.Core.Internal.Log;
using System.ComponentModel;

namespace TapSDK.Core {
    public class TapTapSDK {
        public static readonly string Version = "4.6.1-beta.6";
        
        public static string SDKPlatform = "TapSDK-Unity";

        public static TapTapSdkOptions taptapSdkOptions;
        private static ITapCorePlatform platformWrapper;

        private static bool disableDurationStatistics;

        public static bool DisableDurationStatistics {
            get => disableDurationStatistics;
            set {
                disableDurationStatistics = value;
            }
        }

        static TapTapSDK() {
            platformWrapper = PlatformTypeUtils.CreatePlatformImplementationObject(typeof(ITapCorePlatform),
                "TapSDK.Core") as ITapCorePlatform;
        }

        public static void Init(TapTapSdkOptions coreOption) {
            if (coreOption == null)
                throw new ArgumentException("[TapSDK] options is null!");
            TapTapSDK.taptapSdkOptions = coreOption;
            TapLog.Enabled = coreOption.enableLog;
            platformWrapper?.Init(coreOption);
             // 初始化各个模块
            
            Type[] initTaskTypes = GetInitTypeList();
            if (initTaskTypes != null) {
                List<IInitTask> initTasks = new List<IInitTask>();
                foreach (Type initTaskType in initTaskTypes) {
                    initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                }
                initTasks = initTasks.OrderBy(task => task.Order).ToList();
                foreach (IInitTask task in initTasks) {
                    TapLogger.Debug($"Init: {task.GetType().Name}");
                    task.Init(coreOption);
                }
            }
        }

        public static void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions){
            if (coreOption == null)
                throw new ArgumentException("[TapSDK] options is null!");
            long startTime = DateTime.Now.Ticks;
            TapTapSDK.taptapSdkOptions = coreOption;
            TapLog.Enabled = coreOption.enableLog;
            long startCore = DateTime.Now.Ticks;
            platformWrapper?.Init(coreOption,otherOptions);
            long costCore = DateTime.Now.Ticks - startCore;
            // TapLog.Log($"Init core cost time: {costCore / 10000}ms");

            Type[] initTaskTypes = GetInitTypeList();
            if (initTaskTypes != null) {
                List<IInitTask> initTasks = new List<IInitTask>();
                foreach (Type initTaskType in initTaskTypes) {
                    initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                }
                initTasks = initTasks.OrderBy(task => task.Order).ToList();
                foreach (IInitTask task in initTasks) {
                    TapLog.Log($"Init: {task.GetType().Name}");
                    long startModule = DateTime.Now.Ticks;
                    task.Init(coreOption,otherOptions);
                    long costModule = DateTime.Now.Ticks - startModule;
                    // TapLog.Log($"Init {task.GetType().Name} cost time: {costModule / 10000}ms");
                }
            }
            long costTime = DateTime.Now.Ticks - startTime;
            // TapLog.Log($"Init cost time: {costTime / 10000}ms");
        }

        // UpdateLanguage 方法
        public static void UpdateLanguage(TapTapLanguageType language){
            platformWrapper?.UpdateLanguage(language);
        }

        private static Type[] GetInitTypeList(){
            Type interfaceType = typeof(IInitTask);
            Type[] initTaskTypes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(asssembly => asssembly.GetName().FullName.StartsWith("TapSDK"))
                .SelectMany(assembly => assembly.GetTypes())
                .Where(clazz => interfaceType.IsAssignableFrom(clazz) && clazz.IsClass)
                .ToArray();
            return initTaskTypes;
        }

    }
}
