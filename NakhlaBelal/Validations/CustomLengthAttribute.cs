using System.ComponentModel.DataAnnotations;

namespace NakhlaBelal.Validations
{
    public class CustomLengthAttribute : ValidationAttribute
    {
        private readonly int _minLength;
        private readonly int _maxLength;

        public CustomLengthAttribute(int minLength, int maxLength)
        {
            _minLength = minLength;
            _maxLength = maxLength;
        }

        public override bool IsValid(object? value) // New Category, 3, 100
        {
            if (value is string result)
            {
                if (result.Length >= _minLength && result.Length <= _maxLength)
                    return true;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The filed {name} Must be between {_minLength} And {_maxLength}";
        }
    }
}
