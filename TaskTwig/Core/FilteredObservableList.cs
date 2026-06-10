using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TaskTwig.Core;

public class FilteredObservableList<T> : ReadOnlyObservableCollection<T> where T : INotifyPropertyChanged
{
    
    public List<string>? PropertyNames { get; init; }
    
    private ObservableCollection<T> _baseCollection;
    private List<int> _indexes = [];
    private Predicate<T> _filter;


    public FilteredObservableList(ObservableCollection<T> baseCollection, Predicate<T> filter) : base([])
    {
        _baseCollection = baseCollection;
        _filter = filter;
        
        baseCollection.CollectionChanged += _HandleCollectionChanged;
        _ConstructList(_baseCollection);
    }

    private void _ConstructList(IEnumerable<T> items)
    {
        Items.Clear();
        _indexes.Clear();
        foreach (var item in items)
        {
            item.PropertyChanged += _HandlePropertyChanged;
            if (_filter.Invoke(item))
            {
                Items.Add(item);
                _indexes.Add(Items.Count - 1);
            }
            else
            {
                _indexes.Add(-1);
            }
        }
    }

    private void _FilterInItem(int baseIndex)
    {
        int insertIndex = _FindInsertIndex(baseIndex);
        Items.Insert(insertIndex, _baseCollection[baseIndex]);

        _indexes[baseIndex] = insertIndex;
        for (int i = baseIndex + 1; i < _indexes.Count; i++)
        {
            if (_indexes[i] >= 0)
                _indexes[i]++;
        }
    }

    private int _FindInsertIndex(int baseIndex)
    {
        for (int i = baseIndex - 1; i >= 0; i--)
        {
            if (_indexes[i] >= 0)
                return _indexes[i] + 1;
        }

        return 0;
    }

    private void _FilterOutItem(int baseIndex)
    {
        Items.RemoveAt(_indexes[baseIndex]);
        _indexes[baseIndex] = -1;
        for (int i = baseIndex + 1; i < _indexes.Count; i++)
        {
            if (_indexes[i] >= 0)
                _indexes[i]--;
        }
    }

    private void _AddItem(int baseIndex, T item)
    {
        item.PropertyChanged += _HandlePropertyChanged;
        
        _indexes.Insert(baseIndex, -1);
        
        if (_filter.Invoke(item))
            _FilterInItem(baseIndex);
    }

    private void _RemoveItem(int oldBaseIndex, T item)
    {
        item.PropertyChanged -= _HandlePropertyChanged;
        
        if (_indexes[oldBaseIndex] >= 0)
            _FilterOutItem(oldBaseIndex);
        
        _indexes.RemoveAt(oldBaseIndex);
    }

    private void _ReplaceItem(int baseIndex, T oldItem, T newItem)
    {
        oldItem.PropertyChanged -= _HandlePropertyChanged;
        newItem.PropertyChanged += _HandlePropertyChanged;
        
        bool wasFiltered = _indexes[baseIndex] >= 0;
        bool filtered = _filter.Invoke(newItem);
        
        if (wasFiltered && !filtered)
            _FilterOutItem(baseIndex);
        else if (!wasFiltered && filtered)
            _FilterInItem(baseIndex);
    }

    private void _MoveItem(int oldBaseIndex, int newBaseIndex)
    {
        int filteredIndex = _indexes[oldBaseIndex];
        if (filteredIndex >= 0)
            _FilterOutItem(oldBaseIndex);
        
        _indexes.RemoveAt(oldBaseIndex);
        _indexes.Insert(newBaseIndex, filteredIndex);
        
        if (filteredIndex >= 0)
            _FilterInItem(newBaseIndex);
    }
    
    private void _HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < args.NewItems.Count; i++)
                {
                    _AddItem(args.NewStartingIndex + i, (T)args.NewItems[i]);
                }
                break;
            
            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < args.OldItems.Count; i++)
                {
                    _RemoveItem(args.OldStartingIndex + i, (T)args.OldItems[i]);
                }
                break;
            
            case NotifyCollectionChangedAction.Replace:
                for (int i = 0; i < args.OldItems.Count; i++)
                {
                    _ReplaceItem(args.NewStartingIndex + i, (T)args.OldItems[i], (T)args.NewItems[i]);
                }
                break;
            
            case NotifyCollectionChangedAction.Move:
                for (int i = 0; i < args.OldItems.Count; i++)
                {
                    _MoveItem(args.OldStartingIndex + i, args.NewStartingIndex + i);
                }
                break;
            
            case NotifyCollectionChangedAction.Reset:
                _ConstructList(_baseCollection);
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void _HandlePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is T item && (PropertyNames is null ||
                                 args.PropertyName is not null && PropertyNames.Contains(args.PropertyName)))
        {
            int baseIndex = _baseCollection.IndexOf(item);
            if (baseIndex == -1)
                return;
            
            bool filtered = _filter.Invoke(item);
            
            if (_indexes[baseIndex] == -1 && filtered)
            {
                _FilterInItem(baseIndex);
            }
            else if (_indexes[baseIndex] >= 0 && !filtered)
            {
                _FilterOutItem(baseIndex);
            }
        }
    }
}