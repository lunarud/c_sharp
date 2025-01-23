using System;
using System.Collections.Generic;
namespace _6_xx_1
{
    public class GlobalSettings
    {
        private Dictionary<int, object> settings;

        public GlobalSettings()
        {
            settings = new Dictionary<int, object>();
        }

        public T Get<T>(int key, Action<T> callbackOnSettingChanged)
        {
            if (settings.ContainsKey(key))
            {
                return (T)settings[key];
            }

            return default(T);
        }

        public void Set<T>(int key, T value)
        {
            if (settings.ContainsKey(key))
            {
                var oldValue = (T)settings[key];
                if (!EqualityComparer<T>.Default.Equals(oldValue, value))
                {
                    settings[key] = value;
                    NotifySettingChanged(key, value);
                }
            }
            else
            {
                settings.Add(key, value);
                NotifySettingChanged(key, value);
            }
        }

        private void NotifySettingChanged<T>(int key, T value)
        {
            // You can implement the notification mechanism here
            Console.WriteLine($"Setting {key} changed to {value}");

            // You can also invoke the callback if provided
            // Note: Make sure to handle any exceptions in the callback code
            // Example:
            // callbackOnSettingChanged?.Invoke(value);
        }
    }


    internal class Program
    {
        static void Main(string[] args)
        {
            GlobalSettings settings = new GlobalSettings();

            // Subscribe to setting changes
            settings.Get<bool>(1, value =>
            {
                Console.WriteLine($"DisableMagicMoments setting changed to {value}");
            });

            // Get setting value
            bool disableMagicMoments = settings.Get<bool>(1, null);
            Console.WriteLine($"DisableMagicMoments: {disableMagicMoments}");

            // Set setting value
            settings.Set(1, true);

            // Get updated setting value
            disableMagicMoments = settings.Get<bool>(1, null);
            Console.WriteLine($"DisableMagicMoments: {disableMagicMoments}");
            Console.ReadLine();
        }
    }
}
