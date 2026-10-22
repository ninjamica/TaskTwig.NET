using System;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;

namespace TaskTwig.Core.Util;

public class GroupHeaderFlattener<TGroup, TItem, TDestination>
    where TGroup : notnull
    where TItem : notnull
    where TDestination : notnull
{
    private readonly IObservable<IChangeSet<TGroup>> _source;
    private readonly Func<TGroup, IObservableList<TItem>> _itemsSelector;
    private readonly Func<TGroup, TDestination> _headerConverter;
    private readonly Func<TItem, TDestination> _itemConverter;
    
    private readonly SourceList<TDestination> _flatList = new();
    private readonly SourceList<GroupContainer> _groupList = new();
    
    public GroupHeaderFlattener(IObservable<IChangeSet<TGroup>> source, Func<TGroup, IObservableList<TItem>> itemsSelector, Func<TGroup, TDestination> headerConverter, Func<TItem, TDestination> itemConverter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(itemsSelector);
        ArgumentNullException.ThrowIfNull(headerConverter);
        ArgumentNullException.ThrowIfNull(itemConverter);
        
        _source = source;
        _itemsSelector = itemsSelector;
        _headerConverter = headerConverter;
        _itemConverter = itemConverter;
    }

    public IObservable<IChangeSet<TDestination>> Run()
    {
        _source.ForEachChange(OnGroupChange).Subscribe();
        return _flatList.Connect();
    }

    private void OnGroupChange(Change<TGroup> change)
    {
        switch (change.Reason)
        {
            case ListChangeReason.Add:
                AddGroup(change.Item.CurrentIndex, change.Item.Current);
                break;

            case ListChangeReason.AddRange:
                foreach (var groupTuple in change.Range.Index())
                {
                    AddGroup(change.Range.Index + groupTuple.Index, groupTuple.Item);
                }
                break;

            case ListChangeReason.Remove:
                RemoveGroup(change.Item.CurrentIndex);
                break;
            
            case ListChangeReason.RemoveRange:
                for (int i = 0; i < change.Range.Count; i++)
                {
                    RemoveGroup(change.Range.Index);
                }
                break;
            
            case ListChangeReason.Moved:
                MoveGroup(change.Item.PreviousIndex, change.Item.CurrentIndex);
                break;
            
            case ListChangeReason.Replace:
                RemoveGroup(change.Item.CurrentIndex);
                AddGroup(change.Item.CurrentIndex, change.Item.Current);
                break;
            
            case ListChangeReason.Clear:
                ClearGroups();
                break;

            case ListChangeReason.Refresh:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AddGroup(int groupIndex, TGroup group)
    {
        var items = _itemsSelector(group);
        var groupContainer = new GroupContainer(group, items);
        _groupList.Insert(groupIndex, groupContainer);
        
        var startIndex = GetGroupFlatIndex(groupIndex);
        _flatList.Edit(list =>
        {
            list.Insert(startIndex, _headerConverter(group));
            list.AddRange(items.Items.Select(_itemConverter), startIndex + 1);
        });

        groupContainer.Disposable = items.Connect()
            .Transform(_itemConverter)
            .SkipInitial()
            .Select(changes => ShiftIndices(changes, groupContainer))
            .PopulateInto(_flatList);
    }

    private void RemoveGroup(int groupIndex)
    {
        var startIndex = GetGroupFlatIndex(groupIndex);
        var groupContainer = _groupList.Items[groupIndex];
        
        _flatList.RemoveRange(startIndex, groupContainer.Count + 1);
        _groupList.RemoveAt(groupIndex);
        groupContainer.Disposable?.Dispose();
    }

    private void MoveGroup(int originalGroupIndex, int newGroupIndex)
    {
        var originalFlatIndex = GetGroupFlatIndex(originalGroupIndex);
        var groupContainer = _groupList.Items[originalGroupIndex];
        _groupList.Move(originalGroupIndex, newGroupIndex);
        var newFlatIndex = GetGroupFlatIndex(newGroupIndex);
        
        _flatList.Edit(list =>
        {
            var movedItems = list.Skip(originalFlatIndex).Take(groupContainer.Count + 1).ToArray();
            list.RemoveRange(originalFlatIndex, groupContainer.Count + 1);
            list.InsertRange(movedItems, newFlatIndex);
        });
    }

    private void ClearGroups()
    {
        foreach (var group in _groupList.Items)
        {
            group.Disposable?.Dispose();
        }
        
        _groupList.Clear();
        _flatList.Clear();
    }

    private int GetGroupFlatIndex(GroupContainer groupContainer)
    {
        int startIndex = 0;
        foreach (var group in _groupList.Items)
        {
            if (groupContainer == group)
                return startIndex;
            
            startIndex += group.Count + 1;
        }
        
        return startIndex;
    }

    private int GetGroupFlatIndex(int groupIndex)
    {
        int startIndex = 0;
        for (int i = 0; i < groupIndex; i++)
        {
            startIndex += _groupList.Items[i].Count + 1;
        }

        return startIndex;
    }

    private ChangeSet<T> ShiftIndices<T>(IChangeSet<T> changes, GroupContainer group) where T : notnull
    {
        var shiftIndex = GetGroupFlatIndex(group);
        return ShiftIndices(changes, shiftIndex + 1);
    }

    private static ChangeSet<T> ShiftIndices<T>(IChangeSet<T> changes, int amount) where T : notnull
    {
        return new ChangeSet<T>(changes.Select(change =>
        {
            return change.Reason switch
            {
                ListChangeReason.Add => new Change<T>(change.Reason, change.Item.Current, change.Item.CurrentIndex + amount),
                ListChangeReason.AddRange => new Change<T>(change.Reason, change.Range, change.Range.Index + amount),
                ListChangeReason.Replace => new Change<T>(change.Reason, change.Item.Current, change.Item.Previous, change.Item.CurrentIndex + amount, change.Item.PreviousIndex + amount),
                ListChangeReason.Remove => new Change<T>(change.Reason, change.Item.Current, change.Item.Previous, change.Item.CurrentIndex + amount, change.Item.PreviousIndex + amount),
                ListChangeReason.RemoveRange => new Change<T>(change.Reason, change.Range, change.Range.Index + amount),
                ListChangeReason.Refresh => new Change<T>(change.Reason, change.Item.Current),
                ListChangeReason.Moved => new Change<T>(change.Item.Current, change.Item.CurrentIndex + amount, change.Item.PreviousIndex + amount),
                ListChangeReason.Clear => new Change<T>(ListChangeReason.RemoveRange, change.Range, amount),
                _ => throw new ArgumentOutOfRangeException()
            };
        }));
    }

    public class GroupContainer(TGroup group, IObservableList<TItem> items)
    {
        public readonly TGroup Group = group;
        public readonly IObservableList<TItem> Items = items;
        public IDisposable? Disposable;
        
        public int Count => Items.Count;
    }
}