namespace Tuku.Domain.Common
{
    using System.Collections.Generic;

    public sealed class ValidationResult
    {
        private static readonly ValidationResult OkResult = new ValidationResult(true, new string[0]);

        private ValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public bool IsValid { get; }

        public IReadOnlyList<string> Errors { get; }

        public static ValidationResult Ok
        {
            get { return OkResult; }
        }

        public static ValidationResult Fail(params string[] errors)
        {
            return new ValidationResult(false, errors);
        }

        public static ValidationResult Fail(System.Collections.Generic.IEnumerable<string> errors)
        {
            var list = new System.Collections.Generic.List<string>(errors);
            return new ValidationResult(false, list);
        }
    }
}
