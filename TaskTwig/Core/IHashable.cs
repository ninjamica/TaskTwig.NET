using System.IO.Hashing;

namespace TaskTwig.Core;

public interface IHashable
{
    public void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm);
}