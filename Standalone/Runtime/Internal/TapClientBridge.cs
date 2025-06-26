using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;



namespace TapSDK.Core.Standalone.Internal
{

    internal enum TapSDKInitResult
    {
        // 初始化成功
        OK = 0,
        // 其他错误
        FailedGeneric = 1,
        // 未找到 TapTap，用户可能未安装，请引导用户下载安装 TapTap
        NoPlatform = 2,
        // 已安装 TapTap，游戏未通过 TapTap 启动
        NotLaunchedByPlatform = 3,

        // SDK 本地执行时未知错误
        Unknown = -1,
        // SDK 本地执行时超时
        Timeout = -2,
    };

    internal enum TapEventID
    {
        Unknown = 0,

        // [1, 2000), reserved for TapTap platform events
        SystemStateChanged = 1,

        // [2001, 4000), reserved for TapTap user events
        AuthorizeFinished = 2001,
    };

    // 系统事件类型
    internal enum SystemState
    {
        kSystemState_Unknown = 0,            // 未知
        kSystemState_PlatformExit = 1,          // 平台退出
    };

    // 是否触发授权的返回结果
    internal enum AuthorizeResult
    {
        UNKNOWN = 0, // 未知
        OK = 1, // 成功触发授权
        FAILED = 2, // 授权失败
    };

    // 完成授权后的返回结果
    internal enum Result
    {
        kResult_OK = 0,
        kResult_Failed = 1,
        kResult_Canceled = 2,
    };


    public class TapClientBridge
    {

#if UNITY_STANDALONE_WIN
  public const string DLL_NAME = "taptap_api";
#endif

#if UNITY_STANDALONE_WIN
  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern bool TapSDK_RestartAppIfNecessary([MarshalAs( UnmanagedType.LPStr )]string clientId);

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern int TapSDK_Init(StringBuilder errMsg, [MarshalAs( UnmanagedType.LPStr )]string pubKey);

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void TapSDK_Shutdown();

  // 定义与 C 兼容的委托
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  internal delegate void CallbackDelegate(int id, IntPtr userData);
  
  // 系统状态返回结果结构体
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct SystemStateResponse
    {
        public SystemState state; // 枚举直接映射
    }

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void TapSDK_RegisterCallback(int callbackId, IntPtr callback);

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void TapSDK_RunCallbacks();

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void TapSDK_UnregisterCallback(int callbackId, IntPtr callback);


  // 登录相关接口

  // 授权返回结果结构体
   [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct AuthorizeFinishedResponse
    {
        public int is_cancel ; // 是否取消

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
        public string callback_uri; // 256 字节的 C 端字符串

    }

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern bool TapUser_GetOpenID(StringBuilder openId);

  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern bool TapSDK_GetClientID(StringBuilder clientId);


  [global::System.Runtime.InteropServices.DllImport(TapClientBridge.DLL_NAME, CharSet =CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
  internal static extern int TapUser_AsyncAuthorize([MarshalAs( UnmanagedType.LPStr )] string scopeStrings, [MarshalAs( UnmanagedType.LPStr )] string responseType, 
  [MarshalAs( UnmanagedType.LPStr )] string redirectUri, [MarshalAs( UnmanagedType.LPStr )] string codeChallenge, [MarshalAs( UnmanagedType.LPStr )] string state,
  [MarshalAs( UnmanagedType.LPStr )] string codeChallengeMethod, [MarshalAs( UnmanagedType.LPStr )] string versonCode, [MarshalAs( UnmanagedType.LPStr )] string sdkUa, [MarshalAs( UnmanagedType.LPStr )] string info);


   // 初始化检查
   internal static TapSDKInitResult CheckInitState(out string errMessage, string key)
    {
        StringBuilder errMsgBuffer = new StringBuilder(1024); // 分配 1024 字节缓冲区
        int result = TapSDK_Init(errMsgBuffer, key);
        errMessage = errMsgBuffer.ToString();
        TapLogger.Debug("CheckInitState result = " + result);
        return (TapSDKInitResult)result;
    }

    // 预防 GC 回收的静态变量
       private static CallbackDelegate _callbackInstance ;

       private static CallbackDelegate _userCallbackInstance ;

    // 提供 C# 端的注册方法
    /*  授权 callback 实现示例：
     void MyCallback(TapCallbackID id, IntPtr userData){
        AuthorizeFinishedResponse response = Marshal.PtrToStructure<AuthorizeFinishedResponse>(userData);
     }
       系统状态 callback 实现示例：
     void SystemStateCallback(TapCallbackID id, IntPtr state){
        SystemStateResponse response = Marshal.PtrToStructure<SystemStateResponse>(userData);
     }
    */
    internal static void RegisterCallback(TapEventID eventID, CallbackDelegate callback)
    {
       
        IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
        switch (eventID)
        {
            case TapEventID.AuthorizeFinished:
                if (_userCallbackInstance != null)
                {
                    UnRegisterCallback(eventID, _userCallbackInstance);
                }
                _userCallbackInstance = callback;
                break;
            case TapEventID.SystemStateChanged:
                if (_callbackInstance != null)
                {
                    UnRegisterCallback(eventID, _callbackInstance);
                }
                _callbackInstance = callback; // 防止被 GC 回收 
                break;
        }
        
        TapSDK_RegisterCallback((int)eventID, funcPtr);
    }

    // 移除回调
    internal static void UnRegisterCallback(TapEventID eventID,CallbackDelegate callback)
    {
        
        IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
        switch (eventID)
        {
            case TapEventID.AuthorizeFinished:
                _userCallbackInstance = null;
                break;
            case TapEventID.SystemStateChanged:
                _callbackInstance = null;
                break; 
        }
        TapLogger.Debug("start remove delegate ptr " + funcPtr);
        TapSDK_UnregisterCallback((int) eventID, funcPtr);
    }
   
    internal static AuthorizeResult LoginWithScopes(string[] scopeStrings, string responseType, string redirectUri, 
    string codeChallenge, string state, string codeChallengeMethod, string versonCode, string sdkUa, string info) {
        try
        {
            TapLogger.Debug("login start ==== "+ string.Join(",", scopeStrings));
            int result = TapUser_AsyncAuthorize(string.Join(",", scopeStrings), responseType, redirectUri, 
 codeChallenge, state, codeChallengeMethod, versonCode, sdkUa, info);
            TapLogger.Debug("login end === " + result);
            return (AuthorizeResult)result;
        }catch(Exception ex){
            TapLogger.Debug("login crash = " + ex.StackTrace);
            return AuthorizeResult.UNKNOWN;
        }
    }

    internal static bool GetTapUserOpenId(out string openId){
        StringBuilder openIdBuffer = new StringBuilder(256); // 分配一个足够大的缓冲区
        bool success =  TapUser_GetOpenID(openIdBuffer);  // 调用 C 函数
        openId =  openIdBuffer.ToString();
        return success;
    }

     internal static bool GetClientId(out string clientId){
        StringBuilder clientIDBuffer = new StringBuilder(256); // 分配一个足够大的缓冲区
         bool success = TapSDK_GetClientID(clientIDBuffer);  // 调用 C 函数
         clientId = clientIDBuffer.ToString();
        return success;  
    }

#endif
    }



}