using System;
using System.ComponentModel;
using System.IO.Hashing;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public abstract class HashableObject : ObservableObject
{
    private static int _allCacheValid = 0;
    public static bool AllCacheValid
    {
        get => Interlocked.CompareExchange(ref _allCacheValid, 1, 1) == 1;
        set
        {
            if (value) Interlocked.CompareExchange(ref _allCacheValid, 1, 0);
            else Interlocked.CompareExchange(ref _allCacheValid, 0, 1);
        }
    }
    
    private static int _isReadingData = 0;
    public static bool IsReadingData
    {
        get => Interlocked.CompareExchange(ref _isReadingData, 1, 1) == 1;
        set
        {
            if (value)
            {
                Interlocked.CompareExchange(ref _isReadingData, 1, 0);
                SaveTimer.Stop();
            }
            else Interlocked.CompareExchange(ref _isReadingData, 0, 1);
        }
    }
    
    private static readonly DispatcherTimer SaveTimer;

    public static Action? SaveCallback
    {
        get;
        set
        {
            field = value;
            SaveTimer.Stop();
        }
    }
    
    public static void StopSaveTimer() => SaveTimer.Stop();

    static HashableObject()
    {
        SaveTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, Dispatcher.UIThread);
        SaveTimer.IsEnabled = false;
        SaveTimer.Tick += OnSaveTimerElapsed;
    }

    private static void OnSaveTimerElapsed(object? sender, EventArgs e)
    {
        Console.WriteLine($"SaveTimer Tick");
        SaveTimer.Stop();
        SaveCallback?.Invoke();
    }
    
    private byte[]? _cachedHash;
    
    protected void InvalidateCachedHash()
    {
        AllCacheValid = false;
        _cachedHash = null;
    }

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

        if (IsReadingData) 
            return;
        
        SaveTimer.Stop();
        SaveTimer.Start();
            
        Console.WriteLine("Hashable Property Changed");
    }
}