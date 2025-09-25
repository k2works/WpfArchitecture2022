using System;
using UnitGenerator;

namespace AdventureWorks.Sales
{
    /// <summary>
    /// ID of User
    /// </summary>
    [UnitOf(typeof(int), UnitGenerateOptions.DapperTypeHandler)]
    public partial struct UserId
    {
    }
}
