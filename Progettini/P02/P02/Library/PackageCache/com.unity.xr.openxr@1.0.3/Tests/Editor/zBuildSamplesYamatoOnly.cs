﻿using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.OpenXR.Features;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

class zBuildSamplesYamatoOnly
{
    struct SampleBuildTargetSetup
    {
        public BuildTarget buildTarget;
        public BuildTargetGroup targetGroup;
        public Action<string, string> setupPlayerSettings;
        public string outputPostfix;
        public Regex sampleRegex;
    }

    static void WriteAndroidInstallerScripts(string outputFile, string identifier)
    {
        var dir = Path.GetDirectoryName(outputFile);
        if (dir == null) return;
        Directory.CreateDirectory(dir);

        var scripts = new string[] {"install.command", "install.bat"};
        foreach (var script in scripts)
        {
            var scriptPath = Path.Combine(dir, script);

            var scriptContents = $"adb uninstall {identifier}\n" +
                                $"adb install \"{Path.GetFileName(outputFile)}\"\n\n";

            File.AppendAllText(scriptPath, scriptContents);
        }
    }

    static void EnableQuestFeature()
    {
        foreach (var feature in OpenXRSettings.ActiveBuildTargetInstance.features)
        {
            if (feature.nameUi.Contains("Oculus Quest"))
            {
                Console.WriteLine($"Enable: {feature.nameUi}");
                feature.enabled = true;
                return;
            }
        }
        Assert.IsTrue(false, "Could not enable oculus quest extension - if you're not on build machine you must copy dir OculusQuest to your project.");
    }

    static void EnableFeature<TFeatureType>() where TFeatureType : OpenXRFeature
    {
        foreach (var feature in OpenXRSettings.ActiveBuildTargetInstance.features)
        {
            if (feature is TFeatureType)
            {
                Console.WriteLine($"Enable: {feature.nameUi}");
                feature.enabled = true;
                break;
            }
        }
    }

    static void EnableSampleFeatures()
    {
        foreach(var feature in OpenXRSettings.ActiveBuildTargetInstance.features)
        {
            if (feature.GetType().Namespace == null)
            {
                throw new Exception("All code in the OpenXR Package must be in a namespace.");
            }

            if (feature.GetType().Namespace.StartsWith("UnityEngine.XR.OpenXR.Samples"))
            {
                Console.WriteLine($"Enable: {feature.nameUi}");
                feature.enabled = true;
            }
        }
    }

    static void EnableStandaloneProfiles()
    {
        EnableFeature<MicrosoftHandInteraction>();
        EnableFeature<MicrosoftMotionControllerProfile>();
        EnableFeature<HTCViveControllerProfile>();
        EnableFeature<ValveIndexControllerProfile>();
        EnableFeature<OculusTouchControllerProfile>();
    }

    static void EnableWSAProfiles()
    {
        EnableFeature<MicrosoftHandInteraction>();
        EnableFeature<EyeGazeInteraction>();
        EnableFeature<MicrosoftMotionControllerProfile>();
    }

    static void EnableAndroidProfiles()
    {
        EnableFeature<OculusTouchControllerProfile>();
    }

