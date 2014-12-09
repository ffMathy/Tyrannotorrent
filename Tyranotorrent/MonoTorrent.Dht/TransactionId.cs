#if !DISABLE_DHT
using System;
using System.Collections.Generic;
using System.Text;
using MonoTorrent.BEncoding;

namespace MonoTorrent.Dht
{
    internal static class TransactionId
    {
        private static int current = 0;

        private static object lock_object = new object();

        public static BEncodedString NextId()
        {
            lock (lock_object)
            {
                BEncodedString result = new BEncodedString(current.ToString());
                if(current++ > int.MaxValue) // highly unlikely but
                    current = 0;
                return result;
            }
        }
    }
}
#endif