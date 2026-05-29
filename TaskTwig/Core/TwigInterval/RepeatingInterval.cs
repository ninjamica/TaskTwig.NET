using System;

namespace TaskTwig.Core.TwigInterval;

public abstract class RepeatingInterval : ITwigInterval
{
    public DateOnly? NextOccurrence
    {
        get
        {
            DateOnly? date = NextFromDate(ReferenceDate);

            if (AutoRepeat && date is not null)
            {
                // TODO: implement auto repeat, requires TaskTwig.Today                
            }
            
            return date;
        }
    }

    public DateOnly? PreviousOccurrence
    {
        get
        {
            DateOnly? date = PreviousFromDate(ReferenceDate);

            if (AutoRepeat && date is not null)
            {
                // TODO: implement auto repeat, requires TaskTwig.Today                
            }
            
            return date;
        }
    }

    public required DateOnly ReferenceDate { get; set; }
    public required bool AutoRepeat { get; set; }

    protected abstract DateOnly? NextFromDate(DateOnly refDate);
    protected abstract DateOnly? PreviousFromDate(DateOnly refDate);
}