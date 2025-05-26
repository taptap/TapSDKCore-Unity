using TapSDK.Core.Internal;
using UnityEngine;
using TapSDK.Core.Standalone.Internal;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal.Openlog;
using TapSDK.Core.Internal.Log;
using TapSDK.Core.Standalone.Internal.Http;
using Newtonsoft.Json;
using TapSDK.Core.Standalone.Internal.Bean;
using System.Threading.Tasks;
using System;
using System.Threading;
using TapSDK.UI;
using System.Runtime.InteropServices;

namespace TapSDK.Core.Standalone
{
    /// <summary>
    /// Represents the standalone implementation of the TapCore SDK.
    /// </summary>
    public class TapCoreStandalone : ITapCorePlatform
    {
        internal static Prefs Prefs;
        internal static Tracker Tracker;
        internal static User User;
        internal static TapTapSdkOptions coreOptions;
        
        // client 信息是否匹配
        internal static bool isClientInfoMatched = true;
        internal static bool enableAutoEvent = true;

        internal static TapGatekeeper gatekeeperData = new TapGatekeeper();

        private readonly TapHttp tapHttp = TapHttp.NewBuilder("TapSDKCore", TapTapSDK.Version).Build();


         // 初始化校验结果
        private class TapInitResult
        {
            internal TapSDKInitResult result;
            internal string errorMsg;

            internal bool needQuitGame = false;

            public TapInitResult(TapSDKInitResult result, string errorMsg)
            {
                this.result = result;
                this.errorMsg = errorMsg;
            }

            public TapInitResult(bool needQuitGame)
            {
                this.needQuitGame = needQuitGame;
            }
        }

        // 使用客户端登录结果返回值
        public class TapLoginResponseByTapClient
        {

            public bool isCancel = false;

            public string redirectUri;

            public bool isFail = false;

            public string errorMsg;

            public TapLoginResponseByTapClient(bool isCancel, string redirctUri)
            {
                this.redirectUri = redirctUri;
                this.isCancel = isCancel;
            }

            public TapLoginResponseByTapClient(string errorMsg)
            {
                isFail = true;
                isCancel = false;
                this.errorMsg = errorMsg;
            }


        }

        // 使用客户端登录结果回调
        public interface TapLoginCallbackWithTapClient
        {
            void onSuccess(TapLoginResponseByTapClient response);

            void onFailure(string error);

            void onCancel();
        }

        // 是否是渠道服游戏包
        private static bool isChannelPackage = false;

        // -1 未执行 0 失败  1 成功
        private static int lastIsLaunchedFromTapTapPCResult = -1;
        private static bool isRuningIsLaunchedFromTapTapPC = false;


        // 当为渠道游戏包时，与启动器的初始化校验结果
        private TapInitResult tapInitResult;



        /// <summary>
        /// Initializes a new instance of the <see cref="TapCoreStandalone"/> class.
        /// </summary>
        public TapCoreStandalone()
        {
            // Instantiate modules
           
            Tracker = new Tracker();
            User = new User();
            TapLoom.Initialize();
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified options.
        /// </summary>
        /// <param name="options">The TapCore SDK options.</param>
        public void Init(TapTapSdkOptions options)
        {
            Init(options, null);
        }

        /// <summary>
        /// Initializes the TapCore SDK with the specified core options and additional options.
        /// </summary>
        /// <param name="coreOption">The TapCore SDK core options.</param>
        /// <param name="otherOptions">Additional TapCore SDK options.</param>
        public void Init(TapTapSdkOptions coreOption, TapTapSdkBaseOptions[] otherOptions)
        {
            if (coreOption.clientId == null || coreOption.clientId.Length == 0){
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("clientId 不能为空","clientId 不能为空");
                return;
            }
            if(coreOption.clientToken == null || coreOption.clientToken.Length == 0) {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("clientToken 不能为空","clientToken 不能为空");
                return;
            }
            TapLog.Log("SDK Init Options : ", "coreOption : " + JsonConvert.SerializeObject(coreOption) + "\notherOptions : " + JsonConvert.SerializeObject(otherOptions));
            coreOptions = coreOption;
            if (Prefs == null) {
                Prefs = new Prefs();
            }
            TapOpenlogStandalone.Init();

            var path = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + "_" + coreOption.clientId + ".json");
            // 兼容旧版文件
            if (!File.Exists(path)) {
                var oldPath = Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + ".json");
                if(File.Exists(oldPath)){
                    File.Move(oldPath, path);
                }
            }
            if (File.Exists(path))
            {
                var clientSettings = File.ReadAllText(path);
                // TapLog.Log("本地 clientSettings: " + clientSettings);
                try
                {
                    TapGatekeeper tapGatekeeper = JsonConvert.DeserializeObject<TapGatekeeper>(clientSettings);
                    SetAutoEvent(tapGatekeeper);
                    if (tapGatekeeper.Switch?.Heartbeat == true)
                    {
                        TapAppDurationStandalone.Enable();
                    }
                    else
                    {
                        TapAppDurationStandalone.Disable();
                    }
                    gatekeeperData = tapGatekeeper;
                }
                catch (System.Exception e)
                {
                    TapLog.Warning("TriggerEvent error: " + e.Message);
                }
            }

