using System;
using DynamicData;

namespace TaskTwig.Core.Util;

public static class GroupHeaderFlattenerEx
{
    public static IObservable<IChangeSet<TDestination>> FlattenWithGroupHeaders<TGroup, TItem, TDestination>(
        this IObservable<IChangeSet<TGroup>> source, 
        Func<TGroup, IObservableList<TItem>> itemsSelector,
        Func<TGroup, TDestination> headerConverter, 
        Func<TItem, TDestination> itemConverter)
        where TGroup : notnull
        where TItem : notnull
        where TDestination : notnull
    {
        return new GroupHeaderFlattener<TGroup, TItem, TDestination>(source, itemsSelector, headerConverter, itemConverter).Run();
    }
}