    static SampleBuildTargetSetup[] buildTargetSetup =
    {
#if UNITY_EDITOR_WIN
        new SampleBuildTargetSetup
        {
            buildTarget = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                EnableStandaloneProfiles();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new [] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan });
                OpenXRSettings.ActiveBuildTargetInstance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth24Bit;
            },
            outputPostfix = "dx11",
        },
        new SampleBuildTargetSetup
        {
            sampleRegex = new Regex(".*Render.*"), // Only build dx12 variant for Render Samples
            buildTarget = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new [] { GraphicsDeviceType.Direct3D12, GraphicsDeviceType.Direct3D11 });
                QualitySettings.SetQualityLevel(5);
                QualitySettings.antiAliasing = 4;
            },
            outputPostfix = "dx12",
        },
        new SampleBuildTargetSetup
        {
            sampleRegex = new Regex(".*Render.*"), // Only build vulkan variant for Render Samples
            buildTarget = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new [] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.Direct3D11 });
                OpenXRSettings.ActiveBuildTargetInstance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth24Bit;
            },
            outputPostfix = "vk",
        },
        new SampleBuildTargetSetup
        {
            buildTarget = BuildTarget.WSAPlayer,
            targetGroup = BuildTargetGroup.WSA,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                EnableFeature<EyeGazeInteraction>();
                EnableFeature<MicrosoftHandInteraction>();
                EnableWSAProfiles();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WSAPlayer, new [] { GraphicsDeviceType.Direct3D11 });
                PlayerSettings.WSA.packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.WSA);
                OpenXRSettings.ActiveBuildTargetInstance.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                OpenXRSettings.ActiveBuildTargetInstance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;
            },
            outputPostfix = "dx11",
        },
        new SampleBuildTargetSetup
        {
            sampleRegex = new Regex(".*Render.*"), // Only build dx12 variant for Render Samples
            buildTarget = BuildTarget.WSAPlayer,
            targetGroup = BuildTargetGroup.WSA,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                EnableFeature<EyeGazeInteraction>();
                EnableFeature<MicrosoftHandInteraction>();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WSAPlayer, new [] { GraphicsDeviceType.Direct3D12 });
                QualitySettings.SetQualityLevel(5);
                QualitySettings.antiAliasing = 4;
                PlayerSettings.WSA.packageName = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.WSA);
            },
            outputPostfix = "dx12",
        },
#endif
        new SampleBuildTargetSetup
        {
            sampleRegex = new Regex(".*Render.*"), // Only build vulkan variant for Render Samples
            buildTarget = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                EnableQuestFeature();
                EnableAndroidProfiles();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{ GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                WriteAndroidInstallerScripts(outputFile, identifier);
                OpenXRSettings.ActiveBuildTargetInstance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;
            },
            outputPostfix = "arm64_vk",
        },
        new SampleBuildTargetSetup
        {
            buildTarget = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            setupPlayerSettings = (outputFile, identifier) =>
            {
                EnableSampleFeatures();
                EnableQuestFeature();
                EnableAndroidProfiles();
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new []{ GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                WriteAndroidInstallerScripts(outputFile, identifier);
                OpenXRSettings.ActiveBuildTargetInstance.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.Depth16Bit;
            },
            outputPostfix = "arm64_gles3",
        },
    };

    static string GetBuildFileExt(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return ".apk";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return ".exe";
            default:
                return "";
        }
    }

    static void BuildSamples()
    {
        string resultDir = Path.Combine("..", "OpenXR Samples");
        string projSamplesDir = "Assets/Sample";

        PlayerSettings.colorSpace = ColorSpace.Linear;
        FeatureHelpers.RefreshFeatures(EditorUserBuildSettings.selectedBuildTargetGroup);

        foreach (var setup in buildTargetSetup)
        {
            var sampleName = new DirectoryInfo(projSamplesDir).GetDirectories()[0].Name;
            if (setup.sampleRegex != null && !setup.sampleRegex.Match(sampleName).Success)
                continue;

            if (EditorUserBuildSettings.activeBuildTarget != setup.buildTarget)
                continue;

            string outputDir = Path.Combine(resultDir, setup.buildTarget.ToString());

            string identifier = "com.openxr." + sampleName + "." + setup.outputPostfix;
            PlayerSettings.SetApplicationIdentifier(setup.targetGroup, identifier);
            PlayerSettings.productName = "OpenXR " + sampleName + " " + setup.outputPostfix;
            Console.WriteLine("=========== Setting up player settings (changing graphics apis)");
            string outputFile = Path.Combine(outputDir,
                PlayerSettings.productName + GetBuildFileExt(setup.buildTarget));
            setup.setupPlayerSettings(outputFile, identifier);

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = Directory.GetFiles(projSamplesDir, "*.unity", SearchOption.AllDirectories),
                target = setup.buildTarget,
                targetGroup = setup.targetGroup,
                locationPathName = outputFile,
            };
            Console.WriteLine($"=========== Building {sampleName} {setup.buildTarget}_{setup.outputPostfix}");
            var report = BuildPipeline.BuildPlayer(buildOptions);
            Console.WriteLine($"=========== Build Result {sampleName} {setup.buildTarget}_{setup.outputPostfix} {report.summary.result}");

            if (report.summary.result == BuildResult.Failed)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
