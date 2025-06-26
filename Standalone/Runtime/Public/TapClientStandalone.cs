using UnityEngine;
using System.Threading.Tasks;
using TapSDK.Core.Internal.Utils;
using TapSDK.Core.Standalone.Internal;
using TapSDK.UI;
using System;
using System.Runtime.InteropServices;
using TapSDK.Core.Standalone.Internal.Openlog;
using System.Threading;



namespace TapSDK.Core.Standalone
{
#if UNITY_STANDALONE_WIN
    public class TapClientStandalone
    {

        // 是否是渠道服游戏包
        private static bool isChannelPackage = false;

        // -1 未执行 0 失败  1 成功
        private static int lastIsLaunchedFromTapTapPCResult = -1;
        private static bool isRuningIsLaunchedFromTapTapPC = false;


        // 当为渠道游戏包时，与启动器的初始化校验结果
        private static TapInitResult tapInitResult;

        // <summary>
        // 校验游戏是否通过启动器唤起，建立与启动器通讯
        //</summary>
        public static async Task<bool> IsLaunchedFromTapTapPC()
        {
            // 正在执行中
            if (isRuningIsLaunchedFromTapTapPC)
            {
                UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 正在执行，请勿重复调用", UIManager.GeneralToastLevel.Error);
                TapLogger.Error("IsLaunchedFromTapTapPC 正在执行，请勿重复调用");
                return false;
            }
            // 多次执行时返回上一次结果
            if (lastIsLaunchedFromTapTapPCResult != -1)
            {
                TapLogger.Debug("IsLaunchedFromTapTapPC duplicate invoke return " + lastIsLaunchedFromTapTapPCResult);
                return lastIsLaunchedFromTapTapPCResult > 0;
            }

            isChannelPackage = true;
            TapTapSdkOptions coreOptions = TapCoreStandalone.coreOptions;
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
                UIManager.Instance.OpenToast("clientId 及 TapPubKey 参数都不能为空, clientId =" + clientId + ", TapPubKey = " + pubKey, UIManager.GeneralToastLevel.Error);
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
                    if (result.result == TapSDKInitResult.OK)
                    {
                        string currentClientId;
                        bool isFetchClientIdSuccess = TapClientBridge.GetClientId(out currentClientId);
                        TapLogger.Debug("IsLaunchedFromTapTapPC get  clientId = " + currentClientId);
                        if (isFetchClientIdSuccess && !string.IsNullOrEmpty(currentClientId) && currentClientId != clientId)
                        {
                            UIManager.Instance.OpenToast("SDK 中配置的 clientId = " + clientId + "与 Tap 启动器中" + currentClientId + "不一致", UIManager.GeneralToastLevel.Error);
                            TapLogger.Error("SDK 中配置的 clientId = " + clientId + "与 Tap 启动器中" + currentClientId + "不一致");
                            TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, -1, "SDK 中配置的 clientId = " + clientId + "与 Tap 启动器中" + currentClientId + "不一致");
                            lastIsLaunchedFromTapTapPCResult = 0;
                            return false;
                        }
                        string openId;
                        bool fetchOpenIdSuccess = TapClientBridge.GetTapUserOpenId(out openId);
                        if (fetchOpenIdSuccess)
                        {
                            TapLogger.Debug("IsLaunchedFromTapTapPC get  openId = " + openId);
                            EventManager.TriggerEvent(EventManager.IsLaunchedFromTapTapPCFinished, openId);
                        }
                        else
                        {
                            TapLogger.Debug("IsLaunchedFromTapTapPC get  openId failed");
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
            catch (TimeoutException e)
            {
                lastIsLaunchedFromTapTapPCResult = 0;
                TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, (int)TapSDKInitResult.Timeout, e.Message ?? "");

                TapLogger.Debug("IsLaunchedFromTapTapPC check timeout");
                string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                if (Resources.Load<GameObject>(tipPannelPath) != null)
                {
                    var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(tipPannelPath);
                    pannel.Show(TapSDKInitResult.Timeout);
                }
                return false;
            }
            catch (Exception e)
            {
                lastIsLaunchedFromTapTapPCResult = 0;
                TapCoreTracker.Instance.TrackFailure(TapCoreTracker.METHOD_LAUNCHER, sessionId, (int)TapSDKInitResult.Unknown, e.Message ?? "");

                TapLogger.Debug("IsLaunchedFromTapTapPC check exception = " + e.Message + " \n" + e.StackTrace);
                string tipPannelPath = "Prefabs/TapClient/TapClientConnectTipPanel";
                if (Resources.Load<GameObject>(tipPannelPath) != null)
                {
                    var pannel = UIManager.Instance.OpenUI<TapClientConnectTipController>(tipPannelPath);
                    pannel.Show(TapSDKInitResult.Unknown);
                }
                return false;
            }
        }

