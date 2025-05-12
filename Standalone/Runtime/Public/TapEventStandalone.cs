using System;
using TapSDK.Core.Internal;
using TapSDK.Core.Standalone.Internal;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TapSDK.Core.Standalone
{
    /// <summary>
    /// Represents the standalone implementation of the Tap event.
    /// </summary>
    public class TapEventStandalone : ITapEventPlatform
    {
        private readonly Tracker Tracker = TapCoreStandalone.Tracker;
        private readonly User User = TapCoreStandalone.User;

        /// <summary>
        /// Sets the user ID for tracking events.
        /// </summary>
        /// <param name="userID">The user ID to set.</param>
        public void SetUserID(string userID)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            SetUserID(userID, null);
        }

        /// <summary>
        /// Sets the user ID and additional properties for tracking events.
        /// </summary>
        /// <param name="userID">The user ID to set.</param>
        /// <param name="properties">Additional properties to associate with the user.</param>
        public void SetUserID(string userID, string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            if (!IsValidUserID(userID))
            {
                TapLogger.Error("Invalid user ID, length should be 1-160 and only contains a-zA-Z0-9_+/=.,:");
                return;
            }

            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            User.Login(userID, filterProperties(prop));
        }

        /// <summary>
        /// Clears the current user.
        /// </summary>
        public void ClearUser()
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            User.Logout();
        }

        /// <summary>
        /// Gets the device ID.
        /// </summary>
        /// <returns>The device ID.</returns>
        public string GetDeviceId()
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return "";
            }
            return Identity.DeviceId;
        }

        /// <summary>
        /// Logs an event with the specified name and properties.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="properties">Additional properties to associate with the event.</param>
        public void LogEvent(string name, string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            // name 长度256非空，不符合的丢事件，打log
            if (!checkLength(name))
            {
                Debug.LogError(name + " Event name length should be less than or equal to 256 characters.");
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackEvent(name, filterProperties(prop));
        }

        /// <summary>
        /// Tracks device initialization with the specified properties.
        /// </summary>
        /// <param name="properties">Additional properties to associate with the device initialization.</param>
        public void DeviceInitialize(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackDeviceProperties(Constants.PROPERTY_INITIALIZE_TYPE, filterProperties(prop));
        }

        /// <summary>
        /// Tracks device update with the specified properties.
        /// </summary>
        /// <param name="properties">Additional properties to associate with the device update.</param>
        public void DeviceUpdate(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackDeviceProperties(Constants.PROPERTY_UPDATE_TYPE, filterProperties(prop));
        }

        /// <summary>
        /// Tracks device addition with the specified properties.
        /// </summary>
        /// <param name="properties">Additional properties to associate with the device addition.</param>
        public void DeviceAdd(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackDeviceProperties(Constants.PROPERTY_ADD_TYPE, filterProperties(prop));
        }

        /// <summary>
        /// Tracks user initialization with the specified properties.
        /// </summary>
        /// <param name="properties">Additional properties to associate with the user initialization.</param>
        public void UserInitialize(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackUserProperties(Constants.PROPERTY_INITIALIZE_TYPE, filterProperties(prop));
        }

        /// <summary>
        /// Tracks user update with the specified properties.
        /// </summary>
        /// <param name="properties">Additional properties to associate with the user update.</param>
        public void UserUpdate(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackUserProperties(Constants.PROPERTY_UPDATE_TYPE, filterProperties(prop));
        }

        /// <summary>
        /// Tracks user addition with the specified properties.
        /// </summary>
        /// <param name="properties">Additional properties to associate with the user addition.</param>
        public void UserAdd(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.TrackUserProperties(Constants.PROPERTY_ADD_TYPE, filterProperties(prop));
        }

        /// <summary>
        /// Adds a common property with the specified key and value.
        /// </summary>
        /// <param name="key">The key of the common property.</param>
        /// <param name="value">The value of the common property.</param>
        public void AddCommonProperty(string key, string value)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            if (!checkLength(key))
            {
                Debug.LogError(key + " Property key length should be less than or equal to 256 characters.");
                return;
            }
            if (!checkLength(value))
            {
                Debug.LogError(value + " Property value length should be less than or equal to 256 characters.");
                return;
            }
            Tracker.AddCommonProperty(key, value);
        }

        /// <summary>
        /// Adds common properties with the specified JSON string.
        /// </summary>
        /// <param name="properties">The JSON string containing the common properties.</param>
        public void AddCommon(string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Dictionary<string, object> prop = Json.Deserialize(properties) as Dictionary<string, object>;
            Tracker.AddCommon(filterProperties(prop));
        }

        /// <summary>
        /// Clears the common property with the specified key.
        /// </summary>
        /// <param name="key">The key of the common property to clear.</param>
        public void ClearCommonProperty(string key)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Tracker.ClearCommonProperty(key);
        }

        /// <summary>
        /// Clears the common properties with the specified keys.
        /// </summary>
        /// <param name="keys">The keys of the common properties to clear.</param>
        public void ClearCommonProperties(string[] keys)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Tracker.ClearCommonProperties(keys);
        }

        /// <summary>
        /// Clears all common properties.
        /// </summary>
        public void ClearAllCommonProperties()
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            Tracker.ClearAllCommonProperties();
        }

        /// <summary>
        /// Logs a charge event with the specified details and properties.
        /// </summary>
        /// <param name="orderID">The ID of the order.</param>
        /// <param name="productName">The name of the product.</param>
        /// <param name="amount">The amount of the charge.</param>
        /// <param name="currencyType">The currency type of the charge.</param>
        /// <param name="paymentMethod">The payment method used for the charge.</param>
        /// <param name="properties">Additional properties to associate with the charge event.</param>
        public void LogChargeEvent(string orderID, string productName, long amount, string currencyType, string paymentMethod, string properties)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            if (amount <= 0 || amount > 100000000000)
            {
                UnityEngine.Debug.LogError(amount + " is invalid, amount should be in range (0, 100000000000]");
                return;
            }
            Tracker.LogPurchasedEvent(orderID, productName, amount, currencyType, paymentMethod, properties);
        }

        /// <summary>
        /// Registers a callback function for retrieving dynamic properties.
        /// </summary>
        /// <param name="callback">The callback function that returns a JSON string containing the dynamic properties.</param>
        public void RegisterDynamicProperties(Func<string> callback)
        {
            if (!TapCoreStandalone.CheckInitState())
            {
                return;
            }
            DynamicProperties dynamicProperties = new DynamicProperties(callback);
            Tracker.RegisterDynamicPropsDelegate(dynamicProperties);
        }

        /// <summary>
        /// Represents the implementation of dynamic properties for the Tap event platform.
        /// </summary>
        public class DynamicProperties : Tracker.IDynamicProperties
        {
            readonly Func<string> callback;

            /// <summary>
            /// Initializes a new instance of the <see cref="DynamicProperties"/> class with the specified callback function.
            /// </summary>
            /// <param name="callback">The callback function that returns a JSON string containing the dynamic properties.</param>
            public DynamicProperties(Func<string> callback)
            {
                this.callback = callback;
            }

            /// <summary>
            /// Gets the dynamic properties.
            /// </summary>
            /// <returns>A dictionary containing the dynamic properties.</returns>
            public Dictionary<string, object> GetDynamicProperties()
            {
                var jsonString = callback();
                return Json.Deserialize(jsonString) as Dictionary<string, object>;
            }
        }

        private bool checkLength(string value)
        {
            var maxLength = 256;
            if (value.Length <= 0 || value.Length > maxLength)
            {
                return false;
            }
            return true;
        }

        private bool IsValidUserID(string userID)
        {
            string pattern = @"^[a-zA-Z0-9_+/=.,:]{1,160}$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(userID);
        }

        private Dictionary<string, object> filterProperties(Dictionary<string, object> properties)
        {
            Dictionary<string, object> filteredProperties = new Dictionary<string, object>();
            if(properties != null) {
                foreach (var property in properties)
                {
                    if (property.Key.Length <= 0 || property.Key.Length > 256)
                    {
                        Debug.Log(property.Key + " Property key length should be more then 0 and less than or equal to 256 characters.");
                        continue;
                    }
                    if (property.Value.ToString().Length > 256)
                    {
                        Debug.Log(property.Value + " Property value length should be less than or equal to 256 characters.");
                        continue;
                    }
                    filteredProperties.Add(property.Key, property.Value);
                }
            }
            return filteredProperties;
        }
    }
}
