using System;
using UnitGenerator;

namespace AdventureWorks
{
    /// <summary>
    /// Revision number
    /// </summary>
    [UnitOf(typeof(short), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct RevisionNumber
    {
    }

    /// <summary>
    /// ID of Employee
    /// </summary>
    [UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct EmployeeId
    {
    }

    /// <summary>
    /// Money value
    /// </summary>
    [UnitOf(typeof(decimal), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct Money
    {
    }

    /// <summary>
    /// Modified date time
    /// </summary>
    [UnitOf(typeof(DateTime), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct ModifiedDateTime
    {
    }

    /// <summary>
    /// Number of days
    /// </summary>
    [UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct Days
    {
    }

    /// <summary>
    /// Date without time
    /// </summary>
    [UnitOf(typeof(DateTime), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct Date
    {
    }

    /// <summary>
    /// Weight
    /// </summary>
    [UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct Gram
    {
    }

    /// <summary>
    /// ID of SalesTaxRate
    /// </summary>
    [UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct SalesTaxRateId
    {
    }
}