#region Copyright

// //=======================================================================================
// // Microsoft Azure Customer Advisory Team  
// //
// // This sample is supplemental to the technical guidance published on the community
// // blog at http://blogs.msdn.com/b/paolos/. 
// // 
// // Author: Paolo Salvatori
// //=======================================================================================
// // Copyright © 2016 Microsoft Corporation. All rights reserved.
// // 
// // THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// // EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// // MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
// //=======================================================================================

#endregion

namespace Microsoft.AzureCat.Samples.ObserverPattern.Framework
{
    #region Using Directives

    using System;
    using System.Data.HashFunction;

    #endregion

    public static class PartitionResolver
    {
        #region Private Constants

        private const uint DefaultSeed = 54325U;
        private const int DefaultPartitionCount = 3;

        #endregion

        #region Public Static Methods

        public static long Resolve(string input)
        {
            return Resolve(input, DefaultPartitionCount);
        }

        public static long Resolve(string input, int partitions)
        {
            MurmurHash3 hasher = new MurmurHash3(32, DefaultSeed);
            byte[] hashRaw = hasher.ComputeHash(input);
            uint fullHash = BitConverter.ToUInt32(hashRaw, 0);
            long hash = fullHash%partitions;
            return hash + 1;
        }

        #endregion
    }
}