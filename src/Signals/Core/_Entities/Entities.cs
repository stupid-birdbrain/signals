using System.Collections.Concurrent;

namespace Signals.Core;

public partial class Entities {
    internal struct UniqueWorldData() {
        public int NextEntityIndex = 0;

        public BitArray<ulong> EntityPresenceMasks;
        public uint[] EntityGenerations;
        public ConcurrentBag<int> FreeEntityIndices;
    }

    internal static UniqueWorldData[] WorldData = [];
}