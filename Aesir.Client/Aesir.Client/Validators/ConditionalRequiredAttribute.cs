using System.ComponentModel.DataAnnotations;

namespace Aesir.Client.Validators;

/// <summary>
/// Validation attribute that makes a property required based on the value of another property.
/// </summary>
public class ConditionalRequiredAttribute : ValidationAttribute
{
    private readonly string _dependentProperty;
    private readonly object _targetValue;

    /// <summary>
    /// Initializes a new instance of the ConditionalRequiredAttribute class.
    /// </summary>
    /// <param name="dependentProperty">The name of the property that this validation depends on.</param>
    /// <param name="targetValue">The value that the dependent property must have for this property to be required.</param>
    public ConditionalRequiredAttribute(string dependentProperty, object targetValue)
    {
        _dependentProperty = dependentProperty;
        _targetValue = targetValue;
    }

    /// <summary>
    /// Validates the specified value with respect to the current validation attribute.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="validationContext">The context information about the validation operation.</param>
    /// <returns>An instance of ValidationResult class.</returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var property = validationContext.ObjectType.GetProperty(_dependentProperty);
        if (property == null)
        {
            return new ValidationResult($"Unknown property: {_dependentProperty}");
        }

        var dependentValue = property.GetValue(validationContext.ObjectInstance);
        
        // If the dependent property matches the target value, then this property is required
        if (Equals(dependentValue, _targetValue))
        {
            if (value == null || (value is string stringValue && string.IsNullOrWhiteSpace(stringValue)))
            {
                return new ValidationResult(ErrorMessage ?? $"{validationContext.DisplayName} is required");
            }
        }

        return ValidationResult.Success;
    }
}