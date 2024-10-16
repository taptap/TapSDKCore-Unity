// #if UNITY_EDITOR || (!UNITY_ANDROID && !UNITY_IOS)
// using Google.Protobuf;

// namespace TapSDK.Core.Standalone.Internal.Utils
// {

//     public class TapProtoBufffer
//     {
//         public static byte[] Serialize(IMessage message)
//         {
//             return message.ToByteArray();
//         }

//         public static T DeSerialize<T>(byte[] packet) where T : IMessage, new()
//         {
//             IMessage message = new T();
//             try
//             {
//                 return (T)message.Descriptor.Parser.ParseFrom(packet);
//             }
//             catch (System.Exception e)
//             {
//                 throw;
//             }
//         }
//     }
// }
// #endif