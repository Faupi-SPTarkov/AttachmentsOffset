using AttachmentsOffset.Utils;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttachmentsOffset
{
	public class OffsetableComponentTransaction : GInterface83, GInterface84<OffsetableComponentTransaction>, GInterface205
	{
		// Token: 0x06008EA0 RID: 36512 RVA: 0x000F7EDC File Offset: 0x000F60DC
		internal OffsetableComponentTransaction(OffsetableComponent component, Vector3Json position, Vector3Json rotation)
		{
			
		}

		// Token: 0x06008EA1 RID: 36513 RVA: 0x000A4155 File Offset: 0x000A2355
		public bool CanExecute(GClass1765 itemController)
		{
			return true;
		}

		public GStruct248<OffsetableComponentTransaction> Execute()
		{
			return this.offset.Set(this.position, this.rotation);
		}

		public void RaiseEvents(GClass1765 controller, CommandStatus status)
		{
			this.offset.Item.RaiseRefreshEvent(false, false);
		}

		public GStruct248<GInterface205> Replay()
		{
			return this.Execute().Cast<GInterface205>();
		}

		public void RollBack()
		{
			this.offset.Set(offset.OldPosition.ToJsonVector(), offset.OldRotation.ToJsonVector());
		}

		public OffsetableComponent offset;
		public Vector3Json position;
		public Vector3Json rotation;
	}
}
