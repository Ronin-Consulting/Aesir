using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Aesir.Client.Validators;

public class ValidJsonDictionaryAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string jsonString || string.IsNullOrWhiteSpace(jsonString))
        {
            return true;
        }

        try
        {
            var result = JsonConvert.DeserializeObject<Dictionary<string, string?>>(jsonString);
            return result != null;
        }
        catch (JsonException)
        {
            return false;
        }

    }

    public override string FormatErrorMessage(string name)
    {
        return $"{name} must contain valid JSON (dictionary of string key-value pairs).";
    }
}