            Tracker.Init();

            requestClientSetting();
        }

        public void UpdateLanguage(TapTapLanguageType language)
        {
            if (coreOptions == null)
            {
                Debug.Log("coreOptions is null");
                return;
            }
            TapLog.Log("UpdateLanguage called with language: " + language);
            coreOptions.preferredLanguage = language;
        }

        public static string getGatekeeperConfigUrl(string key)
        {
            if (gatekeeperData != null)
            {
                var urlsData = gatekeeperData.Urls;
                if (urlsData != null && urlsData.ContainsKey(key))
                {
                    var keyData = urlsData[key];
                    if (keyData != null)
                    {
                        return keyData.Browser;
                    }
                }
            }
            return null;
        }

        private void requestClientSetting()
        {
            // 使用 httpclient 请求 /sdk-core/v1/gatekeeper 获取配置
#if UNITY_EDITOR
            var bundleIdentifier = PlayerSettings.applicationIdentifier;
#else
            var bundleIdentifier = Application.identifier;
#endif
            var path = "sdk-core/v1/gatekeeper";
            var body = new Dictionary<string, object> {
                { "platform", "pc" },
                { "bundle_id", bundleIdentifier }
            };

            tapHttp.PostJson<TapGatekeeper>(
               url: path,
               json: body,
               onSuccess: (data) =>
               {
                   SetAutoEvent(data);
                   if (data.Switch?.Heartbeat == true)
                   {
                       TapAppDurationStandalone.Enable();
                   }
                   else
                   {
                       TapAppDurationStandalone.Disable();
                   }
                   gatekeeperData = data;
                   // 把 data 存储在本地
                   saveClientSettings(data);
                   // 发通知
                   EventManager.TriggerEvent(Constants.ClientSettingsEventKey, data);
               },
               onFailure: (error) =>
               {
                   if (error is TapHttpServerException se)
                   {
                       if (TapHttpErrorConstants.ERROR_INVALID_CLIENT.Equals(se.ErrorData.Error))
                       {
                           isClientInfoMatched = false;
                           TapLog.Error("Init Failed", se.ErrorData.ErrorDescription);
                           TapMessage.ShowMessage(se.ErrorData.Msg, TapMessage.Position.bottom, TapMessage.Time.twoSecond);
                       }
                   }
               }
           );
        }

