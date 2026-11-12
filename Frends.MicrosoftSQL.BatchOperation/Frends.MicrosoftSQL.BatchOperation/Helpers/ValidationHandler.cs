using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Frends.MicrosoftSQL.BatchOperation.Helpers;

/// <summary>
/// Validates objects by their ValidationAttributes.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Untouched version already tested in template solution.")]
internal static class ValidationHandler
{
    internal static void Run(params object[] objects)
    {
        if (objects == null || objects.Length == 0)
            throw new ValidationException("Validation failed:\nYou must provide objects to validate");
        if (objects.Any(obj => obj == null))
            throw new ValidationException("Validation failed:\nAny of validated objects can't be null");
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var errors = new List<string>();
        foreach (var obj in objects)
            Validate(obj, obj.GetType().Name, visited, errors);

        if (errors.Count > 0)
            throw new ValidationException($"Validation failed:\n{string.Join("\n", errors)}");
    }

    private static void Validate(
        object obj,
        string path,
        HashSet<object> visited,
        List<string> errors)
    {
        if (obj == null)
        {
            errors.Add("Validated object can't be null!");
            return;
        }

        if (IsLeaf(obj.GetType()) || !visited.Add(obj)) return;

        List<ValidationResult> results = [];
        Validator.TryValidateObject(obj, new ValidationContext(obj), results, true);
        errors.AddRange(results.Select(r => r.ErrorMessage));

        if (obj is IEnumerable enumerable)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                var itemPath = $"{path}[{index++}]";
                Validate(item, itemPath, visited, errors);
            }

            return;
        }

        foreach (var property in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || !property.CanRead) continue;
            if (IsLeaf(property.PropertyType)) continue;

            var value = property.GetValue(obj);

            // Null properties are covered by their own attributes (e.g. [Required]).
            if (value != null)
            {
                Validate(
                    value,
                    $"{path}.{property.Name}",
                    visited,
                    errors);
            }
        }
    }

    private static bool IsLeaf(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
               || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
               || type == typeof(Guid) || type == typeof(byte[]) || type == typeof(object);
    }
}
