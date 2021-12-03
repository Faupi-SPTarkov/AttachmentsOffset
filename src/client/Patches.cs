using Aki.Common.Utils.Patching;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Settings;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using AttachmentVisual = GClass418.GClass419;
using Newtonsoft.Json;
using AttachmentsOffset.Utils;
using System.IO;
using Notifier = GClass1368;

namespace AttachmentsOffset.Patches
{
    class Patcher
    {
        public static void PatchAll()
        {
            PatcherUtil.Patch<ModViewAddedPatch>();
            PatcherUtil.Patch<ModViewRemovedPatch>();
            PatcherUtil.Patch<ModConstructorAddTagComponentPatch>();
            PatcherUtil.Patch<ModItemInteractionButtonsPatch>();

            // Serialization
            PatcherUtil.Patch<SerializationPatches.SerializeComponentPatch>();
            PatcherUtil.Patch<SerializationPatches.DeserializeComponentPatch>(); 
            PatcherUtil.Patch<SerializationPatches.WritePolymorphPatch>();
            PatcherUtil.Patch<SerializationPatches.ReadPolymorphPatch>();
            /*PatcherUtil.Patch<SerializationPatches.TestDescriptorPatch>();*/  // TESTING

            // ItemUiContext
            PatcherUtil.Patch<ItemUiContextPatches.ItemUiContextEditTagPatch>();
            PatcherUtil.Patch<ItemUiContextPatches.ItemUiContextCloseAllWindowsPatch>();
            PatcherUtil.Patch<ItemUiContextPatches.ItemUiContextResetTagPatch>();
        }
    }

    class ModItemInteractionButtonsPatch : GenericPatch<ModItemInteractionButtonsPatch>
    {
        public ModItemInteractionButtonsPatch() : base(null, "PatchPostfix", null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetProperty("ItemInteractionButtons", BindingFlags.Public | BindingFlags.Instance).GetGetMethod();
        }

        private static void PatchPostfix(ref List<EItemInfoButton> __result, ref Mod __instance)
        {
            // Adds interaction buttons for the "tag" component
            OffsetableComponent offsetableComponent = __instance.GetItemComponent<OffsetableComponent>();
            if (offsetableComponent != null)
            {
                __result.Add(EItemInfoButton.Tag);

                if (offsetableComponent.Rotation != Vector3.zero || offsetableComponent.Position != Vector3.zero)
                {
                    __result.Add(EItemInfoButton.ResetTag);
                }
            }
        }
    }

    class ModConstructorAddTagComponentPatch : GenericPatch<ModConstructorAddTagComponentPatch>
    {
        public ModConstructorAddTagComponentPatch() : base(null, "PatchPostfix", null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(Mod).GetConstructor(new Type[] { typeof(string), typeof(ModTemplate) });
        }

        private static void PatchPostfix(ref Mod __instance, string id, ModTemplate template)
        {
            // Adds the Offsetable component to mods that fit the requirements set in IsOffsetable()
            if (!AttachmentsOffset.IsOffsetable(__instance)) return;

            FieldInfo componentsField = typeof(Item).GetField("Components", BindingFlags.NonPublic | BindingFlags.Instance);
            List<IItemComponent> components = (List<IItemComponent>)componentsField.GetValue(__instance);
            OffsetableComponent component = __instance.GetItemComponent<OffsetableComponent>();

            if (component == null)
            {
                components.Add(component = new OffsetableComponent(__instance));

                componentsField.SetValue(__instance, components);
            }
        }
    }

    class ModViewAddedPatch : GenericPatch<ModViewAddedPatch>
    {
        public ModViewAddedPatch() : base(null, "PatchPostfix", null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AttachmentVisual).GetMethod("InsertItem", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void PatchPostfix(ref AttachmentVisual __instance, Item item, GameObject itemView)
        {
            // Loads and applies the offset to an attachment when its model is attached to a weapon
            if (item == null) return;
            ItemViewCache.Add(item, itemView.transform);

            OffsetableComponent component = item.GetItemComponent<OffsetableComponent>();
            if (component == null) return;

            component.InitIfNeeded();
            AttachmentsOffset.SetOffset(item, component.Position);
            AttachmentsOffset.SetRotationalOffset(item, component.Rotation);
        }
    }

