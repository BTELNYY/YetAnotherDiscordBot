using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YetAnotherDiscordBot
{
    public class Utility
    {
        public static T FixNullFields<T>(T input, out bool anyFixed)
        {
            if(input is null)
            {
                throw new ArgumentNullException();
            }
            T copy = input;
            bool anyFixedInter = false;
            FieldInfo[] prop = copy.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            Log.GlobalDebug(prop.Length.ToString());
            foreach (FieldInfo propInfo in prop)
            {
                if (propInfo == null)
                {
                    Log.GlobalError("Null field.");
                    continue;
                }
                FieldInfo? info = copy.GetType().GetField(propInfo.Name);
                if (info == null)
                {
                    Log.GlobalError("Failed to get field by name. Name: " + propInfo.Name);
                    continue;
                }
                if (info.GetValue(copy) == null)
                {
                    object? value = Activator.CreateInstance(propInfo.FieldType);
                    if (value is null)
                    {
                        Log.GlobalError("Can't create value instance for property. Name: " + propInfo.Name);
                        continue;
                    }
                    info.SetValue(info, value);
                    anyFixedInter = true;
                }
            }
            anyFixed = anyFixedInter;
            return copy;
        }
    }
}
