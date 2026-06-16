using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace TaskTwig.Core;

public class MappedObservableList<TBase, TMapped> : ReadOnlyObservableCollection<TMapped> where TBase : INotifyPropertyChanged
{
    private ObservableCollection<TBase> _baseCollection;
    private Func<TBase, TMapped> _mapper;
    private string[]? _propertyNames;

    public MappedObservableList(ObservableCollection<TBase> baseCollection, Func<TBase, TMapped> mapper) : base([])
    {
        _baseCollection = baseCollection;
        _mapper = mapper;
        _ResetItems();
        
        _baseCollection.CollectionChanged += _HandleCollectionChanged;
        
        Console.WriteLine($"MappedObservableList: {Items.Count}");
    }

    private void _ResetItems()
    {
        Items.Clear();
        for (int i = 0; i < _baseCollection.Count; i++)
            _AddItem(i, _baseCollection[i]);
    }

    private void _AddItem(int index, TBase item)
    {
        item.PropertyChanged += _HandleItemPropertyChanged;
        Items.Insert(index, _mapper(item));
    }

    private void _RemoveItem(int index, TBase item)
    {
        item.PropertyChanged -= _HandleItemPropertyChanged;
        Items.RemoveAt(index);
    }

    private void _ReplaceItem(int index, TBase oldItem, TBase newItem)
    {
        oldItem.PropertyChanged -= _HandleItemPropertyChanged;
        newItem.PropertyChanged += _HandleItemPropertyChanged;
        Items[index] = _mapper(newItem);
    }

    private void _HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (args.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Move)
        {
            for (int i = 0; i < args.OldItems.Count; i++)
                _RemoveItem(args.OldStartingIndex + i, (TBase)args.OldItems[i]);
        }
        if (args.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Move)
        {
            for (int i = 0; i < args.NewItems.Count; i++)
                _AddItem(args.NewStartingIndex + i, (TBase)args.NewItems[i]);
        }

        if (args.Action is NotifyCollectionChangedAction.Replace)
        {
            for (int i = 0; i < args.OldItems.Count; i++)
                _ReplaceItem(args.OldStartingIndex + i, (TBase)args.OldItems[i], (TBase)args.NewItems[i]);
        }

        if (args.Action is NotifyCollectionChangedAction.Reset)
        {
            _ResetItems();
        }
        
        Console.WriteLine($"MappedObservableList._HandleCollectionChanged(): {args.Action}: {Items.Count}");
    }

    private void _HandleItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        Console.WriteLine($"_HandleItemPropertyChanged: {args.PropertyName}");
        if (sender is TBase item && (_propertyNames is null || _propertyNames.Contains(args.PropertyName)))
        {
            int index = _baseCollection.IndexOf(item);
            Items[index] = _mapper(item);
        }
        
        Console.WriteLine($"MappedObservableList._HandleItemPropertyChanged(): {string.Join(", ", _propertyNames ?? ["None"])}:{args.PropertyName}: {Items.Count}");
    }
}