        private void saveClientSettings(TapGatekeeper settings)
        {
            string json = JsonConvert.SerializeObject(settings);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, Constants.ClientSettingsFileName + "_" + TapTapSDK.taptapSdkOptions.clientId + ".json"), json);
        }

        private void SetAutoEvent(TapGatekeeper gatekeeper)
        {
            if (gatekeeper != null)
            {
                var switchData = gatekeeper.Switch;
                if (switchData != null)
                {
                    enableAutoEvent = switchData.AutoEvent;
                }
            }
        }

        public static bool CheckInitState()
        {
            // 未初始化
            if (coreOptions == null || coreOptions.clientId == null || coreOptions.clientId.Length  == 0
            || coreOptions.clientToken == null || coreOptions.clientToken.Length == 0) {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("当前应用还未初始化","当前应用还未初始化: 请在调用 SDK 业务接口前，先调用 TapTapSDK.Init  接口");
                return false;
            }
            // 应用信息不匹配
            if(isClientInfoMatched == false) {
                TapVerifyInitStateUtils.ShowVerifyErrorMsg("当前应用初始化信息错误","当前应用初始化信息错误: 请在 TapTap 开发者中心检查当前应用调用初始化接口设置的 clientId 、clientToken 是否匹配");
                return false;
            }
            return true;
        }

        // 获取当前用户设置的 DB userID
        public static string GetCurrentUserId(){
            return User?.Id;
        }


            // <summary>
        // 校验游戏是否通过启动器唤起，建立与启动器通讯
        //</summary>
        public async Task<bool> IsLaunchedFromTapTapPC()
        {
#if UNITY_STANDALONE_WIN
            // 正在执行中
            if(isRuningIsLaunchedFromTapTapPC){
                UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 正在执行，请勿重复调用", UIManager.GeneralToastLevel.Error);
                TapLogger.Error("IsLaunchedFromTapTapPC 正在执行，请勿重复调用");
                return false;
            }
            // 多次执行时返回上一次结果
            if(lastIsLaunchedFromTapTapPCResult != -1){
                TapLogger.Debug("IsLaunchedFromTapTapPC duplicate invoke return " + lastIsLaunchedFromTapTapPCResult);
                return lastIsLaunchedFromTapTapPCResult > 0;
            }
            
            isChannelPackage = true;
            if (coreOptions == null)
            {
                UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 调用必须在初始化之后", UIManager.GeneralToastLevel.Error);
                TapLogger.Error("IsLaunchedFromTapTapPC 调用必须在初始化之后");
                return false;
            }
            string clientId = coreOptions.clientId;
            string pubKey = coreOptions.clientPublicKey;
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(pubKey))
            {
                UIManager.Instance.OpenToast("clientId 及 TapPubKey 参数都不能为空, clientId =" +  clientId + ", TapPubKey = " + pubKey, UIManager.GeneralToastLevel.Error);
                TapLogger.Error("clientId 或 TapPubKey 无效, clientId = " + clientId + ", TapPubKey = " + pubKey);
                return false;
            }
            isRuningIsLaunchedFromTapTapPC = true;

            string sessionId = Guid.NewGuid().ToString();
            TapCoreTracker.Instance.TrackStart(TapCoreTracker.METHOD_LAUNCHER, sessionId);
            try
            {
                TapInitResult result = await RunClientBridgeMethodWithTimeout(clientId, pubKey);
                isRuningIsLaunchedFromTapTapPC = false;
                if (result.needQuitGame)
                {
                    lastIsLaunchedFromTapTapPCResult = 0;
                    TapCoreTracker.Instance.TrackSuccess(TapCoreTracker.METHOD_LAUNCHER, sessionId, TapCoreTracker.SUCCESS_TYPE_RESTART);
                    TapLogger.Debug("IsLaunchedFromTapTapPC Quit game");
                    Application.Quit();
                    return false;
                }
                else
                {
                    if (result.result == TapSDKInitResult.kTapSDKInitResult_OK)
                    {
                        string currentClientId;
                        bool isFetchClientIdSuccess = TapClientBridge.GetClientId(out currentClientId);
                        TapLogger.Debug("IsLaunchedFromTapTapPC get  clientId = " + currentClientId);
                        if (isFetchClientIdSuccess && !string.IsNullOrEmpty(currentClientId) && currentClientId != clientId ){
                             UIManager.Instance.OpenToast("SDK 中配置的 clientId = " + clientId + "与 Tap 启动器中" + currentClientId + "不一致", UIManager.GeneralToastLevel.Error);
                             TapLogger.Error("SDK 中配置的 clientId = " + clientId + "与 Tap 启动器中" + currentClientId + "不一致");
                             TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, -1, "SDK 中配置的 clientId = " + clientId + "与 Tap 启动器中" + currentClientId + "不一致");
                             lastIsLaunchedFromTapTapPCResult = 0;
                            return false;
                        }
                        string openId ;
                        bool fetchOpenIdSuccess = TapClientBridge.GetTapUserOpenId(out openId);
                        if (fetchOpenIdSuccess){
                            TapLogger.Debug("IsLaunchedFromTapTapPC get  openId = " + openId);
                            EventManager.TriggerEvent(EventManager.IsLaunchedFromTapTapPCFinished, openId);
                        }else{
                            TapLogger.Debug("IsLaunchedFromTapTapPC get  openId failed" );
                        }
                        lastIsLaunchedFromTapTapPCResult = 1;
                        TapClientBridgePoll.StartUp();
                        TapCoreTracker.Instance.TrackSuccess(TapCoreTracker.METHOD_LAUNCHER, sessionId, TapCoreTracker.SUCCESS_TYPE_INIT);
                        TapLogger.Debug("IsLaunchedFromTapTapPC check success");
                        return true;
                    }
                    else
                    {
                       
                        TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, (int)result.result, result.errorMsg ?? "");
                        lastIsLaunchedFromTapTapPCResult = 0;
                        TapLogger.Debug("IsLaunchedFromTapTapPC show TapClient tip Pannel " + result.result + " , error = " + result.errorMsg);
                        string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                        if (Resources.Load<GameObject>(tipPannelPath) != null)
                        {
                            var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(tipPannelPath);
                            pannel.Show(result.result);
                        }
                        return false;
                    }
                }
            }
            catch(TimeoutException e)
            {
                 lastIsLaunchedFromTapTapPCResult = 0;
                TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, (int)TapSDKInitResult.kTapSDKInitResult_Timeout, e.Message ?? "");

                TapLogger.Debug("IsLaunchedFromTapTapPC check timeout");
                string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                if (Resources.Load<GameObject>(tipPannelPath) != null)
                {
                    var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(tipPannelPath);
                    pannel.Show(TapSDKInitResult.kTapSDKInitResult_Timeout);
                }
                return false;
            }
            catch (Exception e)
            {
                lastIsLaunchedFromTapTapPCResult = 0;
                TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, (int)TapSDKInitResult.kTapSDKInitResult_Unknown, e.Message ?? "");

                TapLogger.Debug("IsLaunchedFromTapTapPC check exception = " + e.StackTrace);
                string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                if (Resources.Load<GameObject>(tipPannelPath) != null)
                {
                    var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(tipPannelPath);
                    pannel.Show(TapSDKInitResult.kTapSDKInitResult_Unknown);
                }
                return false;
            }

