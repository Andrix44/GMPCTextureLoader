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

[assembly: MelonInfo(typeof(GMPCTextureLoader), "Gunner, Mod, PC! Texture loader", "1.1.2", "Andrix")]
[assembly: MelonPriority(101)]
[assembly: MelonGame("Radian Simulations LLC", "GHPC")]

namespace GunnerModPC
{
    struct ReplacedTexture
    {
        public byte[] hash;
        public byte[] data;      
        public HashSet<int> instances;
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

        public override void OnInitializeMelon()
        {
            config = MelonPreferences.CreateCategory("GMPCTextureLoaderConfig");
            reloadChangedTextures = config.CreateEntry<bool>("reloadChangedTextures", false);
            reloadChangedTextures.Description = "When this is enabled, the mod will hash the texture files to see if they changed when loading a new scene. This is useful when designing textures, but can slow down map loading when a lot of data has to be processed.";

            loaded = new Dictionary<string, ReplacedTexture>();
            handlerInstalledForScene = notInstalled;

            Directory.CreateDirectory(folderPath);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
                LoggerInstance.Msg($"Texture loader handler is already installed for scene {handlerInstalledForScene}, it must be unloaded first.");
            }
            else if (sceneName == "LOADER_INITIAL" || sceneName == "LOADER_MENU") return;
            else if (sceneName == "MainMenu2_Scene" || sceneName == "t64_menu" || sceneName == "MainMenu2-1_Scene")
            {
                LoggerInstance.Msg($"Patching textures in main menu {sceneName}.");
                MelonCoroutines.Start(LoadTextures(GameState.AppLoaded));
            }
            else
            {
                var status = StateController.RunOrDefer(GameState.GameInitialization, new GameStateEventHandler(LoadTextures));
                LoggerInstance.Msg($"Trying to add texture loader event handler on scene {sceneName}, result: {status}");
                if (status != GameStateInvocationStatus.Fail)
                {
                    handlerInstalledForScene = sceneName;
                }
            }
        }

        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (handlerInstalledForScene != notInstalled)
            {
                LoggerInstance.Msg($"Texture loader handler is being unloaded together with scene {handlerInstalledForScene}, a new one can now be installed.");
                handlerInstalledForScene = notInstalled;
            }
        }

        IEnumerator LoadTextures(GameState state)
        {
            LoggerInstance.Msg($"GameState is {state}, loading texture replacements.");

            HashSet<string> replacements = Directory.GetFiles(folderPath, "*" + imageExtension).Select(p => Path.GetFileNameWithoutExtension(p)).ToHashSet();
            LoggerInstance.Msg($"Found {replacements.Count} *{imageExtension} texture replacements in \"{folderPath}\".");

            Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
            LoggerInstance.Msg($"Found {textures.Length} Texture2Ds.");

            for (int i = 0; i < textures.Length; ++i)
            {
                Texture2D texture = textures[i];
                string texName = texture.name;

                if (!replacements.Contains(texName))
                    continue;

                string filePath = folderPath + texName + imageExtension;
                int instanceId = texture.GetInstanceID();

                if (!loaded.TryGetValue(texName, out ReplacedTexture replacedTexture))
                {
                    LoggerInstance.Msg($"Replacement texture for \"{texName}\" has not yet been loaded, reading from file...");
                    byte[] data = File.ReadAllBytes(filePath);

                    if (!texture.LoadImage(data, false))
                    {
                        LoggerInstance.Error($"Failed to upload replacement texture \"{texName}\" into GPU memory!");
                    }
                    else
                    {
                        byte[] hash = ComputeHashIfNeeded(data);
                        LoggerInstance.Msg($"Loaded replacement texture \"{texName}\" (instance {instanceId}){(reloadChangedTextures.Value ? $", hash: {HashToString(hash)}" : "")}.");
                        loaded[texName] = new ReplacedTexture
                        {
                            hash = hash,
                            data = data,
                            instances = new HashSet<int> { instanceId }
                        };
                    }
                }
                else
                {
                    if (reloadChangedTextures.Value)
                    {
                        byte[] diskData = File.ReadAllBytes(filePath);
                        byte[] newHash = ComputeHash(diskData);
                        byte[] oldHash = replacedTexture.hash;

                        if (!newHash.SequenceEqual(oldHash))
                        {
                            LoggerInstance.Msg($"Hash changed for \"{texName}\"! Old: {HashToString(oldHash)}, new: {HashToString(newHash)}. Reloading...");
                            texture.LoadImage(diskData, false);
                            loaded[texName] = new ReplacedTexture
                            {
                                hash = newHash,
                                data = diskData,
                                instances = new HashSet<int> { instanceId }
                            };
                        }
                        else if (!replacedTexture.instances.Contains(instanceId))
                        {
                            LoggerInstance.Msg($"Applying cached replacement to new instance of \"{texName}\" (instance {instanceId})...");
                            texture.LoadImage(replacedTexture.data, false);
                            replacedTexture.instances.Add(instanceId);
                            loaded[texName] = replacedTexture;
                        }
                        else
                        {
                            LoggerInstance.Msg($"Texture \"{texName}\" is unchanged and instance {instanceId} has already been replaced.");
                        }
                    }
                    else if (!replacedTexture.instances.Contains(instanceId))
                    {
                        LoggerInstance.Msg($"Applying cached replacement to new instance of \"{texName}\" (instance {instanceId})...");
                        texture.LoadImage(replacedTexture.data, false);
                        replacedTexture.instances.Add(instanceId);
                        loaded[texName] = replacedTexture;
                    }
                    else
                    {
                        LoggerInstance.Msg($"Replacement texture \"{texName}\" has already been loaded for instance {instanceId}.");
                    }
                }
            }

            yield break;
        }

        private byte[] ComputeHash(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
                return md5.ComputeHash(data);
        }

        private byte[] ComputeHashIfNeeded(byte[] data)
        {
            if (reloadChangedTextures.Value)
                return ComputeHash(data);
            return new byte[] { 0 };
        }

        private string HashToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }
    }
}
