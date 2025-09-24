using System;
using System.Collections.Generic;
using System.Linq;
using Aesir.Common.Models;
using Avalonia.Data.Converters;

namespace Aesir.Client.Converters;

public class ToolRequestExistsConverter : FuncValueConverter<IEnumerable<ToolRequest>,string, bool>
{
    public ToolRequestExistsConverter() : base(
        (requests, toolName) => 
            requests?.Any(r => r.Name == toolName) ?? false
    )
    {
    }

    public ToolRequestExistsConverter(Func<IEnumerable<ToolRequest>?, string?, bool> convert) : base(convert)
    {
    }
}