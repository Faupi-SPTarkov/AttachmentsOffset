using AttachmentsOffset.Utils;
using Comfort.Common;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace AttachmentsOffset
{
	public sealed class OffsetableComponentOperation : GClass1825
	{
		public OffsetableComponentOperation(ushort id, GClass1765 controller, OffsetableComponent offset, Vector3Json position, Vector3Json rotation) : base(id, controller)
		{
			this.Offset = offset;
			this.Position = position;
			this.Rotation = rotation;
		}

		internal void vmethod_0(Callback callback, bool requiresExternalFinalization = false)
		{
			this.gstruct248_0 = this.Offset.Set(this.Position, this.Rotation);
			//typeof(GClass1855).GetField("commandStatus_0", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this, CommandStatus.Succeed);
			this.commandStatus_0 = CommandStatus.Succeed;
			callback(null);
			if (!requiresExternalFinalization)
			{
				this.Dispose();
			}
		}

		public override void Dispose()
		{
			if (this.gstruct248_0.Error == null)
			{
				OffsetableComponentTransaction value = this.gstruct248_0.Value;
				if (value == null)
				{
					return;
				}
				value.RaiseEvents(this.Controller, CommandStatus.Succeed);
			}
		}

        public readonly OffsetableComponent Offset;

		public readonly Vector3Json Position;

		public readonly Vector3Json Rotation;

		private GStruct248<OffsetableComponentTransaction> gstruct248_0;
	}
}
