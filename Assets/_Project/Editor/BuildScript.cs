using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace YWonderLand.CI
{
    public static class BuildScript
    {
        public static void BuildWindows()
        {
            var outputPath = GetEnv(
                "UNITY_WINDOWS_PATH",
                Path.Combine("Builds", "Windows", "YWonderGreenFarm.exe"));
            BuildPlayer(outputPath, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64, "Windows");
        }

        public static void BuildAndroid()
        {
            var outputPath = GetEnv(
                "UNITY_ANDROID_PATH",
                Path.Combine("Builds", "Android", "YWonderGreenFarm.apk"));
            BuildPlayer(outputPath, BuildTargetGroup.Android, BuildTarget.Android, "Android");
        }

        public static void BuildIos()
        {
            var outputPath = GetEnv("UNITY_IOS_DIR", "ios");
            var bundleId = GetEnv("BUNDLE_ID", string.Empty);
            var appVersion = GetEnv("APP_VERSION", string.Empty);
            var buildNumber = GetEnv("BUILD_NUMBER", string.Empty);

            if (!string.IsNullOrWhiteSpace(bundleId))
            {
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, bundleId);
            }

            if (!string.IsNullOrWhiteSpace(appVersion))
            {
                PlayerSettings.bundleVersion = appVersion;
            }

            if (!string.IsNullOrWhiteSpace(buildNumber))
            {
                PlayerSettings.iOS.buildNumber = buildNumber;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                throw new InvalidOperationException("Failed to switch active build target to iOS.");
            }

            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"iOS build failed: {report.summary.result}");
            }
        }

        private static void BuildPlayer(
            string outputPath,
            BuildTargetGroup targetGroup,
            BuildTarget target,
            string platformName)
        {
            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes found in EditorBuildSettings.");
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target))
            {
                throw new InvalidOperationException($"Failed to switch active build target to {platformName}.");
            }

            string fullOutputPath = Path.GetFullPath(outputPath);
            string parentDirectory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
                Directory.CreateDirectory(parentDirectory);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = fullOutputPath,
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{platformName} build failed: {report.summary.result}");
            }
        }

        private static string GetEnv(string key, string fallback)
        {
            var value = System.Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