    class ModViewRemovedPatch : GenericPatch<ModViewRemovedPatch>
    {
        public ModViewRemovedPatch() : base("PatchPrefix", null, null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(AttachmentVisual).GetMethod("RemoveItem", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void PatchPrefix(ref AttachmentVisual __instance)
        {
            // Clears the itemView entry from the cache
            ItemViewCache.Remove(__instance.Item, __instance.ItemView);
        }
    }

    public class ItemUiContextPatches
    {
        static List<KeyValuePair<Item, EditOffsetWindow>> openedEditOffsetWindows = new List<KeyValuePair<Item, EditOffsetWindow>>();
#pragma warning disable 0649
        static EditOffsetWindow previousOffsetWindow;
#pragma warning restore 0649
        static EditOffsetWindow editOffsetWindowTemplate;

        public static EditOffsetWindow GetEditOffsetWindowTemplate(ItemUiContext __instance)
        {
            if (editOffsetWindowTemplate != null)
                return editOffsetWindowTemplate;

            EditTagWindow original = typeof(ItemUiContext).GetField("_editTagWindowTemplate", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance) as EditTagWindow;

            EditTagWindow @new = GameObject.Instantiate<EditTagWindow>(original);
            GameObject newWindow = @new.gameObject;
            @new.transform.parent = AttachmentsOffset.GameObjectStorage;
            newWindow.name = "Edit offset window template";

            EditOffsetWindow result = newWindow.AddComponent<EditOffsetWindow>();

            result.CopyComponentValues<Window>(@new);

            result.saveButton = typeof(EditTagWindow).GetField("_saveButtonSpawner", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(@new) as DefaultUIButton;

            GameObject.DestroyImmediate(@new);
            result.SetupWindow();
            editOffsetWindowTemplate = result;

            return editOffsetWindowTemplate;
        }

        public class ItemUiContextEditTagPatch : GenericPatch<ItemUiContextEditTagPatch>
        {
            public ItemUiContextEditTagPatch() : base("PatchPrefix", null, null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(ItemUiContext).GetMethod("EditTag", new[] { typeof(GClass1817), typeof(TagComponent) });
            }

            private static bool PatchPrefix(ref ItemUiContext __instance, GClass1817 itemContext, TagComponent tagComponent)
            {
                // Changes the window for EditTag to our custom one
                if (tagComponent is OffsetableComponent)
                {
                    EditOffsetWindowCreatorOrSomething @class = new EditOffsetWindowCreatorOrSomething();
                    @class.offsetComponent = tagComponent as OffsetableComponent;
                    @class.itemUiContext = __instance;

                    if (GetEditOffsetWindowTemplate(__instance) != null)
                    {
                        typeof(ItemUiContext).GetMethod("method_4", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(typeof(EditOffsetWindow))
                            .Invoke(__instance, BindingFlags.Default, null, new object[] { openedEditOffsetWindows, itemContext, GetEditOffsetWindowTemplate(__instance), previousOffsetWindow, new Action<EditOffsetWindow, Action, Action>(@class.Init) }, null);
                        return false;
                    }
                    UnityEngine.Debug.LogWarning("Edit offset window template is null");
                    return false;
                }
                return true;
            }
        }

        public class ItemUiContextResetTagPatch : GenericPatch<ItemUiContextResetTagPatch>
        {
            public ItemUiContextResetTagPatch() : base("PatchPrefix", null, null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(ItemUiContext).GetMethod("ResetTag", new[] { typeof(TagComponent) });
            }

            private static bool PatchPrefix(ref ItemUiContext __instance, TagComponent tagComponent)
            {
                // Changes the window for EditTag to our custom one
                if (tagComponent is OffsetableComponent)
                {
                    OffsetableComponent offsetable = tagComponent as OffsetableComponent;
                    var itemOffsetWindow = openedEditOffsetWindows.Find(kvp => kvp.Key == offsetable.Item);
                    if (itemOffsetWindow.Value != null)
                        itemOffsetWindow.Value.Close();
                    offsetable.Reset();
                    offsetable.Save();
                    return false;
                }
                return true;
            }
        }

        public class ItemUiContextCloseAllWindowsPatch : GenericPatch<ItemUiContextCloseAllWindowsPatch>
        {
            public ItemUiContextCloseAllWindowsPatch() : base(null, "PatchPostfix", null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(ItemUiContext).GetMethod("CloseAllWindows", BindingFlags.Public | BindingFlags.Instance);
            }

            private static void PatchPostfix(ref ItemUiContext __instance)
            {
                // Adds our new window class to the CloseAllWindows() method

                //this.method_5<EditOffsetWindow>(editOffsetWindows);
                typeof(ItemUiContext).GetMethod("method_5", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(typeof(EditOffsetWindow))
                    .Invoke(__instance, BindingFlags.Default, null, new object[] { openedEditOffsetWindows }, null);
                openedEditOffsetWindows.Clear();
            }
        }

        public class EditOffsetWindowCreatorOrSomething{
            internal void Init(EditOffsetWindow window, Action onSelect, Action onClose)
            {
                OffsetableComponent offsetComponent = this.offsetComponent;

                Action<Vector3Json, Vector3Json> save;
                if ((save = this.saveAction) == null)
                {
                    save = (this.saveAction = new Action<Vector3Json,Vector3Json>(this.saveMethod));
                }

                window.Show(offsetComponent, onSelect, onClose, save);
            }

            internal void saveMethod(Vector3Json position, Vector3Json rotation)
            {
                GClass1768 gclass1768 = (typeof(ItemUiContext).GetField("gclass1768_0", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.itemUiContext) as GClass1768);
                gclass1768.TryRunNetworkTransaction<OffsetableComponentTransaction>(this.offsetComponent.Set(position, rotation), null);
            }

            public Action<Vector3Json, Vector3Json> saveAction;
            public ItemUiContext itemUiContext;
            public OffsetableComponent offsetComponent;
        }
    }

    public class SerializationPatches
    {
        private static FieldInfo _typeListField;
        static FieldInfo typeListField
        {
            get
            {
                Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] Getting typeList field");
                if (_typeListField == null)
                    _typeListField = typeof(GClass1899).GetField("list_0", BindingFlags.NonPublic | BindingFlags.Static);
                return _typeListField;
            }
        }

        static List<Type> typeList
        {
            set => typeListField.SetValue(null, value);
            get => typeListField.GetValue(null) as List<Type>;
        }

        public static void WriteOffsetable(BinaryWriter writer, OffsetableComponentDescriptor target)
        {
            Notifier.DisplayMessageNotification("Saving offsetableComponent");
            Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] Saving offsetableComponent");
            writer.Write(target.Position.FromJsonVector());
            writer.Write(target.Rotation.FromJsonVector());
        }

        public static OffsetableComponentDescriptor ReadEFTOffsetableComponentDescriptor(BinaryReader reader)
        {
            return new OffsetableComponentDescriptor
            {
                Position = reader.ReadClassVector3().ToUnityVector3().ToJsonVector(),
                Rotation = reader.ReadClassVector3().ToUnityVector3().ToJsonVector()
            };
        }

        public class SerializeComponentPatch : GenericPatch<SerializeComponentPatch>
        {
            public SerializeComponentPatch() : base("PatchPrefix", null, null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass997).GetMethod("SerializeComponent", BindingFlags.Static | BindingFlags.Public);
            }

            private static bool PatchPrefix(ref GClass977 __result, IItemComponent component)
            {
                Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] SerializeComponent");
                OffsetableComponent offsetableComponent;
                if ((offsetableComponent = (component as OffsetableComponent)) != null)
                {
                    Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] SerializeComponent - See component");
                    __result = new OffsetableComponentDescriptor
                    {
                        Position = offsetableComponent.Position.ToJsonVector(),
                        Rotation = offsetableComponent.Rotation.ToJsonVector()
                    };
                    return false;
                }
                return true;
            }
        }

