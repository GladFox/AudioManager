#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AudioManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioManagementEditor
{
    public static class AudioProductionSetup
    {
        private const string MixerPath = "Assets/Audio/Data/AudioMain.mixer";
        private const string ConfigPath = "Assets/Resources/Audio/AudioConfig.asset";
        private const string ClipsFolder = "Assets/Audio/Data/GeneratedClips";
        private const string EventsFolder = "Assets/Audio/Data/SoundEvents";

        private static readonly (AudioBus Bus, string GroupName, string ExposedName)[] BusBindings =
        {
            (AudioBus.Master, "Master", "MasterVolume"),
            (AudioBus.Music, "Music", "MusicVolume"),
            (AudioBus.Sfx, "SFX", "SFXVolume"),
            (AudioBus.UI, "UI", "UIVolume"),
            (AudioBus.Ambience, "Ambience", "AmbienceVolume"),
            (AudioBus.Voice, "Voice", "VoiceVolume")
        };

        private static readonly (string Name, int Priority)[] SnapshotBindings =
        {
            ("Default", 0),
            ("Gameplay", 10),
            ("Menu", 20),
            ("Muffled", 25),
            ("Pause", 30),
            ("Muted", 100)
        };

        private static readonly Dictionary<string, Dictionary<AudioBus, float>> SnapshotVolumesDb = new Dictionary<string, Dictionary<AudioBus, float>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Default"] = new Dictionary<AudioBus, float>
            {
                [AudioBus.Master] = 0f,
                [AudioBus.Music] = 0f,
                [AudioBus.Sfx] = 0f,
                [AudioBus.UI] = 0f,
                [AudioBus.Ambience] = 0f,
                [AudioBus.Voice] = 0f
            },
            ["Gameplay"] = new Dictionary<AudioBus, float>
            {
                [AudioBus.Master] = 0f,
                [AudioBus.Music] = -1f,
                [AudioBus.Sfx] = 0f,
                [AudioBus.UI] = 0f,
                [AudioBus.Ambience] = -1f,
                [AudioBus.Voice] = 0f
            },
            ["Menu"] = new Dictionary<AudioBus, float>
            {
                [AudioBus.Master] = 0f,
                [AudioBus.Music] = -6f,
                [AudioBus.Sfx] = -10f,
                [AudioBus.UI] = 0f,
                [AudioBus.Ambience] = -8f,
                [AudioBus.Voice] = -4f
            },
            ["Muffled"] = new Dictionary<AudioBus, float>
            {
                [AudioBus.Master] = -4f,
                [AudioBus.Music] = -8f,
                [AudioBus.Sfx] = -12f,
                [AudioBus.UI] = -4f,
                [AudioBus.Ambience] = -10f,
                [AudioBus.Voice] = -6f
            },
            ["Pause"] = new Dictionary<AudioBus, float>
            {
                [AudioBus.Master] = 0f,
                [AudioBus.Music] = -12f,
                [AudioBus.Sfx] = -80f,
                [AudioBus.UI] = 0f,
                [AudioBus.Ambience] = -18f,
                [AudioBus.Voice] = -8f
            },
            ["Muted"] = new Dictionary<AudioBus, float>
            {
                [AudioBus.Master] = -80f,
                [AudioBus.Music] = -80f,
                [AudioBus.Sfx] = -80f,
                [AudioBus.UI] = -80f,
                [AudioBus.Ambience] = -80f,
                [AudioBus.Voice] = -80f
            }
        };

        [MenuItem("Tools/Audio/Setup/Generate Production Assets")]
        public static void GenerateProductionAssets()
        {
            EnsureFolders();

            var mixerController = EnsureMixerController();
            var groupsByBus = EnsureGroups(mixerController);
            var snapshots = EnsureSnapshots(mixerController);
            EnsureSnapshotVolumes(mixerController, groupsByBus, snapshots);
            EnsureExposedParameters(mixerController, groupsByBus);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var clips = EnsureDemoClips();
            var events = EnsureDemoEvents(clips);
            EnsureAudioConfigAsset(mixerController, groupsByBus, snapshots, events);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[AudioProductionSetup] Production audio assets are generated/updated.");
        }

        public static void GenerateProductionAssetsBatch()
        {
            GenerateProductionAssets();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Audio/Data"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Audio/Data/SoundEvents"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Audio/Data/GeneratedClips"));
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Resources/Audio"));
        }

        private static object EnsureMixerController()
        {
            var controllerType = GetEditorType("UnityEditor.Audio.AudioMixerController");
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

            if (mixer == null)
            {
                var createAtPath = controllerType.GetMethod("CreateMixerControllerAtPath", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (createAtPath == null)
                {
                    throw new MissingMethodException("AudioMixerController.CreateMixerControllerAtPath is unavailable.");
                }

                createAtPath.Invoke(null, new object[] { MixerPath });
                AssetDatabase.ImportAsset(MixerPath, ImportAssetOptions.ForceSynchronousImport);
            }

            var controller = LoadSubAssetByType(MixerPath, controllerType);
            if (controller == null)
            {
                throw new InvalidOperationException("Unable to load AudioMixerController from mixer asset path.");
            }

            EditorUtility.SetDirty((UnityEngine.Object)controller);
            return controller;
        }

        private static Dictionary<AudioBus, object> EnsureGroups(object controller)
        {
            var controllerType = controller.GetType();
            var groupType = GetEditorType("UnityEditor.Audio.AudioMixerGroupController");

            var createGroupMethod = controllerType.GetMethod("CreateNewGroup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var addChildMethod = controllerType.GetMethod("AddChildToParent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var masterGroup = controllerType.GetProperty("masterGroup", BindingFlags.Instance | BindingFlags.Public).GetValue(controller);
            if (createGroupMethod == null || addChildMethod == null || masterGroup == null)
            {
                throw new MissingMethodException("AudioMixerController group methods are unavailable.");
            }

            var byBus = new Dictionary<AudioBus, object>();
            byBus[AudioBus.Master] = masterGroup;

            foreach (var binding in BusBindings)
            {
                if (binding.Bus == AudioBus.Master)
                {
                    continue;
                }

                var existing = FindGroupByName(masterGroup, binding.GroupName, groupType);
                if (existing == null)
                {
                    existing = createGroupMethod.Invoke(controller, new object[] { binding.GroupName, false });
                    addChildMethod.Invoke(controller, new[] { existing, masterGroup });
                }

                byBus[binding.Bus] = existing;
            }

            return byBus;
        }

        private static Dictionary<string, object> EnsureSnapshots(object controller)
        {
            var controllerType = controller.GetType();
            var snapshotType = GetEditorType("UnityEditor.Audio.AudioMixerSnapshotController");
            var snapshotsProp = controllerType.GetProperty("snapshots", BindingFlags.Instance | BindingFlags.Public);
            var targetSnapshotProp = controllerType.GetProperty("TargetSnapshot", BindingFlags.Instance | BindingFlags.Public);
            var startSnapshotProp = controllerType.GetProperty("startSnapshot", BindingFlags.Instance | BindingFlags.Public);

            var cloneMethod = controllerType.GetMethod("CloneNewSnapshotFromTarget", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (cloneMethod == null)
            {
                throw new MissingMethodException("AudioMixerController.CloneNewSnapshotFromTarget is unavailable.");
            }

            var snapshots = ((Array)snapshotsProp.GetValue(controller)).Cast<object>().ToList();
            if (snapshots.Count == 0)
            {
                throw new InvalidOperationException("Mixer has no snapshots after creation.");
            }

            var nameProp = snapshotType.GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
            nameProp.SetValue(snapshots[0], "Default");

            foreach (var required in SnapshotBindings)
            {
                var existing = snapshots.FirstOrDefault(s => string.Equals((string)nameProp.GetValue(s), required.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    continue;
                }

                targetSnapshotProp.SetValue(controller, snapshots[0]);
                cloneMethod.Invoke(controller, new object[] { false });

                snapshots = ((Array)snapshotsProp.GetValue(controller)).Cast<object>().ToList();
                var created = snapshots[snapshots.Count - 1];
                nameProp.SetValue(created, required.Name);
            }

            snapshots = ((Array)snapshotsProp.GetValue(controller)).Cast<object>().ToList();
            var byName = snapshots.ToDictionary(s => (string)nameProp.GetValue(s), s => s, StringComparer.OrdinalIgnoreCase);

            if (byName.TryGetValue("Default", out var defaultSnapshot))
            {
                startSnapshotProp.SetValue(controller, defaultSnapshot);
            }

            return byName;
        }

        private static void EnsureSnapshotVolumes(object controller, Dictionary<AudioBus, object> groups, Dictionary<string, object> snapshots)
        {
            var groupType = GetEditorType("UnityEditor.Audio.AudioMixerGroupController");
            var setVolumeMethod = groupType.GetMethod("SetValueForVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (setVolumeMethod == null)
            {
                throw new MissingMethodException("AudioMixerGroupController.SetValueForVolume is unavailable.");
            }

            foreach (var snapshotPair in snapshots)
            {
                if (!SnapshotVolumesDb.TryGetValue(snapshotPair.Key, out var volumes))
                {
                    continue;
                }

                foreach (var busPair in groups)
                {
                    if (!volumes.TryGetValue(busPair.Key, out var db))
                    {
                        continue;
                    }

                    setVolumeMethod.Invoke(busPair.Value, new[] { controller, snapshotPair.Value, (object)db });
                }
            }
        }

        private static void EnsureExposedParameters(object controller, Dictionary<AudioBus, object> groups)
        {
            var controllerType = controller.GetType();
            var groupType = GetEditorType("UnityEditor.Audio.AudioMixerGroupController");
            var exposedParamType = GetEditorType("UnityEditor.Audio.ExposedAudioParameter");

            var exposedProp = controllerType.GetProperty("exposedParameters", BindingFlags.Instance | BindingFlags.Public);
            var onChangedMethod = controllerType.GetMethod("OnChangedExposedParameter", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var guidField = exposedParamType.GetField("guid", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var nameField = exposedParamType.GetField("name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            var getVolumeGuidMethod = groupType.GetMethod("GetGUIDForVolume", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (exposedProp == null || guidField == null || nameField == null || getVolumeGuidMethod == null)
            {
                throw new MissingMethodException("AudioMixer exposed parameter API is unavailable.");
            }

            foreach (var binding in BusBindings)
            {
                if (!groups.TryGetValue(binding.Bus, out var group))
                {
                    continue;
                }

                var guid = getVolumeGuidMethod.Invoke(group, null);
                var exposedArray = (Array)exposedProp.GetValue(controller);

                var exists = false;
                for (var i = 0; i < exposedArray.Length; i++)
                {
                    var item = exposedArray.GetValue(i);
                    if (Equals(guidField.GetValue(item), guid))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var expanded = Array.CreateInstance(exposedParamType, exposedArray.Length + 1);
                    Array.Copy(exposedArray, expanded, exposedArray.Length);
                    var item = Activator.CreateInstance(exposedParamType);
                    guidField.SetValue(item, guid);
                    nameField.SetValue(item, binding.ExposedName);
                    expanded.SetValue(item, expanded.Length - 1);
                    exposedProp.SetValue(controller, expanded);
                    exposedArray = expanded;
                }

                exposedArray = (Array)exposedProp.GetValue(controller);
                for (var i = 0; i < exposedArray.Length; i++)
                {
                    var item = exposedArray.GetValue(i);
                    if (!Equals(guidField.GetValue(item), guid))
                    {
                        continue;
                    }

                    nameField.SetValue(item, binding.ExposedName);
                    exposedArray.SetValue(item, i);
                }

                exposedProp.SetValue(controller, exposedArray);
            }

            onChangedMethod?.Invoke(controller, null);
        }

        private static Dictionary<string, AudioClip> EnsureDemoClips()
        {
            var clips = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase)
            {
                ["demo.ui.click"] = EnsureToneClip("UI_Click", 1320f, 0.08f, 0.20f, false),
                ["demo.sfx.moving"] = EnsureToneClip("SFX_Moving", 440f, 0.28f, 0.25f, false),
                ["demo.music.loop"] = EnsureMusicLoopClip("Music_Loop", 6f, 0.09f, true)
            };

            return clips;
        }

        private static Dictionary<string, SoundEvent> EnsureDemoEvents(Dictionary<string, AudioClip> clips)
        {
            var events = new Dictionary<string, SoundEvent>(StringComparer.OrdinalIgnoreCase)
            {
                ["demo.ui.click"] = EnsureSoundEvent(
                    Path.Combine(EventsFolder, "Demo_UI_Click.asset").Replace('\\', '/'),
                    id: "demo.ui.click",
                    bus: AudioBus.UI,
                    clip: clips["demo.ui.click"],
                    loop: false,
                    spatialMode: SpatialMode.TwoD,
                    volume: 0.9f,
                    pitchMin: 0.98f,
                    pitchMax: 1.02f,
                    maxInstances: 8,
                    cooldown: 0.02f,
                    priority: 96,
                    minDistance: 1f,
                    maxDistance: 20f),
                ["demo.sfx.moving"] = EnsureSoundEvent(
                    Path.Combine(EventsFolder, "Demo_SFX_Moving.asset").Replace('\\', '/'),
                    id: "demo.sfx.moving",
                    bus: AudioBus.Sfx,
                    clip: clips["demo.sfx.moving"],
                    loop: false,
                    spatialMode: SpatialMode.ThreeD,
                    volume: 1f,
                    pitchMin: 0.95f,
                    pitchMax: 1.05f,
                    maxInstances: 16,
                    cooldown: 0f,
                    priority: 110,
                    minDistance: 1.2f,
                    maxDistance: 22f),
                ["demo.music.loop"] = EnsureSoundEvent(
                    Path.Combine(EventsFolder, "Demo_Music_Loop.asset").Replace('\\', '/'),
                    id: "demo.music.loop",
                    bus: AudioBus.Music,
                    clip: clips["demo.music.loop"],
                    loop: true,
                    spatialMode: SpatialMode.TwoD,
                    volume: 0.85f,
                    pitchMin: 1f,
                    pitchMax: 1f,
                    maxInstances: 1,
                    cooldown: 0f,
                    priority: 64,
                    minDistance: 1f,
                    maxDistance: 20f)
            };

            return events;
        }

        private static void EnsureAudioConfigAsset(object mixerController, Dictionary<AudioBus, object> groups, Dictionary<string, object> snapshots, Dictionary<string, SoundEvent> events)
        {
            var mixerAsset = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            var config = AssetDatabase.LoadAssetAtPath<AudioConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AudioConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var so = new SerializedObject(config);

            so.FindProperty("mixer").objectReferenceValue = mixerAsset;

            var mixerGroupsProp = so.FindProperty("mixerGroups");
            mixerGroupsProp.arraySize = BusBindings.Length;
            for (var i = 0; i < BusBindings.Length; i++)
            {
                var entry = mixerGroupsProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Bus").enumValueIndex = (int)BusBindings[i].Bus;
                entry.FindPropertyRelative("Group").objectReferenceValue = groups.TryGetValue(BusBindings[i].Bus, out var group)
                    ? group as UnityEngine.Object
                    : null;
            }

            var snapshotsProp = so.FindProperty("snapshots");
            snapshotsProp.arraySize = SnapshotBindings.Length;
            for (var i = 0; i < SnapshotBindings.Length; i++)
            {
                var entry = snapshotsProp.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Name").stringValue = SnapshotBindings[i].Name;
                entry.FindPropertyRelative("Priority").intValue = SnapshotBindings[i].Priority;
                entry.FindPropertyRelative("Snapshot").objectReferenceValue = snapshots.TryGetValue(SnapshotBindings[i].Name, out var snapshot)
                    ? snapshot as UnityEngine.Object
                    : null;
            }

            var exposed = so.FindProperty("exposedVolumeParams");
            exposed.FindPropertyRelative("Master").stringValue = "MasterVolume";
            exposed.FindPropertyRelative("Music").stringValue = "MusicVolume";
            exposed.FindPropertyRelative("Sfx").stringValue = "SFXVolume";
            exposed.FindPropertyRelative("UI").stringValue = "UIVolume";
            exposed.FindPropertyRelative("Ambience").stringValue = "AmbienceVolume";
            exposed.FindPropertyRelative("Voice").stringValue = "VoiceVolume";

            var defaults = so.FindProperty("defaultVolumes");
            defaults.FindPropertyRelative("Master").floatValue = 1f;
            defaults.FindPropertyRelative("Music").floatValue = 0.9f;
            defaults.FindPropertyRelative("Sfx").floatValue = 1f;
            defaults.FindPropertyRelative("UI").floatValue = 1f;
            defaults.FindPropertyRelative("Ambience").floatValue = 0.9f;
            defaults.FindPropertyRelative("Voice").floatValue = 1f;

            var pool2D = so.FindProperty("pool2D");
            pool2D.FindPropertyRelative("InitialSize").intValue = 24;
            pool2D.FindPropertyRelative("MaxSize").intValue = 96;
            pool2D.FindPropertyRelative("ExpandStep").intValue = 8;
            pool2D.FindPropertyRelative("StealPolicy").enumValueIndex = (int)StealPolicy.StealLowestPriority;
            pool2D.FindPropertyRelative("AutoReleaseCheckInterval").floatValue = 0.08f;

            var pool3D = so.FindProperty("pool3D");
            pool3D.FindPropertyRelative("InitialSize").intValue = 20;
            pool3D.FindPropertyRelative("MaxSize").intValue = 96;
            pool3D.FindPropertyRelative("ExpandStep").intValue = 8;
            pool3D.FindPropertyRelative("StealPolicy").enumValueIndex = (int)StealPolicy.StealLowestPriority;
            pool3D.FindPropertyRelative("AutoReleaseCheckInterval").floatValue = 0.08f;

            var soundEventsProp = so.FindProperty("soundEvents");
            var orderedEvents = events.Values.OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase).ToArray();
            soundEventsProp.arraySize = orderedEvents.Length;
            for (var i = 0; i < orderedEvents.Length; i++)
            {
                soundEventsProp.GetArrayElementAtIndex(i).objectReferenceValue = orderedEvents[i];
            }

            so.FindProperty("enableDebugLogs").boolValue = true;
            so.FindProperty("pauseSfxAndMusicOnFocusLost").boolValue = true;
            so.FindProperty("pauseSfxAndMusicOnApplicationPause").boolValue = true;
            so.FindProperty("uiAlwaysUnscaled").boolValue = true;
            so.FindProperty("minDb").floatValue = -80f;
            so.FindProperty("maxDb").floatValue = 0f;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);

            EnsureDemoSceneInBuildSettings();
        }

        private static void EnsureDemoSceneInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            AddSceneIfMissing(scenes, "Assets/Scenes/AudioDemoScene.unity");
            AddSceneIfMissing(scenes, "Assets/Scenes/SampleScene.unity");
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddSceneIfMissing(List<EditorBuildSettingsScene> scenes, string path)
        {
            if (scenes.Any(s => string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
        }

        private static SoundEvent EnsureSoundEvent(
            string assetPath,
            string id,
            AudioBus bus,
            AudioClip clip,
            bool loop,
            SpatialMode spatialMode,
            float volume,
            float pitchMin,
            float pitchMax,
            int maxInstances,
            float cooldown,
            int priority,
            float minDistance,
            float maxDistance)
        {
            var evt = AssetDatabase.LoadAssetAtPath<SoundEvent>(assetPath);
            if (evt == null)
            {
                evt = ScriptableObject.CreateInstance<SoundEvent>();
                AssetDatabase.CreateAsset(evt, assetPath);
            }

            var so = new SerializedObject(evt);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("clipSelection").enumValueIndex = (int)ClipSelectionMode.Random;

            var clipsProp = so.FindProperty("clips");
            clipsProp.arraySize = 1;
            clipsProp.GetArrayElementAtIndex(0).objectReferenceValue = clip;

            so.FindProperty("weightedClips").arraySize = 0;
            so.FindProperty("mixerBus").enumValueIndex = (int)bus;
            so.FindProperty("volume").floatValue = Mathf.Clamp01(volume);
            so.FindProperty("pitchRange").vector2Value = new Vector2(Mathf.Max(0.01f, pitchMin), Mathf.Max(0.01f, pitchMax));
            so.FindProperty("randomStartOffsetMax").floatValue = 0f;
            so.FindProperty("loop").boolValue = loop;
            so.FindProperty("spatialMode").enumValueIndex = (int)spatialMode;
            so.FindProperty("minDistance").floatValue = Mathf.Max(0f, minDistance);
            so.FindProperty("maxDistance").floatValue = Mathf.Max(minDistance, maxDistance);
            so.FindProperty("rolloffMode").enumValueIndex = (int)AudioRolloffMode.Logarithmic;
            so.FindProperty("priority").intValue = Mathf.Clamp(priority, 0, 256);
            so.FindProperty("maxInstances").intValue = Mathf.Max(1, maxInstances);
            so.FindProperty("cooldownSeconds").floatValue = Mathf.Max(0f, cooldown);
            so.FindProperty("duckSfxOnUi").boolValue = false;
            so.FindProperty("bypassReverbZones").boolValue = false;
            so.FindProperty("bypassEffects").boolValue = false;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(evt);
            return evt;
        }

        private static AudioClip EnsureToneClip(string baseName, float frequency, float durationSeconds, float amplitude, bool loopable)
        {
            var relativePath = Path.Combine(ClipsFolder, baseName + ".wav").Replace('\\', '/');
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (!File.Exists(absolutePath))
            {
                var samples = GenerateToneSamples(frequency, durationSeconds, amplitude);
                WriteWaveFile(absolutePath, samples, 44100);
            }

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureImportedClip(relativePath, loopable);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
        }

        private static AudioClip EnsureMusicLoopClip(string baseName, float durationSeconds, float amplitude, bool loopable)
        {
            var relativePath = Path.Combine(ClipsFolder, baseName + ".wav").Replace('\\', '/');
            var absolutePath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

            if (!File.Exists(absolutePath))
            {
                var samples = GenerateMusicSamples(durationSeconds, amplitude);
                WriteWaveFile(absolutePath, samples, 44100);
            }

            AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceSynchronousImport);
            ConfigureImportedClip(relativePath, loopable);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
        }

        private static void ConfigureImportedClip(string assetPath, bool loopable)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null)
            {
                return;
            }

            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.ambisonic = false;
            importer.loopable = loopable;

            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.quality = 1f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        private static float[] GenerateToneSamples(float frequency, float durationSeconds, float amplitude)
        {
            var sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            var attack = Mathf.Max(1, Mathf.CeilToInt(sampleRate * 0.005f));
            var release = Mathf.Max(1, Mathf.CeilToInt(sampleRate * 0.012f));
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var env = 1f;
                if (i < attack)
                {
                    env = i / (float)attack;
                }
                else if (i > sampleCount - release)
                {
                    env = (sampleCount - i) / (float)release;
                }

                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * amplitude * Mathf.Clamp01(env);
            }

            return data;
        }

        private static float[] GenerateMusicSamples(float durationSeconds, float amplitude)
        {
            var sampleRate = 44100;
            var sampleCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds * sampleRate));
            var data = new float[sampleCount];

            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)sampleRate;
                var a = Mathf.Sin(2f * Mathf.PI * 220f * t) * 0.6f;
                var b = Mathf.Sin(2f * Mathf.PI * 330f * t) * 0.28f;
                var c = Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.2f;
                data[i] = (a + b + c) * amplitude;
            }

            return data;
        }

        private static void WriteWaveFile(string absolutePath, float[] samples, int sampleRate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath) ?? string.Empty);

            using var stream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream);

            const short channels = 1;
            const short bitsPerSample = 16;
            var byteRate = sampleRate * channels * bitsPerSample / 8;
            var blockAlign = (short)(channels * bitsPerSample / 8);
            var dataSize = samples.Length * blockAlign;

            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);

            for (var i = 0; i < samples.Length; i++)
            {
                var sample = Mathf.Clamp(samples[i], -1f, 1f);
                writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
            }
        }

        private static object FindGroupByName(object root, string name, Type groupType)
        {
            if (root == null)
            {
                return null;
            }

            var nameProp = groupType.GetProperty("name", BindingFlags.Instance | BindingFlags.Public);
            var currentName = (string)nameProp.GetValue(root);
            if (string.Equals(currentName, name, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            var childrenProp = groupType.GetProperty("children", BindingFlags.Instance | BindingFlags.Public);
            var children = (Array)childrenProp.GetValue(root);
            if (children == null)
            {
                return null;
            }

            for (var i = 0; i < children.Length; i++)
            {
                var child = children.GetValue(i);
                var found = FindGroupByName(child, name, groupType);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static object LoadSubAssetByType(string assetPath, Type type)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < allAssets.Length; i++)
            {
                if (allAssets[i] != null && type.IsInstanceOfType(allAssets[i]))
                {
                    return allAssets[i];
                }
            }

            return null;
        }

        private static Type GetEditorType(string fullName)
        {
            var type = typeof(Editor).Assembly.GetType(fullName);
            if (type == null)
            {
                throw new TypeLoadException($"Unable to find editor type: {fullName}");
            }

            return type;
        }
    }
}
#endif
