using System.IO.Hashing;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Note : HashableObject
{
    [WatchForHash]
    [ObservableProperty] 
    public required partial string Title { get; set; }
    
    [WatchForHash]
    [ObservableProperty] 
    public partial string Text { get; set; } = "";
    
    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Title));
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Text));
    }
}