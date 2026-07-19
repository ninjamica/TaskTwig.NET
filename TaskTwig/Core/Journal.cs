using System;
using System.IO.Hashing;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Journal : HashableObject
{
    
    [ObservableProperty]
    public required partial DateOnly Date { get; set; }
    
    [ObservableProperty]
    public partial string Text { get; set; } = "";

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        if (!string.IsNullOrWhiteSpace(Text))
        {
            hashAlgorithm.Append(Encoding.UTF8.GetBytes(Text));
            hashAlgorithm.Append(BitConverter.GetBytes(Date.DayNumber));
        }
    }
    
    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(Text);
    }
}