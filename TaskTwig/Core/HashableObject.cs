using System;
using System.ComponentModel;
using System.IO.Hashing;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using WeakEvent;

namespace TaskTwig.Core;

public class CacheValidChangedEventArgs(bool isAllCacheValid) : EventArgs
{
    public bool IsAllCacheValid { get; } = isAllCacheValid;
}

public abstract class HashableObject : ObservableValidator
{
    private static int _allCacheValid = 0;
    public static bool AllCacheValid
    {
        get => Interlocked.CompareExchange(ref _allCacheValid, 1, 1) == 1;
        set
        {
            var prevVal = value
                ? Interlocked.CompareExchange(ref _allCacheValid, 1, 0)
                : Interlocked.CompareExchange(ref _allCacheValid, 0, 1);
            
            if (value ^ prevVal == 1)
                AllCacheValidChangedEventSource.Raise(null, new CacheValidChangedEventArgs(value));
        }
    }
    
    private static readonly WeakEventSource<CacheValidChangedEventArgs> AllCacheValidChangedEventSource = new();
    public static event EventHandler<CacheValidChangedEventArgs> CacheValidChanged
    {
        add => AllCacheValidChangedEventSource.Subscribe(value);
        remove => AllCacheValidChangedEventSource.Unsubscribe(value);
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

    public static void SetSaveTimer()
    {
        if (IsReadingData) 
            return;
        
        AllCacheValid = false;
        SaveTimer.Stop();
        SaveTimer.Start();

        Console.WriteLine("Hashable Data Changed");
    }

    private static readonly DispatcherTimer FetchTimer;

    public static TimeSpan FetchInterval
    {
        get;
        set
        {
            field = value;
            FetchTimer.Interval = value;
            ResetFetchTimer();
        }
    } = TimeSpan.FromMinutes(5);
    
    public static Action? FetchCallback
    {
        get;
        set
        {
            field = value;
            if (value is null)
            {
                FetchTimer.Stop();
                FetchTimer.IsEnabled = false;
            }
            else
            {
                FetchTimer.IsEnabled = true;
                FetchTimer.Start();
            }
        }
    }
    
    public static void StopFetchTimer() => FetchTimer.Stop();
    public static void ResetFetchTimer()
    {
        FetchTimer.Stop();
        FetchTimer.Start();
    }

    static HashableObject()
    {
        SaveTimer = new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, Dispatcher.UIThread);
        SaveTimer.IsEnabled = false;
        SaveTimer.Tick += OnSaveTimerElapsed;
        
        FetchTimer = new DispatcherTimer(FetchInterval, DispatcherPriority.Background, Dispatcher.UIThread);
        FetchTimer.IsEnabled = false;
        FetchTimer.Tick += OnFetchTimerElapsed;
    }

    private static void OnSaveTimerElapsed(object? sender, EventArgs e)
    {
        Console.WriteLine($"SaveTimer Tick");
        SaveTimer.Stop();
        SaveCallback?.Invoke();
    }
    
    private static void OnFetchTimerElapsed(object? sender, EventArgs e)
    {
        Console.WriteLine($"FetchTimer Tick");
        FetchCallback?.Invoke();
    }
    
    private byte[]? _cachedHash;
    
    protected void InvalidateCachedHash()
    {
        _cachedHash = null;
        SetSaveTimer();
    }

    protected abstract void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm);

    protected virtual void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher) {}

    private byte[] _GetHash(NonCryptographicHashAlgorithm hashAlgorithm)
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
        mainHasher.Append(_GetHash(childHasher));
        AppendHashableChildren(mainHasher, childHasher);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        if (e.PropertyName is not null 
            && GetType().GetProperty(e.PropertyName) is { } property 
            && Attribute.IsDefined(property, typeof(WatchForHashAttribute)))
        {
            InvalidateCachedHash();
        }
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class WatchForHashAttribute : Attribute;