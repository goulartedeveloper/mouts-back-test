using Ambev.DeveloperEvaluation.Domain.Validation;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Validation
{
    /// <summary>
    /// Contains unit tests for the <see cref="PhoneValidator"/> class.
    /// Tests validation of phone numbers according to the following rules:
    /// - Must not be empty
    /// - Must match pattern: ^\+?[1-9]\d{1,14}$
    ///   (Optional '+' prefix, first digit 1-9, followed by 1-14 digits)
    /// </summary>
    public class PhoneValidatorTests
    {
        [Theory(DisplayName = "Given a phone number When validating Then should validate according to regex pattern")]
        [InlineData("+123456789", true)]
        [InlineData("123456789", true)]
        [InlineData("+551199999999", true)]
        [InlineData("11999999999", true)]
        [InlineData("999999999", true)]
        [InlineData("+0123456789", false)]
        [InlineData("0123456789", false)]
        [InlineData("+", false)]
        [InlineData("+12345678901234567", false)]
        [InlineData("12345678901234567", false)]
        [InlineData("abc12345678", false)]
        [InlineData("12.34567890", false)]
        [InlineData("", false)]
        public void Given_PhoneNumber_When_Validating_Then_ShouldValidateAccordingToPattern(string phone, bool expectedResult)
        {
            var validator = new PhoneValidator();

            var result = validator.Validate(phone);

            Assert.Equal(expectedResult, result.IsValid);
        }
    }
}
