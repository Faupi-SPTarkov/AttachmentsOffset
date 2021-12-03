using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;
using Notifier = GClass1368;
using EFT.UI.DragAndDrop;
using Newtonsoft.Json;
using AttachmentsOffset.Patches;
using GPUInstancer;
using Comfort.Common;
using System;
using AttachmentsOffset.Utils;
using UnityEngine.SceneManagement;
using System.Reflection;
using Aki.Common.Utils;
using Newtonsoft.Json.Linq;
using UnityEngine.Assertions;

namespace AttachmentsOffset
{
    public class AttachmentsOffset
    {
        private static ModConfiguration _modConfig;
        public static ModConfiguration ModConfig
        {
            private set
            {
                _modConfig = value;
            }
            get
            {
                if (_modConfig == null)
                    LoadConfig();
                return _modConfig;
            }
        }

        private static ModInformation _modInfo;
        public static ModInformation ModInfo
        {
            private set
            {
                _modInfo = value;
            }
            get
            {
                if (_modInfo == null)
                    LoadModInfo();
                return _modInfo;
            }
        }

        private static Transform _gameObjectStorage;
        public static Transform GameObjectStorage
        {
            get
            {
                if(_gameObjectStorage == null)
                {
                    GameObject storage = new GameObject("AttachmentsOffset Storage");
                    UnityEngine.Object.DontDestroyOnLoad(storage);
                    storage.SetActive(false);
                    _gameObjectStorage = storage.transform;
                }

                return _gameObjectStorage;
            }
        }

        private static void Main()
        {
            Patcher.PatchAll();
        }

        private static void LoadConfig()
        {
            string path = VFS.Combine(ModInfo.path, "config.json");
            Debug.LogError($"Loading config from '{path}'");

            string configJson = null;
            try
            {
                configJson = VFS.ReadFile(path, null);
                ModConfig = JsonConvert.DeserializeObject<ModConfiguration>(configJson);
            }
            catch (Exception configReadingException)
            {
                string loadErrorMsg = $"[{ModInfo.name}] Could not load config!";
                Notifier.DisplayWarningNotification(loadErrorMsg);
                Debug.LogError(loadErrorMsg);
                Debug.LogError(configReadingException);

                // Recreate/fill config file
                JObject defaultConfig = JObject.Parse(JsonConvert.SerializeObject(new ModConfiguration()));
                if (configJson != null)
                {
                    void LogBadJsonFormatting(Exception exception = null)
                    {
                        string mergeLoadedConfigMsg = $"[{ModInfo.name}] There was a {(exception != null ? "fatal " : string.Empty)}problem with loading config as JSON, there's likely a bad typo.";
                        Notifier.DisplayWarningNotification(mergeLoadedConfigMsg);
                        Debug.LogError(mergeLoadedConfigMsg);
                        Debug.LogError(exception);
                        Debug.LogError("Restoring config defaults.");
                    }

                    if (configJson.IsValidJson())
                    {
                        try
                        {
                            // If the file loaded at least partially, overwrite the defaults with it
                            JObject loadedConfigPart = JObject.Parse(configJson);
                            defaultConfig.Merge(loadedConfigPart, new JsonMergeSettings
                            {
                                // union array values together to avoid duplicates
                                MergeArrayHandling = MergeArrayHandling.Union
                            });
                        }
                        catch (Exception mergeLoadedConfigException)
                        {
                            LogBadJsonFormatting(mergeLoadedConfigException);
                        }
                    }
                    else
                    {
                        LogBadJsonFormatting();
                    }
                }

                string fixedConfigJson = defaultConfig.ToString();
                try
                {
                    ModConfig = JsonConvert.DeserializeObject<ModConfiguration>(fixedConfigJson);
                }
                catch (Exception configReconstructionException)
                {
                    string fillErrorMsg = $"[{ModInfo.name}] Could not restore config values!";
                    Notifier.DisplayWarningNotification(fillErrorMsg);
                    Debug.LogError(loadErrorMsg);
                    Debug.LogError("Yell at Faupi with the logs.");

                    throw configReconstructionException;   // Throw because at this point we can't really continue
                }
            }
            string completeConfigJson = JsonConvert.SerializeObject(ModConfig, Formatting.Indented);
            if(completeConfigJson != configJson)    // There's a difference between the config file and actual config
            {
                try
                {
                    Debug.LogError($"[{ModInfo.name}] Writing fixed config...");
                    VFS.WriteFile(path, completeConfigJson.ToString());
                }
                catch
                {
                    Debug.LogError($"[{ModInfo.name}] There was a problem with writing the config.");
                    throw;
                }
            }
        }

