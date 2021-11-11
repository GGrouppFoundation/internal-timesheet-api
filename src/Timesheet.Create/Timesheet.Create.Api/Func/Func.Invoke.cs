﻿using GGroupp.Infra;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;

namespace GGroupp.Internal.Timesheet;

partial class TimesheetCreateGetFunc
{
    public ValueTask<Result<TimesheetCreateOut, Failure<TimesheetCreateFailureCode>>> InvokeAsync(
        TimesheetCreateIn input, CancellationToken cancellationToken)
        =>
        AsyncPipeline.Pipe(
            input, cancellationToken)
        .Pipe(
            @in => new DataverseEntityCreateIn<Dictionary<string, object?>>(
                entityPluralName: "gg_timesheetactivities",
                selectFields: selectedFields,
                entityData: MapJsonIn(@in)))
        .PipeValue(
            entityCreateSupplier.CreateEntityAsync<Dictionary<string, object?>, TimesheetJsonOut>)
        .MapFailure(
            failure => failure.MapFailureCode(MapDataverseFailureCode))
        .MapSuccess(
            entityCreateOut => new TimesheetCreateOut(entityCreateOut.Value.TimesheetId));

    private static Dictionary<string, object?> MapJsonIn(TimesheetCreateIn input)
    {
        var json = new Dictionary<string, object?>
        {
            ["ownerid@odata.bind"] = Invariant($"/systemusers({input.OwnerId:D})"),
            ["gg_date"] = input.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["gg_description"] = input.Description,
            ["gg_duration"] = input.Duration
        };

        var entityData = GetProjectEntityData(input.ProjectType);
        json[$"regardingobjectid_{entityData.Name}@odata.bind"] = Invariant($"/{entityData.PluralName}({input.ProjectId:D})");

        return json;
    }

    private static (string Name, string PluralName) GetProjectEntityData(TimesheetProjectType projectType)
        =>
        projectType switch
        {
            TimesheetProjectType.Lead => ("lead", "leads"),
            TimesheetProjectType.Opportunity => ("opportunity", "opportunities"),
            _ => ("gg_project", "gg_projects")
        };

    private static TimesheetCreateFailureCode MapDataverseFailureCode(int dataverseFailureCode)
        =>
        dataverseFailureCode switch
        {
            NotFoundFailureCode => TimesheetCreateFailureCode.NotFound,
            _ => TimesheetCreateFailureCode.Unknown
        };
}
