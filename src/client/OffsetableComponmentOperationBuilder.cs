using Comfort.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace AttachmentsOffset
{
    public class OffsetableComponmentOperationBuilder
    {
        public static void Init()
        {
            Type baseType = typeof(GClass1825);

            AssemblyName asmName = new AssemblyName(
                string.Format("{0}_{1}", "tmpAsm", Guid.NewGuid().ToString("N"))
            );

            // create in memory assembly only
            AssemblyBuilder asmBuilder =
                AppDomain.CurrentDomain.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            ModuleBuilder moduleBuilder =
                asmBuilder.DefineDynamicModule("core");


            TypeBuilder typeBuilder =
                moduleBuilder.DefineType("OffsetableComponentOperation", TypeAttributes.Public, baseType);

            MethodBuilder setInfo = typeBuilder.DefineMethod("vmethod_0", MethodAttributes.Final | MethodAttributes.NewSlot | MethodAttributes.HideBySig, null, new Type[] { typeof(Callback), typeof(bool)});

            typeBuilder.DefineMethodOverride(setInfo, typeof(GClass1825).GetMethod("vmethod_0", BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

            Type proxy = typeBuilder.CreateType();
        }
    }
}
