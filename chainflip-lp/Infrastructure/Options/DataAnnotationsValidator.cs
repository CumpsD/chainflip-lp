namespace ChainflipLp.Infrastructure.Options
{
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    public interface IDataAnnotationsValidator
    {
        bool TryValidateObject(
            object value,
            ICollection<ValidationResult> results,
            IDictionary<object, object?>? validationContextItems = null);

        bool TryValidateObjectRecursive<T>(
            T value,
            List<ValidationResult> results,
            IDictionary<object, object?>? validationContextItems = null);
    }

    public class DataAnnotationsValidator : IDataAnnotationsValidator
    {
        public bool TryValidateObject(
            object value,
            ICollection<ValidationResult> results,
            IDictionary<object, object?>? validationContextItems = null)
            => Validator.TryValidateObject(
                value,
                new ValidationContext(value, null, validationContextItems),
                results,
                true);

        public bool TryValidateObjectRecursive<T>(
            T value,
            List<ValidationResult> results,
            IDictionary<object, object?>? validationContextItems = null)
            => TryValidateObjectRecursive(
                value,
                results,
                new HashSet<object>(),
                validationContextItems);

        private bool TryValidateObjectRecursive<T>(
            T obj,
            ICollection<ValidationResult> results,
            ISet<object> validatedObjects,
            IDictionary<object, object?>? validationContextItems = null)
        {
            if (obj == null)
                return false;

            //short-circuit to avoid infinite loops on cyclical object graphs
            if (validatedObjects.Contains(obj))
                return true;

            validatedObjects.Add(obj);

            var result = TryValidateObject(obj, results, validationContextItems);

            var properties = obj
                .GetType()
                .GetProperties()
                .Where(
                    prop => prop.CanRead &&
                        prop.CanWrite &&
                        prop.GetIndexParameters()
                            .Length ==
                        0)
                .ToList();

            foreach (var property in properties)
            {
                var property1 = property;

                if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
                    continue;

                var value = obj
                        .GetType()
                        .GetProperty(property.Name)
                        ?
                        .GetValue(obj, null) ??
                    null;

                switch (value)
                {
                    case null:
                        continue;

                    case IEnumerable asEnumerable:
                    {
                        foreach (var enumObj in asEnumerable)
                        {
                            if (enumObj == null)
                                continue;

                            var nestedResults = new List<ValidationResult>();
                            if (TryValidateObjectRecursive(
                                    enumObj,
                                    nestedResults,
                                    validatedObjects,
                                    validationContextItems))
                                continue;

                            result = false;
                            foreach (var validationResult in nestedResults)
                                results.Add(
                                    new ValidationResult(
                                        validationResult.ErrorMessage,
                                        validationResult.MemberNames.Select(x => property1.Name + '.' + x)));
                        }

                        break;
                    }

                    default:
                    {
                        var nestedResults = new List<ValidationResult>();
                        if (!TryValidateObjectRecursive(value, nestedResults, validatedObjects, validationContextItems))
                        {
                            result = false;
                            foreach (var validationResult in nestedResults)
                                results.Add(
                                    new ValidationResult(
                                        validationResult.ErrorMessage,
                                        validationResult.MemberNames.Select(x => property1.Name + '.' + x)));
                        }

                        break;
                    }
                }
            }

            return result;
        }
    }
}