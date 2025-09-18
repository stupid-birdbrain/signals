using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Signals.Core;

public interface ISignal;

internal sealed class Signals {
    internal class Data<T> where T : struct, ISignal {
        internal static List<T>[] SignalsByWorld = [];
    }

    internal static event Action ClearSignals;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendMessage<T>(uint worldIndex, in T message) where T : struct, ISignal {
        ref var worldSignals = ref Data<T>.SignalsByWorld;
        if (worldIndex >= worldSignals.Length) {
            int oldLength = worldSignals.Length;
            int newLength = (int)BitOperations.RoundUpToPowerOf2(worldIndex + 1);
            Array.Resize(ref worldSignals, newLength);
            
            for (int i = oldLength; i < newLength; i++) {
                worldSignals[i] = new List<T>(); 
            }
        }
        
        Data<T>.SignalsByWorld[worldIndex].Add(message);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> ReadMessages<T>(uint worldIndex) where T : struct, ISignal {
        if (worldIndex >= Data<T>.SignalsByWorld.Length) {
            return ReadOnlySpan<T>.Empty;
        }
        
        var list = Data<T>.SignalsByWorld[worldIndex];
        
        var span = CollectionsMarshal.AsSpan(list); 
        
        list.Clear();
        return span;
    }
}