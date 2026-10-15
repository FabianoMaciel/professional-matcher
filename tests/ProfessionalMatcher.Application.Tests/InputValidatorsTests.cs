using ProfessionalMatcher.Application.Models;
using ProfessionalMatcher.Application.Professionals.Commands;
using ProfessionalMatcher.Application.Professionals.Validators;

namespace ProfessionalMatcher.Application.Tests;

public class InputValidatorsTests
{
    private static ExperienceInput Experience(string start = "2020-01", string? end = null) =>
        new() { Company = "Acme", Role = "Dev", StartDate = start, EndDate = end };

    [Theory]
    [InlineData("2020-01", null, true)]
    [InlineData("2020-01", "", true)]
    [InlineData("2020-01", "2020-01", true)]
    [InlineData("2020-01", "2022-06", true)]
    [InlineData("2020-13", null, false)]
    [InlineData("2020/01", null, false)]
    [InlineData("", null, false)]
    [InlineData("2020-05", "2019-01", false)]
    [InlineData("2020-01", "2022-6", false)]
    public void ExperienceInput_ValidatesMonthsAndOrder(string start, string? end, bool expected)
    {
        new ExperienceInputValidator().Validate(Experience(start, end)).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(6, false)]
    public void SkillInput_LevelMustBeBetweenOneAndFive(int level, bool expected)
    {
        var input = new SkillInput { Name = "C#", Category = "Backend", YearsOfExperience = 3, Level = level };

        new SkillInputValidator().Validate(input).IsValid.Should().Be(expected);
    }

    [Fact]
    public void SkillInput_MissingNameOrCategory_IsInvalid()
    {
        var input = new SkillInput { Name = "", Category = "", Level = 3 };

        var result = new SkillInputValidator().Validate(input);

        result.Errors.Select(e => e.PropertyName).Should().Contain(["Name", "Category"]);
    }

    [Theory]
    [InlineData(2005, 2009, true)]
    [InlineData(2005, null, true)]
    [InlineData(2009, 2005, false)]
    [InlineData(1800, null, false)]
    public void EducationInput_ValidatesYears(int start, int? end, bool expected)
    {
        var input = new EducationInput { Institution = "Univ", Degree = "BSc", StartYear = start, EndYear = end };

        new EducationInputValidator().Validate(input).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("Jane", "jane@example.com", true)]
    [InlineData("", "jane@example.com", false)]
    [InlineData("Jane", "not-an-email", false)]
    public void ProfessionalDetails_RequiresNameAndEmail(string name, string email, bool expected)
    {
        var input = new ProfessionalDetailsInput { FullName = name, Email = email };

        new ProfessionalDetailsInputValidator().Validate(input).IsValid.Should().Be(expected);
    }

    [Theory]
    [InlineData("image/png", 10, true)]
    [InlineData("image/jpeg", 10, true)]
    [InlineData("image/gif", 10, false)]
    [InlineData("image/png", 0, false)]
    [InlineData("image/png", UploadProfessionalPhoto.MaxSizeInBytes + 1, false)]
    public void Photo_ValidatesTypeAndSize(string contentType, int size, bool expected)
    {
        var request = new UploadProfessionalPhoto.Request { ContentType = contentType, Content = new byte[size] };

        new UploadProfessionalPhoto.Validator().Validate(request).IsValid.Should().Be(expected);
    }

    [Fact]
    public void MonthFormat_RoundTrips()
    {
        MonthFormat.ToText(MonthFormat.Parse("2021-07")).Should().Be("2021-07");
    }
}
