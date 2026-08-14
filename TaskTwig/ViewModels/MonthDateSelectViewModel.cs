using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.ViewModels;

public partial class MonthDateSelectViewModel : ViewModelBase
{
    // public ObservableCollection<int> Dates { get; private set; } =
    // [
    //     1, 2, 3, 4, 5, 6, 7,
    //     8, 9, 10, 11, 12, 13, 14,
    //     15, 16, 17, 18, 19, 20, 21,
    //     22, 23, 24, 25, 26, 27, 28,
    //     29, 30, 31
    // ];
    
    public ObservableCollection<DateCell> Dates { get; set; } =
    [
        new(1),  new(2),  new(3),  new(4),  new(5),  new(6),  new(7),
        new(8),  new(9),  new(10), new(11), new(12), new(13), new(14),
        new(15), new(16), new(17), new(18), new(19), new(20), new(21),
        new(22), new(23), new(24), new(25), new(26), new(27), new(28),
        new(29), new(30), new(31)
    ];

    [ObservableProperty]
    public partial uint SelectedDatesMap { get; set; } = 0u;
}

public class DateCell(int date)
{
    public int Date { get; } = date;
}