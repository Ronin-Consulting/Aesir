using System.Data;
using Aesir.Common.Models;
using Dapper;

namespace Aesir.Infrastructure.Data;

public class ThinkValueTypeHandler : SqlMapper.TypeHandler<ThinkValue>
{
    // write ThinkValue as text (its string form) or pass DBNull if null
    public override void SetValue(IDbDataParameter parameter, ThinkValue value)
    {
        var s = (string?)value;
        parameter.Value = (object?)s ?? DBNull.Value;
        parameter.DbType = DbType.String;
    }

    // read from database text into ThinkValue
    public override ThinkValue Parse(object value)
    {
        // value can be string or DBNull
        if (value is DBNull) return new ThinkValue((string?)null);
        return new ThinkValue(value?.ToString());
    }
}
