using GHPC;
using GHPC.State;
using GunnerModPC;
using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

[assembly: MelonInfo(typeof(GMPCTextureLoader), "Gunner, Mod, PC! Texture loader", "1.2.0", "Andrix")]
[assembly: MelonPriority(101)]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace GunnerModPC
{
    struct ReplacedTexture
    {
        public byte[] hash;
        public byte[] data;
    }

    public class GMPCTextureLoader : MelonMod
    {
        static MelonPreferences_Category config;
        static MelonPreferences_Entry<bool> reloadChangedTextures;

        static Dictionary<string, ReplacedTexture> loaded;
        static string handlerInstalledForScene;
        const string notInstalled = "TextureLoaderHandlerNotInstalled";

        readonly string folderPath = "Mods/GMPCTextureLoader/";
        readonly string imageExtension = ".png";

        static MD5 md5 = MD5.Create();

        public override void OnInitializeMelon()
        {
            config = MelonPreferences.CreateCategory("GMPCTextureLoaderConfig");
            reloadChangedTextures = config.CreateEntry<bool>("reloadChangedTextures", false);
            reloadChangedTextures.Description =
                "When enabled, hashes texture files to detect changes. Useful for development, but may slow loading.";

            loaded = new Dictionary<string, ReplacedTexture>();
            handlerInstalledForScene = notInstalled;

            Directory.CreateDirectory(folderPath);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
                LoggerInstance.Msg($"Texture loader handler already installed for {handlerInstalledForScene}.");
            }
            else if (sceneName == "LOADER_INITIAL" || sceneName == "LOADER_MENU")
            {
                return;
            }
            else if (sceneName == "MainMenu2_Scene" || sceneName == "t64_menu" || sceneName == "MainMenu2-1_Scene")
            {
                LoggerInstance.Msg($"Patching textures in main menu {sceneName}.");
                MelonCoroutines.Start(LoadTextures(GameState.AppLoaded));
            }
            else
            {
                var status = StateController.RunOrDefer(
                    GameState.GameInitialization,
                    new GameStateEventHandler(LoadTextures));

                LoggerInstance.Msg($"Adding texture loader handler on scene {sceneName}, result: {status}");

                if (status != GameStateInvocationStatus.Fail)
                    handlerInstalledForScene = sceneName;
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
                LoggerInstance.Msg($"Unloading texture loader handler for {handlerInstalledForScene}.");
                handlerInstalledForScene = notInstalled;
            }
        }

        IEnumerator LoadTextures(GameState state)
        {
            LoggerInstance.Msg($"GameState is {state}, loading texture replacements.");

            HashSet<string> replacements = Directory
                .GetFiles(folderPath, "*" + imageExtension)
                .Select(p => Path.GetFileNameWithoutExtension(p))
                .ToHashSet();

            LoggerInstance.Msg($"Found {replacements.Count} replacement textures.");

            Dictionary<string, byte[]> diskDataCache = new Dictionary<string, byte[]>();
            Dictionary<string, byte[]> diskHashCache = new Dictionary<string, byte[]>();

            foreach (string texName in replacements)
            {
                string filePath = Path.Combine(folderPath, texName + imageExtension);

                byte[] data = File.ReadAllBytes(filePath);
                diskDataCache[texName] = data;

                if (reloadChangedTextures.Value)
                    diskHashCache[texName] = ComputeHash(data);
            }

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            LoggerInstance.Msg($"Found {textures.Length} Texture2Ds.");

            HashSet<int> appliedThisRun = new HashSet<int>();

            for (int i = 0; i < textures.Length; ++i)
            {
                Texture2D texture = textures[i];
                string texName = texture.name;

                if (!replacements.Contains(texName))
                    continue;

                int instanceId = texture.GetInstanceID();

                if (appliedThisRun.Contains(instanceId))
                    continue;

                byte[] diskData = diskDataCache[texName];

                if (!loaded.TryGetValue(texName, out ReplacedTexture replacedTexture))
                {
                    LoggerInstance.Msg($"Loading new replacement for \"{texName}\"...");

                    if (!texture.LoadImage(diskData, false))
                    {
                        LoggerInstance.Error($"Failed to upload texture \"{texName}\"!");
                    }
                    else
                    {
                        byte[] hash = reloadChangedTextures.Value ? diskHashCache[texName] : new byte[] { 0 };

                        loaded[texName] = new ReplacedTexture
                        {
                            hash = hash,
                            data = diskData
                        };

                        appliedThisRun.Add(instanceId);
                    }
                }
                else
                {
                    if (reloadChangedTextures.Value)
                    {
                        byte[] newHash = diskHashCache[texName];

                        if (!newHash.SequenceEqual(replacedTexture.hash))
                        {
                            LoggerInstance.Msg($"Detected change in \"{texName}\", reloading...");

                            texture.LoadImage(diskData, false);

                            loaded[texName] = new ReplacedTexture
                            {
                                hash = newHash,
                                data = diskData
                            };
                        }
                        else
                        {
                            texture.LoadImage(replacedTexture.data, false);
                        }
                    }
                    else
                    {
                        texture.LoadImage(replacedTexture.data, false);
                    }

                    appliedThisRun.Add(instanceId);
                }

                // This stops stutter
                if (i % 50 == 0)
                    yield return null;
            }

            LoggerInstance.Msg("Texture loading complete.");
        }

        private byte[] ComputeHash(byte[] data)
        {
            return md5.ComputeHash(data);
        }

        private string HashToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
