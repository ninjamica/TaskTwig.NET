using System.IO.Hashing;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Note : ObservableObject, IHashable
{
    [ObservableProperty] public required partial string Title { get; set; }
    [ObservableProperty] public partial string Text { get; set; } = "";
    
    public void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Title));
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Text));
    }
}