using System;

namespace LivingFSM
{
    public static class EnumExtension
    {
        public static string GetHashStringCode(this Enum @enum) => @enum.GetType().AssemblyQualifiedName + ((IConvertible)@enum).ToInt32(null);

        public static bool IsSame(this Enum @enum, Enum enum2) => @enum.GetType() == enum2.GetType() && @enum.ToInt32() == enum2.ToInt32() && @enum.ToString() == enum2.ToString();

        public static int ToInt32(this Enum @enum) => ((IConvertible)@enum).ToInt32(null);
    }
}