        private static void LoadModInfo()
        {
            JObject response = JObject.Parse(Aki.SinglePlayer.Utils.RequestHandler.GetJson($"/AttachmentOffset/GetInfo"));
            try
            {
                Assert.IsTrue(response.Value<int>("status") == 0);
                //ModInfo = JsonConvert.DeserializeObject<ModInformation>(JsonConvert.SerializeObject(response["data"]));
                ModInfo = response["data"].ToObject<ModInformation>();
            }
            catch(Exception getModInfoException)
            {
                string errMsg = $"[{typeof(AttachmentsOffset)}] Package.json couldn't be found! Make sure you've installed the mod on the server as well!";
                Notifier.DisplayWarningNotification(errMsg);
                Debug.LogError(errMsg);
                throw getModInfoException;
            }

        }

        public static void SetRotationalOffset(Item item, Vector3 offset)
        {
            if (item == null || offset == null)
                return;

            List<Transform> itemViews = ItemViewCache.Get(item);
            if (itemViews == null || itemViews.Count == 0) return;
            foreach (Transform itemView in itemViews)
            {
                SetRotationalOffset(itemView, offset);
            }
        }
        public static void SetRotationalOffset(Transform itemView, Vector3 offset)
        {
            if (itemView == null || offset == null)
                return;

            ModPlacer modPlacer = itemView.GetComponent<ModPlacer>();
            Vector3 oldRot = itemView.localRotation.eulerAngles;
            Vector3 vanillaRot = modPlacer != null ? modPlacer.ModRotation : new Vector3(90,0,0);
            Quaternion newRot = Quaternion.Euler(vanillaRot) * Quaternion.Euler(offset);
            itemView.localRotation = newRot;
        }

        public static void SetOffset(Item item, Vector3 offset)
        {
            if (item == null || offset == null)
                return;

            List<Transform> itemViews = ItemViewCache.Get(item);
            if (itemViews == null || itemViews.Count == 0) return;
            foreach(Transform itemView in itemViews)
            {
                SetOffset(itemView, offset);
            }
        }
        public static void SetOffset(Transform itemView, Vector3 offset)
        {
            if (itemView == null || offset == null)
                return;

            ModPlacer modPlacer = itemView.GetComponent<ModPlacer>();
            Vector3 oldPos = itemView.localPosition;
            Vector3 vanillaPos = modPlacer != null ? modPlacer.ModPosition : Vector3.zero;
            Vector3 newPos = vanillaPos + offset;
            itemView.localPosition = newPos;
        }

        public static Dictionary<string, Vector3Json> GetDataFromServer(string id)
        {
            string json = Aki.SinglePlayer.Utils.RequestHandler.GetJson($"/AttachmentOffset/GetOffset/{id}");
            Dictionary<string,object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            //Notifier.DisplayMessageNotification($"Received data: \"{json}\"");

            int status = int.Parse(data["status"].ToString());
            if(status == 0 && data["data"] != null)
            {
                try
                {
                    Dictionary<string, Vector3Json> offsets = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(data["data"]), typeof(Dictionary<string, Vector3Json>)) as Dictionary<string, Vector3Json>;
                    return offsets;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static void SendDataToServer(string id, Vector3 position, Vector3 rotation)
        {
            Dictionary<string, Vector3Json> data = new Dictionary<string, Vector3Json>()
            {
                {
                    "pos",
                    position.ToJsonVector()
                },
                {
                    "rot",
                    rotation.ToJsonVector()
                }
            };

            string serializedData = JsonConvert.SerializeObject(data, Formatting.None);
            Aki.SinglePlayer.Utils.RequestHandler.PostJson($"/AttachmentOffset/SetOffset/{id}", serializedData);
            //Notifier.DisplayMessageNotification($"Sent data: \"{serializedData}\"");
        }

        public static bool IsOffsetable(Mod item)
        {
            EModClass type = ItemViewFactory.GetModClass(item.GetType());
            EModSubclass subtype = ItemViewFactory.GetModSubclass(item.GetType());

            if (type != EModClass.Gear && type != EModClass.Functional) return false; // Could add master for vital parts (at least I think that's what it's called)
            //if (subtype != EModSubclass.SightMod) return false;

            return true;
        }
    }

    
}