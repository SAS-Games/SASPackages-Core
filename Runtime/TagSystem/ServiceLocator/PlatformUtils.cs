using UnityEngine;

namespace SAS.Core.TagSystem
{
    public enum PlatformType
    {
        Windows,
        MacOS,
        Linux,
        Android,
        iOS,
        PS4,
        PS5,
        XboxOne,
        XboxSeries,
        Switch
    }
    
    public static class PlatformUtils
    {
        public static bool IsPlatformExcluded(PlatformType[] excludedPlatforms)
        {
            foreach (var platform in excludedPlatforms)
            {
                switch (platform)
                {
                    case PlatformType.Windows:
                        if (Application.platform == RuntimePlatform.WindowsPlayer ||
                            Application.platform == RuntimePlatform.WindowsEditor)
                            return true;
                        break;
                    case PlatformType.MacOS:
                        if (Application.platform == RuntimePlatform.OSXPlayer ||
                            Application.platform == RuntimePlatform.OSXEditor)
                            return true;
                        break;
                    case PlatformType.Linux:
                        if (Application.platform == RuntimePlatform.LinuxPlayer ||
                            Application.platform == RuntimePlatform.LinuxEditor)
                            return true;
                        break;
                    case PlatformType.Android:
                        if (Application.platform == RuntimePlatform.Android)
                            return true;
                        break;
                    case PlatformType.iOS:
                        if (Application.platform == RuntimePlatform.IPhonePlayer)
                            return true;
                        break;
                    case PlatformType.PS4:
                        if (Application.platform == RuntimePlatform.PS4)
                            return true;
                        break;
                    case PlatformType.PS5:
                        if (Application.platform == RuntimePlatform.PS5)
                            return true;
                        break;
                    case PlatformType.XboxOne:
                        if (Application.platform == RuntimePlatform.XboxOne)
                            return true;
                        break;
                    case PlatformType.XboxSeries:
#if UNITY_GAMECORE_XBOX_SERIES
                        return true;
#endif
                        break;
                    case PlatformType.Switch:
                        if (Application.platform == RuntimePlatform.Switch)
                            return true;
                        break;
                }
            }

            return false;
        }
    }
}