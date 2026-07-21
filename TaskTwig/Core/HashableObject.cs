using System.ComponentModel;
using System.IO.Hashing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public abstract class HashableObject : ObservableObject
{
    private byte[]? _cachedHash;
    
    protected void InvalidateCachedHash() => _cachedHash = null;

    protected abstract void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm);

    protected virtual void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher) {}

    public byte[] GetHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        if (_cachedHash is null)
        {
            AppendHash(hashAlgorithm);
            _cachedHash = hashAlgorithm.GetHashAndReset();
        }
        return _cachedHash;
    }

    public void AppendHashAndChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        mainHasher.Append(GetHash(childHasher));
        AppendHashableChildren(mainHasher, childHasher);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        InvalidateCachedHash();
    }
}