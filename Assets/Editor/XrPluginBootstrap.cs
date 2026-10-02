using UnityEditor;
using UnityEditor.XR.ARKit;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

namespace ARTrackBuilder.Editor
{
    /// <summary>
    /// Activa ARCore en Android y ARKit en iOS la primera vez que el editor compila
    /// con XR Plug-in Management disponible, y deja los Player Settings del prototipo.
    /// </summary>
    [InitializeOnLoad]
    internal static class XrPluginBootstrap
    {
        private const string XR_SETTINGS_KEY = "com.unity.xr.management.loader_settings";
        private const string XR_SETTINGS_ASSET_PATH = "Assets/XR/XRGeneralSettings.asset";
        private const string SESSION_KEY = "ARTrackBuilder.XrConfigured";
        private const string ARCORE_LOADER = "UnityEngine.XR.ARCore.ARCoreLoader";
        private const string ARKIT_LOADER = "UnityEngine.XR.ARKit.ARKitLoader";
        private const string CAMERA_USAGE_DESCRIPTION = "ARTrackBuilder usa la cámara para detectar el tapete de juego.";
        private const string APPLICATION_IDENTIFIER = "com.TuNombre.ARTrackBuilder";
        private const string IOS_MIN_VERSION = "12.0";
        private const int IOS_ARM64 = 1;
        private const int MAX_ATTEMPTS = 10;

        private static int _attempts;

        static XrPluginBootstrap()
        {
            EditorApplication.delayCall += TryConfigure;
        }

        private static void TryConfigure()
        {
            if (SessionState.GetBool(SESSION_KEY, false))
            {
                return;
            }

            ApplyPlayerSettings();

            if (ConfigureLoaders())
            {
                CompleteConfiguration(true);
                return;
            }

            _attempts++;
            if (_attempts < MAX_ATTEMPTS)
            {
                EditorApplication.delayCall += TryConfigure;
                return;
            }

            CompleteConfiguration(false);
        }

        private static void CompleteConfiguration(bool loadersReady)
        {
            SessionState.SetBool(SESSION_KEY, true);
            AssetDatabase.SaveAssets();

            if (loadersReady)
            {
                Debug.Log("[AR] ARCore (Android) y ARKit (iOS) quedaron activos en XR Plug-in Management.");
            }
            else
            {
                Debug.LogError("[AR] No se pudieron activar ARCore/ARKit. Revisa Edit > Project Settings > XR Plug-in Management.");
            }

            SwitchActivePlatform();
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            if ((int)PlayerSettings.Android.minSdkVersion < (int)AndroidSdkVersions.AndroidApiLevel24)
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            }

            AssignIdentifierIfDefault(BuildTargetGroup.Android);
            AssignIdentifierIfDefault(BuildTargetGroup.iOS);

            PlayerSettings.iOS.cameraUsageDescription = CAMERA_USAGE_DESCRIPTION;
            if (IsBelowIos12(PlayerSettings.iOS.targetOSVersionString))
            {
                PlayerSettings.iOS.targetOSVersionString = IOS_MIN_VERSION;
            }

            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, IOS_ARM64);

            var arkitSettings = ARKitSettings.GetOrCreateSettings();
            arkitSettings.requirement = ARKitSettings.Requirement.Required;
            EditorUtility.SetDirty(arkitSettings);
        }

        private static void AssignIdentifierIfDefault(BuildTargetGroup buildTargetGroup)
        {
            var current = PlayerSettings.GetApplicationIdentifier(buildTargetGroup);
            if (string.IsNullOrEmpty(current) || current.Contains("DefaultCompany"))
            {
                PlayerSettings.SetApplicationIdentifier(buildTargetGroup, APPLICATION_IDENTIFIER);
            }
        }

        private static bool IsBelowIos12(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return true;
            }

            var dot = version.IndexOf('.');
            var majorText = dot >= 0 ? version.Substring(0, dot) : version;
            return !int.TryParse(majorText, out var major) || major < 12;
        }

        private static void SwitchActivePlatform()
        {
            var target = Application.platform == RuntimePlatform.OSXEditor
                ? BuildTarget.iOS
                : BuildTarget.Android;
            var group = target == BuildTarget.iOS ? BuildTargetGroup.iOS : BuildTargetGroup.Android;

            if (EditorUserBuildSettings.activeBuildTarget == target)
            {
                return;
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
        }

        private static bool ConfigureLoaders()
        {
            var generalSettings = GetOrCreateGeneralSettings();
            if (generalSettings == null)
            {
                return false;
            }

            var androidReady = EnsureLoader(generalSettings, BuildTargetGroup.Android, ARCORE_LOADER);
            var iosReady = EnsureLoader(generalSettings, BuildTargetGroup.iOS, ARKIT_LOADER);
            if (!androidReady || !iosReady)
            {
                return false;
            }

            EditorUtility.SetDirty(generalSettings);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateGeneralSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject(XR_SETTINGS_KEY, out XRGeneralSettingsPerBuildTarget registered) && registered != null)
            {
                return registered;
            }

            var existing = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XR_SETTINGS_ASSET_PATH);
            if (existing == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR"))
                {
                    AssetDatabase.CreateFolder("Assets", "XR");
                }

                existing = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(existing, XR_SETTINGS_ASSET_PATH);
            }

            EditorBuildSettings.AddConfigObject(XR_SETTINGS_KEY, existing, true);
            return existing;
        }

        private static bool EnsureLoader(XRGeneralSettingsPerBuildTarget generalSettings, BuildTargetGroup buildTargetGroup, string loaderTypeName)
        {
            if (XRPackageMetadataStore.IsLoaderAssigned(loaderTypeName, buildTargetGroup))
            {
                return true;
            }

            if (!generalSettings.HasSettingsForBuildTarget(buildTargetGroup))
            {
                generalSettings.CreateDefaultSettingsForBuildTarget(buildTargetGroup);
            }

            if (!generalSettings.HasManagerSettingsForBuildTarget(buildTargetGroup))
            {
                generalSettings.CreateDefaultManagerSettingsForBuildTarget(buildTargetGroup);
            }

            var managerSettings = generalSettings.ManagerSettingsForBuildTarget(buildTargetGroup);
            if (managerSettings == null)
            {
                return false;
            }

            return XRPackageMetadataStore.AssignLoader(managerSettings, loaderTypeName, buildTargetGroup);
        }
    }
}
