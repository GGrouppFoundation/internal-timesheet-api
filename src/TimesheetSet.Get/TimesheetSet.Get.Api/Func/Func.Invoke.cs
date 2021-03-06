using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GGroupp.Infra;

namespace GGroupp.Internal.Timesheet;

partial class TimesheetSetGetFunc
{
    public ValueTask<Result<TimesheetSetGetOut, Failure<TimesheetSetGetFailureCode>>> InvokeAsync(
        TimesheetSetGetIn input, CancellationToken cancellationToken = default)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .HandleCancellation()
        .Pipe(
            static @in => new DataverseEntitySetGetIn(
                entityPluralName: ApiNames.TimesheetEntityPluralName,
                selectFields: ApiNames.SelectedFields,
                filter: BuildFilter(@in.UserId, @in.Date),
                orderBy: ApiNames.OrderBy))
        .PipeValue(
            entitySetGetSupplier.GetEntitySetAsync<TimesheetJsonOut>)
        .MapFailure(
            failure => failure.MapFailureCode(MapFailureCode))
        .MapSuccess(
            succsess => new TimesheetSetGetOut(
                timesheets: succsess.Value.Select(MapItemSuccess).ToArray()));

    private static string BuildFilter(Guid userId, DateOnly date)
        =>
        new StringBuilder(
            $"_ownerid_value eq '{userId}'")
        .Append(
            " and ")
        .Append(
            $"gg_date eq {date:yyyy-MM-dd}")
        .ToString();

    private static TimesheetSetItemGetOut MapItemSuccess(TimesheetJsonOut itemJson)
        =>
        new(
            timesheetId: itemJson.TimesheetId,
            date: new(itemJson.Date.Year, itemJson.Date.Month, itemJson.Date.Day),
            duration: itemJson.Duration,
            projectName: itemJson.ProjectName, 
            description: itemJson.Description);

    private static TimesheetSetGetFailureCode MapFailureCode(DataverseFailureCode code)
        =>
        code switch
        {
            DataverseFailureCode.UserNotEnabled => TimesheetSetGetFailureCode.NotAllowed,
            DataverseFailureCode.PrivilegeDenied => TimesheetSetGetFailureCode.NotAllowed,
            _ => default
        };
}