using System;
using System.Threading.Tasks;
using System.Linq;
using TapSDK.Core.Internal;
using System.Collections.Generic;

using UnityEngine;
using System.Reflection;
using TapSDK.Core.Internal.Init;
using TapSDK.Core.Internal.Log;

namespace TapSDK.Core {
    public class TapTapSDK {
        public static readonly string Version = "4.3.10";
        
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
            if (string.IsNullOrEmpty(coreOption.clientId))
                throw new ArgumentException("[TapSDK] clientID is null or empty!");
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
            if (string.IsNullOrEmpty(coreOption.clientId))
                throw new ArgumentException("[TapSDK] clientID is null or empty!");

            TapTapSDK.taptapSdkOptions = coreOption;
            TapLog.Enabled = coreOption.enableLog;
            platformWrapper?.Init(coreOption,otherOptions);

            Type[] initTaskTypes = GetInitTypeList();
            if (initTaskTypes != null) {
                List<IInitTask> initTasks = new List<IInitTask>();
                foreach (Type initTaskType in initTaskTypes) {
                    initTasks.Add(Activator.CreateInstance(initTaskType) as IInitTask);
                }
                initTasks = initTasks.OrderBy(task => task.Order).ToList();
                foreach (IInitTask task in initTasks) {
                    TapLogger.Debug($"Init: {task.GetType().Name}");
                    task.Init(coreOption,otherOptions);
                }
            }
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
