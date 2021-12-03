using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;
using Notifier = GClass1368;
using AttachmentsOffset.Utils;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace AttachmentsOffset
{
    public class OffsetableComponentDescriptor : GClass977
    {
        public Vector3Json Position;
        public Vector3Json Rotation;
    }

    public class OffsetableComponent : TagComponent
    {
		public OffsetableComponent(Item item) : base(item)
		{
            loaded = false;
		}

        public void InitIfNeeded()
        {
            return;
            //if (loaded == false)
            //{
            //    Dictionary<string, Vector3Json> fromServer = AttachmentsOffset.GetDataFromServer(Item.Id);
            //    try
            //    {
            //        if (fromServer != null)
            //        {
            //            Vector3 pos = fromServer["pos"].FromJsonVector();
            //            Vector3 rot = fromServer["rot"].FromJsonVector();

            //            if (pos != null)
            //                Position = pos;
            //            else
            //                Position = Vector3.zero;

            //            if (rot != null)
            //                Rotation = rot;
            //            else
            //                Rotation = Vector3.zero;
            //        }
            //        else
            //        {
            //            Position = Vector3.zero;
            //            Rotation = Vector3.zero;
            //        }
            //    }
            //    catch
            //    {
            //        Position = Vector3.zero;
            //        Rotation = Vector3.zero;
            //    }

            //    OldPosition = Position;
            //    OldRotation = Rotation;

            //    loaded = true;
            //}
        }

        public GStruct248<OffsetableComponentTransaction> Set(Vector3Json position, Vector3Json rotation)
        {
            return new OffsetableComponentTransaction(this, position, rotation);
        }

        public void Load()
        {
            var data = Deserialize();
            if(data != null)
            {
                Position = data["pos"].FromJsonVector();
                Rotation = data["rot"].FromJsonVector();
            }
        }
        public void Save()
        {
            Item item = this.Item;
            AttachmentsOffset.SetOffset(item, Position);
            AttachmentsOffset.SetRotationalOffset(item, Rotation);

            //this.Set(Serialize(), 0, false);

            //AttachmentsOffset.SendDataToServer(this.Item.Id, Position, Rotation);

            OldPosition = Position;
            OldRotation = Rotation;
        }

        public string Serialize()
        {
            Dictionary<string, Vector3Json> data = new Dictionary<string, Vector3Json>()
            {
                {
                    "pos",
                    Position.ToJsonVector()
                },
                {
                    "rot",
                    Rotation.ToJsonVector()
                }
            };

            return JsonConvert.SerializeObject(data, Formatting.None).Replace("\"", "[slash]");
        }
        public Dictionary<string, Vector3Json> Deserialize()
        {
            try
            {
                Dictionary<string, Vector3Json> offsets = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(this.Name.Replace("[slash]", "\"")), typeof(Dictionary<string, Vector3Json>)) as Dictionary<string, Vector3Json>;
                return offsets;
            }
            catch(Exception ex)
            {
                Debug.LogError(ex);
                return null;
            }
        }

        public void Revert()
        {
            Position = OldPosition;
            Rotation = OldRotation;
        }

        public void Reset()
        {
            Position = Vector3.zero;
            Rotation = Vector3.zero;
        }

        public bool loaded = false;

        Vector3 _oldPosition;
        Vector3 _oldRotation;
        public Vector3 OldPosition
        {
            get => _oldPosition;
            set
            {
                if(value != _oldPosition)
                {
                    _oldPosition = value;
                }
            }
        }
        public Vector3 OldRotation
        {
            get => _oldRotation;
            set
            {
                if (value != _oldRotation)
                {
                    _oldRotation = value;
                }
            }
        }

        private Vector3 _position = Vector3.zero;
        [GAttribute20]
        public Vector3 Position
        {
            get => _position;
            set
            {
                if(value != _position)
                {
                    _position = value;
                    AttachmentsOffset.SetOffset(this.Item, value);
                }
            }
        }

        public float PosX
        {
            get => Position.x;
            set => Position = new Vector3(value, Position.y, Position.z);
        }
        public float PosY
        {
            get => Position.y;
            set => Position = new Vector3(Position.x, value, Position.z);
        }
        public float PosZ
        {
            get => Position.z;
            set => Position = new Vector3(Position.x, Position.y, value);
        }

        private Vector3 _rotation = Vector3.zero;
        [GAttribute20]
        public Vector3 Rotation
        {
            get => _rotation;
            set
            {
                if (value != _rotation)
                {
                    _rotation = value;
                    AttachmentsOffset.SetRotationalOffset(this.Item, value);
                }
            }
        }

        public float RotX
        {
            get => Rotation.x;
            set => Rotation = new Vector3(value, Rotation.y, Rotation.z);
        }
        public float RotY
        {
            get => Rotation.y;
            set => Rotation = new Vector3(Rotation.x, value, Rotation.z);
        }
        public float RotZ
        {
            get => Rotation.z;
            set => Rotation = new Vector3(Rotation.x, Rotation.y, value);
        }

        [GAttribute20]
        public string TestField = "Stop digging in this";
    }
}
