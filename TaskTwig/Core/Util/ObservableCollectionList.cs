using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace TaskTwig.Core;

public class ObservableCollectionList<T, TList> : ReadOnlyObservableCollection<T> where T : INotifyPropertyChanged where TList : IList<T>, INotifyCollectionChanged
{
    public ObservableCollection<TList> Sources { get; private init; }
    
    private List<int> _indices = [];
    
    public ObservableCollectionList(IList<TList> sources) : base([])
    {
        Sources = new ObservableCollection<TList>(sources);
        Sources.CollectionChanged += _HandleSourcesChanged;
        _RegisterAllSources();
    }

    private int _GetCountToSource(int sourceIndex)
    {
        if (sourceIndex == 0)
            return 0;
        
        return _indices[sourceIndex - 1] + Sources[sourceIndex - 1].Count;
    }

    private void _RegisterAllSources()
    {
        Items.Clear();
        _indices.Clear();
        foreach (var source in Sources)
        {
            source.CollectionChanged += _HandleBaseCollectionChanged;
            
            _indices.Add(Items.Count);
            foreach (var item in source)
                Items.Add(item);
        }
    }

    private void _RegisterAddedSource(int sourceIndex, TList source)
    {
        source.CollectionChanged += _HandleBaseCollectionChanged;
        
        int indexOffset = _GetCountToSource(sourceIndex);
        _indices.Insert(sourceIndex, indexOffset);
        
        for (int i = sourceIndex + 1; i < _indices.Count; i++)
            _indices[i] += source.Count;
        
        for (int i = 0; i < source.Count; i++)
            Items.Insert(indexOffset + i, source[i]);
    }

    private void _RegisterRemovedSource(int oldSourceIndex, TList source)
    {
        source.CollectionChanged -= _HandleBaseCollectionChanged;
        
        int oldOffset = _indices[oldSourceIndex];
        _indices.RemoveAt(oldSourceIndex);
        
        for (int i = oldSourceIndex; i < _indices.Count; i++)
            _indices[i] -= source.Count;
        
        for (int i = 0; i < source.Count; i++)
            Items.RemoveAt(oldOffset);
    }

    private void _HandleSourcesChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < args.NewItems.Count; i++)
                    _RegisterAddedSource(args.NewStartingIndex + i, (TList)args.NewItems[i]);
                break;
            
            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < args.OldItems.Count; i++)
                    _RegisterRemovedSource(args.OldStartingIndex + i, (TList)args.OldItems[i]);
                break;
            
            case NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move:
                for (int i = 0; i < args.OldItems.Count; i++)
                    _RegisterRemovedSource(args.OldStartingIndex + i, (TList)args.OldItems[i]);
                
                for (int i = 0; i < args.NewItems.Count; i++)
                    _RegisterAddedSource(args.NewStartingIndex + i, (TList)args.NewItems[i]);
                break;
            
            case NotifyCollectionChangedAction.Reset:
                _RegisterAllSources();
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    private void _HandleBaseCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        var sourceList = (TList)sender;
        int sourceIndex = Sources.IndexOf(sourceList);
        int baseIndex = _indices[sourceIndex];
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < args.NewItems.Count; i++)
                    Items.Insert(baseIndex + args.NewStartingIndex + i, (T)args.NewItems[i]);

                for (int i = sourceIndex + 1; i < _indices.Count; i++)
                    _indices[i] += args.NewItems.Count;
                break;
            
            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < args.OldItems.Count; i++)
                    Items.RemoveAt(baseIndex + args.OldStartingIndex + i);
                
                for (int i = sourceIndex + 1; i < _indices.Count; i++)
                    _indices[i] -= args.OldItems.Count;
                break;
            
            case NotifyCollectionChangedAction.Replace:
                for (int i = 0; i < args.NewItems.Count; i++)
                    Items[baseIndex + args.NewStartingIndex + i] = (T)args.NewItems[i];
                break;
            
            case NotifyCollectionChangedAction.Move:
                for (int i = 0; i < args.OldItems.Count; i++)
                    Items.RemoveAt(baseIndex + args.OldStartingIndex + i);
                
                for (int i = 0; i < args.NewItems.Count; i++)
                    Items.Insert(baseIndex + args.NewStartingIndex + i, (T)args.NewItems[i]);
                break;
            
            case NotifyCollectionChangedAction.Reset:
                int maxOldIndex = sourceIndex < _indices.Count - 1 ? _indices[sourceIndex + 1] : Items.Count;
                for (int i = baseIndex; i < maxOldIndex; i++)
                    Items.RemoveAt(baseIndex);
                
                for (int i = 0; i < sourceList.Count; i++)
                    Items.Insert(baseIndex + i, sourceList[i]);
                
                int oldItemCount = maxOldIndex - baseIndex;
                for (int i = sourceIndex + 1; i < _indices.Count; i++)
                    _indices[i] += sourceList.Count - oldItemCount;
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}