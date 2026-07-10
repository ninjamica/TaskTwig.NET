using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaskTwig.Core;

namespace TaskTwig.ViewModels;

public class FileActionResponse(DataFile file)
{
    public DataFile File { get; } = file;
    public bool Upload { get; set; } = false;
    public bool Download { get; set; } = false;
}

public class SyncConflictDialogViewModel: ViewModelBase
{
    private readonly Dictionary<DataFile, DataFileAction> _actions;
    public ObservableCollection<FileActionResponse> FileActionResponses { get; }

    public SyncConflictDialogViewModel(Dictionary<DataFile, DataFileAction> actions)
    {
        _actions = actions;
        FileActionResponses = new ObservableCollection<FileActionResponse>(actions
            .Where(pair => pair.Value == DataFileAction.Conflict).Select(pair => new FileActionResponse(pair.Key)));
    }
    
    public Dictionary<DataFile, DataFileAction> GetActions()
    {
        foreach (var action in FileActionResponses)
        {
            if (action.Download)
                _actions[action.File] = DataFileAction.Download;
            else if (action.Upload)
                _actions[action.File] = DataFileAction.Upload;
            else
                return new Dictionary<DataFile, DataFileAction>();
        }
        return _actions;
    }
}