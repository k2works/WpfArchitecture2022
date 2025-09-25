using UnitGenerator;

namespace AdventureWorks.Purchasing
{

    /// <summary>
    /// ID of Unit
    /// </summary>
    [UnitOf(typeof(int), UnitGenerateOptions.None, "{0:###,###,###}")]
    public readonly partial struct UserId
    {
    }

}
