using System;

namespace GGroupp.Internal.Timesheet;

public readonly record struct TimesheetSetGetIn
{
    public TimesheetSetGetIn(Guid userId, DateOnly date)
    {
        UserId = userId;
        Date = date;
    }

    public Guid UserId { get; }

    public DateOnly Date { get; }
}