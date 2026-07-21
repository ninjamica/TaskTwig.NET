using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TaskTwig.Core.Util;

public class FilteredObservableList<T> : ReadOnlyObservableCollection<T> where T : INotifyPropertyChanged
{
    private readonly ObservableCollection<T> _baseCollection;
    private readonly List<int> _indices = [];
    private readonly Predicate<T> _filter;
    private readonly string[]? _propertyNames;


    public FilteredObservableList(ObservableCollection<T> baseCollection, Predicate<T> filter, params string[]? propertyNames) : base([])
    {
        _baseCollection = baseCollection;
        _filter = filter;
        _propertyNames = propertyNames;
        
        baseCollection.CollectionChanged += _HandleCollectionChanged;
        _ConstructList(_baseCollection);
    }

    private void _ConstructList(IEnumerable<T> items)
    {
        Items.Clear();
        _indices.Clear();
        foreach (var item in items)
        {
            item.PropertyChanged += _HandlePropertyChanged;
            if (_filter.Invoke(item))
            {
                Items.Add(item);
                _indices.Add(Items.Count - 1);
            }
            else
            {
                _indices.Add(-1);
            }
        }
    }

    private void _FilterInItem(int baseIndex)
    {
        int insertIndex = _FindInsertIndex(baseIndex);
        Items.Insert(insertIndex, _baseCollection[baseIndex]);

        _indices[baseIndex] = insertIndex;
        for (int i = baseIndex + 1; i < _indices.Count; i++)
        {
            if (_indices[i] >= 0)
                _indices[i]++;
        }
    }

    private int _FindInsertIndex(int baseIndex)
    {
        for (int i = baseIndex - 1; i >= 0; i--)
        {
            if (_indices[i] >= 0)
                return _indices[i] + 1;
        }

        return 0;
    }

    private void _FilterOutItem(int baseIndex)
    {
        Items.RemoveAt(_indices[baseIndex]);
        _indices[baseIndex] = -1;
        for (int i = baseIndex + 1; i < _indices.Count; i++)
        {
            if (_indices[i] >= 0)
                _indices[i]--;
        }
    }

    private void _AddItem(int baseIndex, T item)
    {
        item.PropertyChanged += _HandlePropertyChanged;
        
        _indices.Insert(baseIndex, -1);
        
        if (_filter.Invoke(item))
            _FilterInItem(baseIndex);
    }

    private void _RemoveItem(int oldBaseIndex, T item)
    {
        item.PropertyChanged -= _HandlePropertyChanged;
        
        if (_indices[oldBaseIndex] >= 0)
            _FilterOutItem(oldBaseIndex);
        
        _indices.RemoveAt(oldBaseIndex);
    }

    private void _ReplaceItem(int baseIndex, T oldItem, T newItem)
    {
        oldItem.PropertyChanged -= _HandlePropertyChanged;
        newItem.PropertyChanged += _HandlePropertyChanged;
        
        bool wasFiltered = _indices[baseIndex] >= 0;
        bool filtered = _filter.Invoke(newItem);
        
        if (wasFiltered && !filtered)
            _FilterOutItem(baseIndex);
        else if (!wasFiltered && filtered)
            _FilterInItem(baseIndex);
    }

    private void _MoveItem(int oldBaseIndex, int newBaseIndex)
    {
        int filteredIndex = _indices[oldBaseIndex];
        if (filteredIndex >= 0)
            _FilterOutItem(oldBaseIndex);
        
        _indices.RemoveAt(oldBaseIndex);
        _indices.Insert(newBaseIndex, filteredIndex);
        
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
        
        // Console.WriteLine($"FilteredObservableList._HandleCollectionChanged(): {args.Action}: {Items.Count}");
    }

    private void _HandlePropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender is T item && (_propertyNames is null ||
                                 args.PropertyName is not null && _propertyNames.Contains(args.PropertyName)))
        {
            int baseIndex = _baseCollection.IndexOf(item);
            if (baseIndex == -1)
                return;
            
            bool filtered = _filter.Invoke(item);
            
            if (_indices[baseIndex] == -1 && filtered)
            {
                _FilterInItem(baseIndex);
            }
            else if (_indices[baseIndex] >= 0 && !filtered)
            {
                _FilterOutItem(baseIndex);
            }
            
            // Console.WriteLine($"FilteredObservableList._HandlePropertyChanged(): {args.PropertyName}({filtered}): {Items.Count}");
        }
        // else
            // Console.WriteLine($"FilteredObservableList._HandlePropertyChanged(): {string.Join(", ", _propertyNames ?? ["None"])}:{args.PropertyName}: {Items.Count}");
    }
}