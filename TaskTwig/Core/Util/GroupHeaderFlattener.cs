using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;

namespace TaskTwig.Core.Util;

public class GroupHeaderFlattener<TGroup, TItem, TDestination>
    where TGroup : TaskCategory 
    where TItem : notnull
    where TDestination : notnull
{
    private IObservable<IChangeSet<TGroup>> _source;
    private Func<TGroup, IObservable<IChangeSet<TItem>>> _itemsSelector;
    private Func<TGroup, TDestination> _headerConverter;
    private Func<TItem, TDestination> _itemConverter;
    
    private ChangeAwareList<TDestination> _flatList = new();
    private ChangeAwareList<GroupContainer> _groupList = new();
    
    public FlattenWithGroupHeaders(IObservable<IChangeSet<TGroup>> source, Func<TGroup, IObservable<IChangeSet<TItem>>> itemsSelector, Func<TGroup, TDestination> headerConverter, Func<TItem, TDestination> itemConverter)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(itemsSelector);
        ArgumentNullException.ThrowIfNull(headerConverter);
        
        _source = source;
        _itemsSelector = itemsSelector;
        _headerConverter = headerConverter;
        _itemConverter = itemConverter;

        source.ForEachItemChange(OnIndividualGroupChange).Transform(group =>
        {
            var header = headerConverter(group);
            // var headerStream =
            //     Observable.Return(new ChangeSet<TDestination>([
            //         new Change<TDestination>(ListChangeReason.Add, header, 0)
            //     ]));
            var headerList = new SourceList<TDestination>();
            headerList.Add(header);
            
            var items = itemsSelector(group).Transform(itemConverter).Select(changeSet => ShiftIndices(changeSet, 1));
            items.PopulateInto(headerList);
            // headerStream.Concat(items);
            // items.PopulateInto<TDestination>(headerList);
            // return headerList.Pop;
            
            return headerList.Connect();
        }).Subscribe();
    }

    private void OnIndividualGroupChange(ItemChange<GroupContainer> change)
    {
        switch (change.Reason)
        {
            case ListChangeReason.Add:
                var addLength = change.Current.Group.TasksView.Count + 1;
                int newFlatIndex = change.CurrentIndex < _groupList.Count ? _groupList[change.CurrentIndex].Index : (change.CurrentIndex > 0 ? _groupList.Last().Index + _groupList.Last().Length : 0);

                for (int index = change.CurrentIndex; index < _groupList.Count; index++)
                {
                    _groupList[index].Index += addLength;
                }
                
                _groupList.Add(new(change.Current.Group, newFlatIndex, addLength));
                break;
            
            case ListChangeReason.Replace:
                break;
            
            case ListChangeReason.Remove:
                var remLength = _groupList[change.CurrentIndex].Length;
                for (int index = change.CurrentIndex + 1; index < _groupList.Count; index++)
                {
                    _groupList[index].Index -= remLength;
                }
                _groupList.RefreshAt(change.CurrentIndex);
                break;
            
            case ListChangeReason.Refresh:
                break;
            
            case ListChangeReason.Moved:
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
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
                ListChangeReason.Remove => new Change<T>(change.Reason, change.Item.Current, change.Item.Previous, change.Item.CurrentIndex + 1, change.Item.PreviousIndex + 1),
                ListChangeReason.RemoveRange => new Change<T>(change.Reason, change.Range, change.Range.Index + 1),
                ListChangeReason.Refresh => new Change<T>(change.Reason, change.Item.Current),
                ListChangeReason.Moved => new Change<T>(change.Item.Current, change.Item.PreviousIndex + 1, change.Item.CurrentIndex + 1),
                ListChangeReason.Clear => new Change<T>(ListChangeReason.RemoveRange, change.Range, 1),
                _ => throw new ArgumentOutOfRangeException()
            };
        }));
    }

    public class GroupContainer(TGroup group, int index, int length)
    {
        public readonly TGroup Group = group;
        public int Index = index;
        public int Length = length;
    }
}