#else
            UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 仅支持 Windows PC 端", UIManager.GeneralToastLevel.Error);
            TapLogger.Error("IsLaunchedFromTapTapPC 仅支持 Windows PC 端");
            return false;
#endif
        }

        private async Task<TapInitResult> RunClientBridgeMethodWithTimeout(string clientId, string pubKey)
        {
#if UNITY_STANDALONE_WIN            
            TaskCompletionSource<TapInitResult> task = new TaskCompletionSource<TapInitResult>();
            try
            {
                TapInitResult result = await ExecuteWithTimeoutAsync(() =>
                {
                    bool needQuitGame = TapClientBridge.TapSDK_RestartAppIfNecessary(clientId);
                    TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke  TapSDK_RestartAppIfNecessary result = " + needQuitGame);
                    if (needQuitGame)
                    {
                        tapInitResult = new TapInitResult(needQuitGame);
                    }
                    else
                    {
                        string outputError;
                        TapSDKInitResult tapSDKInitResult = TapClientBridge.CheckInitState(out outputError, pubKey);
                        TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke  CheckInitState result = " + tapSDKInitResult + ", error = " + outputError);
                        tapInitResult = new TapInitResult(tapSDKInitResult, outputError);
                    }
                    return tapInitResult;
                }, TimeSpan.FromSeconds(5));
                    task.TrySetResult(tapInitResult);
                
            }
            catch (TimeoutException ex)
            {
                   TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke  CheckInitState 方法执行超时！");
                   task.TrySetException(ex);
            }
            catch (Exception ex)
            {
                 TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke C 方法出错！" + ex.StackTrace);
                 task.TrySetException(ex);
            }
            return await task.Task;
#else
            throw new Exception("当前平台不支持该操作，仅支持 Windows PC");
#endif                        
        }

        /// <summary>
        /// 在 IO 线程执行 C 方法，超时 5 秒后切回主线程
        /// </summary>
        private static async Task<T> ExecuteWithTimeoutAsync<T>(Func<T> cMethod, TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource())
            {
                Task<T> ioTask = Task.Run(cMethod); // 在后台线程执行 C 方法
                Task delayTask = Task.Delay(timeout); // 超时任务

                Task completedTask = await Task.WhenAny(ioTask, delayTask);

                if (completedTask == delayTask)
                {
                    cts.Cancel(); // 取消 C 方法任务
                    throw new TimeoutException("C 方法执行超时！");
                }
                else
                {
                    cts.Cancel();
                    return await ioTask;
                }
            }
        }

        /// <summary>
        /// 是否需要从启动器登录
        /// </summary>
        public static bool IsNeedLoginByTapClient()
        {
            return isChannelPackage;
        }


