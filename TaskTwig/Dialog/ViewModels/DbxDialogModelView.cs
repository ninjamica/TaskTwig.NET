using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTwig.ViewModels;

namespace TaskTwig.Dialog.ViewModels;

public partial class DbxDialogModelView(Uri url) : ViewModelBase
{
    [ObservableProperty]
    public partial string UrlLinkText { get; set; } = url.OriginalString;
    
    [ObservableProperty]
    public partial string? CodeText { get; set; }
}