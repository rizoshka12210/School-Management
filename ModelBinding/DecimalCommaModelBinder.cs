using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SchoolManagementSystem.Web.ModelBinding;

/// <summary>
/// Accepts both "85.5" and "85,5" for decimal grade fields, regardless of
/// the current request culture - teachers type grades using a comma
/// (the local convention) and HTML number inputs are unreliable across
/// browsers/locales, so this always parses with the invariant culture
/// after normalizing a comma to a dot.
/// </summary>
public class DecimalCommaModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

        var rawValue = valueProviderResult.FirstValue;
        var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) != null;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (isNullable)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        var normalized = rawValue.Trim().Replace(',', '.');

        if (decimal.TryParse(
                normalized,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var value))
        {
            bindingContext.Result = ModelBindingResult.Success(value);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                modelName,
                "The value must be a number.");
        }

        return Task.CompletedTask;
    }
}
