using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace YWonderLand.CI
{
    public static class BuildScript
    {
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

        private static string GetEnv(string key, string fallback)
        {
            var value = System.Environment.GetEnvironmentVariable(key);
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }
    }
}
