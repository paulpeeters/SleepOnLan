using System.Net.NetworkInformation;

namespace SleepOnLan
{
    public static class MacAddressHelpers
    {
        public static byte[]? StringToMacAddress(string source)
        {
            if (PhysicalAddress.TryParse(source, out PhysicalAddress? physicalAddress))
            {
                return physicalAddress.GetAddressBytes();
            }
            else
            {
                return null;
            }
        }

        public static string MacAddressToString(byte[] source)
        {
            return BitConverter.ToString(source).Replace("-", ":");
        }

        public static byte[] ReverseMacAddress(byte[] source)
        {
            return source.Reverse().ToArray();
        }
    }
}
