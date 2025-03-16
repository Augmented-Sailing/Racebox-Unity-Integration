using System;
using System.Collections.Generic;
using UnityEngine;

namespace Proxima
{
    public class ProximaStatic : ScriptableObject
    {
        public List<StaticFile> Files;

        [Serializable]
        public struct StaticFile
        {
            public string Path;
            public byte[] Bytes;
            public long LastModified;
        }
    }
}