        public class DeserializeComponentPatch : GenericPatch<DeserializeComponentPatch>
        {
            public DeserializeComponentPatch() : base("PatchPrefix", null, null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass997).GetMethod("DeserializeComponent", BindingFlags.Static | BindingFlags.Public);
            }

            private static bool PatchPrefix(GClass977 descriptor, Item item)
            {
                Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] DeserializeComponent");
                OffsetableComponentDescriptor componentData;
                if ((componentData = (descriptor as OffsetableComponentDescriptor)) != null)
                {
                    Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] DeserializeComponent - See component");
                    OffsetableComponent offsetableComponent = item.GetItemComponent<OffsetableComponent>();
                    offsetableComponent.Position = componentData.Position.FromJsonVector();
                    offsetableComponent.Position = componentData.Rotation.FromJsonVector();
                    return false;
                }
                return true;
            }
        }

        public class WritePolymorphPatch : GenericPatch<WritePolymorphPatch>
        {
            public WritePolymorphPatch() : base("PatchPrefix", null, null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass1899).GetMethod("WritePolymorph", BindingFlags.Static | BindingFlags.Public);
            }

            private static bool PatchPrefix(BinaryWriter writer, object target)
            {
                Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] WritePolymorph");
                if (target is OffsetableComponentDescriptor)
                {
                    if(!typeList.Contains(typeof(OffsetableComponentDescriptor)))
                    {
                        typeList.Add(typeof(OffsetableComponentDescriptor));
                    }
                    int index = typeList.IndexOf(typeof(OffsetableComponentDescriptor));

                    byte value = (byte)index;
                    writer.Write(value);
                    WriteOffsetable(writer, (OffsetableComponentDescriptor)target);
                    Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] WritePolymorph - Wrote");

                    return false;
                }
                return true;
            }
        }
        public class ReadPolymorphPatch : GenericPatch<ReadPolymorphPatch>
        {
            public ReadPolymorphPatch() : base("PatchPrefix", null, null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass1899).GetMethod("ReadPolymorph", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(typeof(OffsetableComponentDescriptor));
            }

            private static bool PatchPrefix(ref OffsetableComponentDescriptor __result, BinaryReader reader)
            {
                Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] ReadPolymorph");
                int val = reader.ReadByte();
                if (val == typeList.IndexOf(typeof(OffsetableComponentDescriptor)))
                {
                    __result = ReadEFTOffsetableComponentDescriptor(reader);
                    Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] ReadPolymorph - Read");
                    return false;
                }
                reader.BaseStream.Seek(-1, SeekOrigin.Current); //NEED TO REVERSE IF WE DIDN'T FIND IT
                return true;
            }
        }

        public class TestDescriptorPatch : GenericPatch<TestDescriptorPatch>
        {
            public TestDescriptorPatch() : base(null, "Patch", null, null) { }

            protected override MethodBase GetTargetMethod()
            {
                return typeof(GClass997).GetMethod("DeserializeInventory", BindingFlags.Static | BindingFlags.Public);
            }

            private static void Patch(GClass967 itemFactory, GClass969 descriptor)
            {
                Debug.LogError($"[{AttachmentsOffset.ModInfo.name}] SHEEEEEEEESH");
            }
        }
    }

    class OperationPatch : GenericPatch<OperationPatch>
    {
        public OperationPatch() : base("PatchPrefix", null, null, null) { }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1765).GetMethod("ConvertOperationResultToOperation", BindingFlags.Instance | BindingFlags.Public);
        }

        private static void PatchPrefix(ref GClass1765 __instance, ref GClass1825 __result, GInterface205 operationResult)
        {
            if (operationResult != null)
            {
                OffsetableComponentTransaction gclass31;
                if ((gclass31 = (operationResult as OffsetableComponentTransaction)) != null)
                {
                    OffsetableComponentTransaction gclass32 = gclass31;
                    ushort num = this.ushort_0;
                    this.ushort_0 = num + 1;
                    return new OffsetableComponentOperation(num, this, gclass32.offset, gclass32.position, gclass32.rotation);
                }
            }
        }
    }
}