        private static async Task<TapInitResult> RunClientBridgeMethodWithTimeout(string clientId, string pubKey)
        {
            TaskCompletionSource<TapInitResult> task = new TaskCompletionSource<TapInitResult>();
            try
            {
                TapInitResult result = await ExecuteWithTimeoutAsync(() =>
                {
                    bool needQuitGame = TapClientBridge.TapSDK_RestartAppIfNecessary(clientId);
                    TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke  TapSDK_RestartAppIfNecessary result = " + needQuitGame);
                    TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke  TapSDK_RestartAppIfNecessary finished " );
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
                TapLogger.Debug("RunClientBridgeMethodWithTimeout invoke C 方法出错！" + ex.Message);
                task.TrySetException(ex);
            }
            return await task.Task;
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


        private static Action<bool, string> currentLoginCallback;

        /// <summary>
        /// 发起登录授权
        /// </summary>
        public static bool StartLoginWithScopes(string[] scopes, string responseType, string redirectUri,
    string codeChallenge, string state, string codeChallengeMethod, string versonCode, string sdkUa, string info, Action<bool, string> callback)
        {
            if (lastIsLaunchedFromTapTapPCResult == -1)
            {
                // UIManager.Instance.OpenToast("IsLaunchedFromTapTapPC 正在执行，请在完成后调用授权接口", UIManager.GeneralToastLevel.Error);
                TapLogger.Error(" login must be invoked after IsLaunchedFromTapTapPC success");
                throw new Exception("login must be invoked after IsLaunchedFromTapTapPC success");
            }
            TapLogger.Debug("LoginWithScopes start login by tapclient thread = " + Thread.CurrentThread.ManagedThreadId);
            try
            {
                TapClientBridge.RegisterCallback(TapEventID.AuthorizeFinished, loginCallbackDelegate);
                AuthorizeResult authorizeResult = TapClientBridge.LoginWithScopes(scopes, responseType, redirectUri,
     codeChallenge, state, codeChallengeMethod, versonCode, sdkUa, info);
                TapLogger.Debug("LoginWithScopes start result = " + authorizeResult);
                if (authorizeResult != AuthorizeResult.OK)
                {
                    TapClientBridge.UnRegisterCallback(TapEventID.AuthorizeFinished,loginCallbackDelegate);
                    return false;
                }
                else
                {
                    currentLoginCallback = callback;
                    return true;
                }

            }
            catch (Exception ex)
            {
                TapLogger.Debug("LoginWithScopes start login by tapclient error = " + ex.Message);
                TapClientBridge.UnRegisterCallback(TapEventID.AuthorizeFinished,loginCallbackDelegate);
                return false;
            }

        }


        [AOT.MonoPInvokeCallback(typeof(TapClientBridge.CallbackDelegate))]
        static void loginCallbackDelegate(int id, IntPtr userData)
        {
            TapLogger.Debug("LoginWithScopes recevie callback " + id);
            if (id == (int)TapEventID.AuthorizeFinished)
            {
                TapLogger.Debug("LoginWithScopes callback thread = " + Thread.CurrentThread.ManagedThreadId);
                TapClientBridge.AuthorizeFinishedResponse response = Marshal.PtrToStructure<TapClientBridge.AuthorizeFinishedResponse>(userData);
                TapLogger.Debug("LoginWithScopes callback = " + response.is_cancel + " uri = " + response.callback_uri);
                if (currentLoginCallback != null)
                {
                    currentLoginCallback(response.is_cancel != 0, response.callback_uri);
                    TapClientBridge.UnRegisterCallback(TapEventID.AuthorizeFinished,loginCallbackDelegate);
                    currentLoginCallback = null;
                }
            }
        }

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
    }
#endif
}
