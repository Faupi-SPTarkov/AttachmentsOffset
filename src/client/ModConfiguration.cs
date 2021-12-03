using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttachmentsOffset
{
    public class ModConfiguration
    {
        public bool extendedMode;
        public OffsetConfig positionOffset;
        public OffsetConfig rotationOffset;

        public ModConfiguration(bool extendedMode = false, OffsetConfig positionOffsetRange = null, OffsetConfig rotationOffsetRange = null)
        {
            this.extendedMode = extendedMode;
            this.positionOffset = positionOffsetRange ?? new OffsetConfig(.5f);
            this.rotationOffset = rotationOffsetRange ?? new OffsetConfig(180f);
        }
    }

    public class OffsetConfig
    {
        private float _range;
        public float range
        {
            set => _range = Math.Abs(value);  //We want a positive all the time
            get => _range;
        }

        public OffsetConfig(float limit)
        {
            this.range = limit;
        }
    }
}