#if UNITY_STANDALONE_WIN  
        private static TaskCompletionSource<TapLoginResponseByTapClient> taskCompletionSource;
        private static TapClientBridge.CallbackDelegate currentLoginDelegate;
#endif

        /// <summary>
        /// 发起登录授权
        /// </summary>
        public static async Task<TapLoginResponseByTapClient> LoginWithScopesAsync(string[] scopes, string responseType, string redirectUri,
    string codeChallenge, string state, string codeChallengeMethod, string versonCode, string sdkUa, string info)
        {
#if UNITY_STANDALONE_WIN 
            if(lastIsLaunchedFromTapTapPCResult == -1){
                // UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 正在执行，请在完成后调用授权接口", UIManager.GeneralToastLevel.Error);
                TapLogger.Error("IsLaunchedFromTapTapPC 正在执行，请在完成后调用授权接口");
                throw new Exception("操作异常: IsLaunchedFromTapTapPC 正在执行，请在完成后调用授权接口");
            }
            TapLogger.Debug("LoginWithScopes start login by tapclient mainthread = " + Thread.CurrentThread.ManagedThreadId);

            taskCompletionSource = new TaskCompletionSource<TapLoginResponseByTapClient>();
            
             currentLoginDelegate = loginCallbackDelegate;
            try
            {
                
                // if(currentLoginDelegate == null){
                //     currentLoginDelegate = loginCallbackDelegate;
                    TapLogger.Debug("LoginWithScopes setDelegate ");
                    TapClientBridge.RegisterCallback((int)TapCallbackID.kTapCallbackIDAuthorizeFinished, currentLoginDelegate);
                // }
                TapLogger.Debug("LoginWithScopes try get login result ");
                AuthorizeResult authorizeResult =  TapClientBridge.LoginWithScopes(scopes, responseType,  redirectUri, 
     codeChallenge,  state,  codeChallengeMethod,  versonCode,  sdkUa,  info);
                   
                TapLogger.Debug("LoginWithScopes in mainthread = " + authorizeResult);
                if (authorizeResult != AuthorizeResult.kAuthorizeResult_OK)
                {
                    TapClientBridge.UnRegisterCallback(currentLoginDelegate, true);
                    taskCompletionSource.TrySetResult(new TapLoginResponseByTapClient("发起授权失败，请确认 Tap 客户端是否正常运行"));
                }
                
            }
            catch (Exception ex)
            {
                TapLogger.Debug("LoginWithScopes start login by tapclient error = " + ex.StackTrace);
                TapClientBridge.UnRegisterCallback(currentLoginDelegate, true);
                taskCompletionSource.TrySetResult(new TapLoginResponseByTapClient(false, ex.StackTrace));
            }
            return await taskCompletionSource.Task;
#else
            throw new Exception("当前平台不支持该授权操作，仅支持 Windows PC ");
#endif          
        }

#if UNITY_STANDALONE_WIN
        [AOT.MonoPInvokeCallback(typeof(TapClientBridge.CallbackDelegate))]
        static void loginCallbackDelegate(int id, IntPtr userData)
        {

            TapLogger.Debug("LoginWithScopes recevie callback " + id);
            if (id == (int)TapCallbackID.kTapCallbackIDAuthorizeFinished)
            {
                TapLogger.Debug("LoginWithScopes callback mainthread = " + Thread.CurrentThread.ManagedThreadId);
                    TapClientBridge.AuthorizeFinishedResponse response = Marshal.PtrToStructure<TapClientBridge.AuthorizeFinishedResponse>(userData);
                    TapLogger.Debug("LoginWithScopes callback = " + response.is_cancel + " uri = " + response.callback_uri);
                    if (taskCompletionSource != null) {
                        taskCompletionSource.TrySetResult(new TapLoginResponseByTapClient(response.is_cancel > 0, response.callback_uri));
                        taskCompletionSource = null;
                    }
                        TapLogger.Debug("LoginWithScopes callback finish and will remove delegate " );
                        if (currentLoginDelegate != null){
                        TapLogger.Debug("LoginWithScopes callback finish and will remove delegate and not null" );
                        TapClientBridge.UnRegisterCallback(currentLoginDelegate, true);
                        currentLoginDelegate = null;
                        }

                TapLogger.Debug("LoginWithScopes callback finish and will remove delegate finish" );

            }
        }
#endif

        
    }

    public interface IOpenIDProvider
    {
        string GetOpenID();
    }
}
