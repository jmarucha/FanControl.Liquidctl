using System;
using System.Linq;

namespace FanControl.Liquidctl;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public class OptionSwitch : Attribute
{
    public string Switch { get; set; }

    public OptionSwitch(string option)
    {
        Switch = option;
    }
}

public class OptionType : Attribute
{
    public Type Type { get; set; }

    public OptionType(Type type)
    {
        Type = type;
    }
}

public static class EnumExtensionMethods
{
    public static string GetSwitch(this Enum value)
    {
        return value.GetType().GetField(value.ToString())
            ?.GetCustomAttributes(typeof(OptionSwitch), false)
            .Cast<OptionSwitch>()
            .Single().Switch;
    }

    public static bool IsNumeric(this Enum value)
    {
        var encoded = value.GetType().GetField(value.ToString())
            ?.GetCustomAttributes(typeof(OptionType), false)
            .Cast<OptionType>()
            .Single().Type;
        return encoded == typeof(long) || encoded == typeof(int) || encoded == typeof(byte) ||
               encoded == typeof(sbyte) || encoded == typeof(uint) || encoded == typeof(ushort) ||
               encoded == typeof(ulong) || encoded == typeof(float) || encoded == typeof(double) ||
               encoded == typeof(decimal);
    }
}

public enum Option
{
    [OptionSwitch("--serial"), OptionType(typeof(string))]
    SerialId,

    [OptionSwitch("--address"), OptionType(typeof(string))]
    Address,

    [OptionSwitch("-m"), OptionType(typeof(string))]
    Description,

    [OptionSwitch("--product"), OptionType(typeof(int))]
    Product,

    [OptionSwitch("--vendor"), OptionType(typeof(int))]
    Vendor,

    [OptionSwitch("--release"), OptionType(typeof(long))]
    Release
}