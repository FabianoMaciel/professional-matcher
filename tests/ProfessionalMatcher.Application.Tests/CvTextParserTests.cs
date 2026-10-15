using ProfessionalMatcher.Application.Professionals.CvImport;

namespace ProfessionalMatcher.Application.Tests;

public class CvTextParserTests
{
    private static readonly DateOnly Today = new(2026, 10, 14);

    private static ParsedCv Parse(string text) => CvTextParser.Parse(text, Today);

    /// <summary>The layout of the reference CV (title | company with a right-aligned date, location — blurb, bullets), with made-up data.</summary>
    private const string ReferenceLayout = """
        JANE ALICE DOE
        Senior Software Engineer — .NET, Azure & Cloud
        Ireland | jane.doe@example.com | linkedin.com/in/janedoe
        SUMMARY
        Senior Software Engineer with 12+ years building business-critical systems. Core stack: C#, .NET / .NET Core,
        SQL Server, and Azure, with experience in Docker and Kubernetes.
        KEY ACHIEVEMENTS
        • Cut processing time from 24 hours to 10 minutes.
        TECHNICAL SKILLS
        Languages & Frameworks: C#, .NET / .NET Core, ASP.NET WebForms/MVC, Angular, TypeScript, Entity Framework,
        Dapper, REST APIs
        Data: SQL Server, Oracle SQL Developer, T-SQL performance tuning
        Cloud & DevOps: Azure, Azure Service Bus, CI/CD pipelines, Docker,
        Kubernetes, Internet Information Services (IIS)
        Practices: Microservices architecture, Agile
        EXPERIENCE
        Senior Software Engineer | Acme, Widgets & Co.   Jan 2022 – Present
        Limerick, Ireland — Order management SaaS platform
        • Developed and maintained the order portal serving 1,300+ customers across ~100 markets and
        supporting 30,000+ annual orders, delivering new REST API-backed services.
        • Optimized SQL Server stored procedures, cutting report time from 1 hour to a few minutes.
        .NET Developer | Beta Payments Corporation   May 2020 – Dec 2021
        Co. Dublin, Ireland — Payments platform
        • Maintained a legacy C#/ASP.NET WebForms platform used by retail banks.
        Senior .NET Developer | Gamma For Sales Management   Apr 2012 – Aug 2017
        Brazil — Multi-tenant sales platform
        • Rewrote a legacy VB integration in C#/.NET with parallel processing, cutting 24 hours to minutes.
        • Mentored a team of 5 junior developers.
        EDUCATION
        Postgraduate Specialization, Full Stack .NET Development — Sirius Education (Aug 2024 – Dec 2025)
        Higher Technology Degree in Systems Analysis — Grupo Uniftec, Brazil (2012 – 2016)
        CERTIFICATIONS
        ISTQB Certified Tester – Foundation Level (CTFL)
        LANGUAGES
        English (Full Professional) · Portuguese (Native)
        """;

    [Fact]
    public void Header_NameHeadlineLocationAndEmail()
    {
        var cv = Parse(ReferenceLayout);

        cv.FullName.Should().Be("Jane Alice Doe", "an ALL-CAPS name is title-cased");
        cv.Headline.Should().Be("Senior Software Engineer — .NET, Azure & Cloud");
        cv.Location.Should().Be("Ireland");
        cv.Email.Should().Be("jane.doe@example.com");
        cv.Summary.Should().StartWith("Senior Software Engineer with 12+ years").And.EndWith("Docker and Kubernetes.");
    }

    [Fact]
    public void Skills_AreGroupedByCategoryAndWrappedListsContinue()
    {
        var cv = Parse(ReferenceLayout);

        cv.Skills.Where(s => s.Category == "Languages & Frameworks").Select(s => s.Name).Should().Equal(
            "C#", ".NET", ".NET Core", "ASP.NET WebForms/MVC", "Angular", "TypeScript", "Entity Framework", "Dapper", "REST APIs");
        cv.Skills.Where(s => s.Category == "Data").Select(s => s.Name).Should().Equal(
            "SQL Server", "Oracle SQL Developer", "T-SQL performance tuning");
        cv.Skills.Where(s => s.Category == "Cloud & DevOps").Select(s => s.Name).Should().Equal(
            "Azure", "Azure Service Bus", "CI/CD pipelines", "Docker", "Kubernetes", "Internet Information Services (IIS)");
        cv.Skills.Should().Contain(s => s.Name == "Agile" && s.Category == "Practices");
    }

    [Fact]
    public void Skills_YearsAndLevelComeFromTheJobsThatMentionThem()
    {
        var cv = Parse(ReferenceLayout);

        // C# appears in the 2020-21 job and in the 2012-17 job: 20 + 64 months = 7 years.
        var csharp = cv.Skills.Single(s => s.Name == "C#");
        csharp.YearsOfExperience.Should().Be(7);
        csharp.Level.Should().Be(4);
        csharp.IsPrimary.Should().BeTrue("6+ years");

        // Listed on the CV but never shown in a job: basic, secondary.
        var angular = cv.Skills.Single(s => s.Name == "Angular");
        angular.YearsOfExperience.Should().Be(0);
        angular.Level.Should().Be(2);
        angular.IsPrimary.Should().BeFalse();

        // Short experience, but named in the headline.
        cv.Skills.Single(s => s.Name == "Azure").IsPrimary.Should().BeTrue("it is in the headline");
        cv.Skills.Single(s => s.Name == "Docker").IsPrimary.Should().BeFalse();
    }

    [Fact]
    public void Experiences_TitleCompanyDatesAndTechnologies()
    {
        var cv = Parse(ReferenceLayout);

        cv.Experiences.Should().HaveCount(3);

        var current = cv.Experiences[0];
        current.Role.Should().Be("Senior Software Engineer");
        current.Company.Should().Be("Acme, Widgets & Co.");
        current.StartDate.Should().Be(new DateOnly(2022, 1, 1));
        current.EndDate.Should().BeNull("\"Present\" means no end date");
        current.Description.Should().StartWith("Limerick, Ireland — Order management SaaS platform\n• Developed and maintained");
        current.Description.Should().Contain("supporting 30,000+ annual orders", "wrapped bullet lines are joined");
        current.Technologies.Should().Contain(["SQL Server", "REST APIs"]);

        var second = cv.Experiences[1];
        second.Role.Should().Be(".NET Developer");
        second.Company.Should().Be("Beta Payments Corporation");
        second.StartDate.Should().Be(new DateOnly(2020, 5, 1));
        second.EndDate.Should().Be(new DateOnly(2021, 12, 1));

        var third = cv.Experiences[2];
        third.Role.Should().Be("Senior .NET Developer");
        third.Company.Should().Be("Gamma For Sales Management", "the part without job-title words is the company");
        third.StartDate.Should().Be(new DateOnly(2012, 4, 1));
        third.EndDate.Should().Be(new DateOnly(2017, 8, 1));
        third.Description.Split('\n').Should().HaveCount(3);
    }

    [Fact]
    public void Education_DegreeInstitutionAndYears()
    {
        var cv = Parse(ReferenceLayout);

        cv.Education.Should().Equal(
            new ParsedEducation("Sirius Education", "Postgraduate Specialization, Full Stack .NET Development", 2024, 2025),
            new ParsedEducation("Grupo Uniftec, Brazil", "Higher Technology Degree in Systems Analysis", 2012, 2016));
    }

    [Fact]
    public void OtherSections_AreReportedAsIgnored_AndWellFormedCvHasNoWarnings()
    {
        var cv = Parse(ReferenceLayout);

        cv.IgnoredSections.Should().Equal("KEY ACHIEVEMENTS", "CERTIFICATIONS", "LANGUAGES");
        cv.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Portuguese_HeadingsAndDates()
    {
        var cv = Parse("""
            Carlos Eduardo Souza
            Desenvolvedor Back-end Sênior
            São Paulo, SP | carlos@example.com | (11) 99999-0000
            RESUMO PROFISSIONAL
            Desenvolvedor com 10 anos de experiência em sistemas corporativos.
            HABILIDADES TÉCNICAS
            Linguagens: C#, Java, SQL
            Bancos de dados: PostgreSQL, Oracle
            EXPERIÊNCIA PROFISSIONAL
            Desenvolvedor Sênior | Banco Exemplo S.A.   jan/2020 - atual
            • Desenvolvimento de APIs em C# com PostgreSQL.
            Analista de Sistemas | Tecnologia Beta Ltda   03/2015 – 12/2019
            • Manutenção de sistemas em Java e Oracle.
            FORMAÇÃO ACADÊMICA
            Bacharelado em Ciência da Computação — Universidade Federal de Minas (2008 – 2012)
            IDIOMAS
            Português (nativo) · Inglês (avançado)
            """);

        cv.FullName.Should().Be("Carlos Eduardo Souza");
        cv.Headline.Should().Be("Desenvolvedor Back-end Sênior");
        cv.Location.Should().Be("São Paulo, SP");
        cv.Summary.Should().Be("Desenvolvedor com 10 anos de experiência em sistemas corporativos.");
        cv.Skills.Select(s => (s.Name, s.Category)).Should().Contain(
            [("C#", "Linguagens"), ("PostgreSQL", "Bancos de dados"), ("Oracle", "Bancos de dados")]);

        cv.Experiences.Should().HaveCount(2);
        cv.Experiences[0].Should().BeEquivalentTo(new
        {
            Company = "Banco Exemplo S.A.",
            Role = "Desenvolvedor Sênior",
            StartDate = new DateOnly(2020, 1, 1),
            EndDate = (DateOnly?)null,
        }, o => o.ExcludingMissingMembers());
        cv.Experiences[1].Company.Should().Be("Tecnologia Beta Ltda");
        cv.Experiences[1].Role.Should().Be("Analista de Sistemas");
        cv.Experiences[1].StartDate.Should().Be(new DateOnly(2015, 3, 1));
        cv.Experiences[1].EndDate.Should().Be(new DateOnly(2019, 12, 1));

        cv.Education.Should().ContainSingle().Which.Should().Be(
            new ParsedEducation("Universidade Federal de Minas", "Bacharelado em Ciência da Computação", 2008, 2012));
        cv.IgnoredSections.Should().Equal("IDIOMAS");
    }

    [Fact]
    public void Experience_CompanyAndTitleOnTheirOwnLinesAboveTheDates()
    {
        var cv = Parse("""
            Jane Doe
            WORK HISTORY
            Acme Corp
            Senior Developer
            Jan 2020 – Present
            • Built the billing system.
            Beta Ltd
            Developer
            2015 – 2019
            • Maintained the legacy portal.
            """);

        cv.Experiences.Select(e => (e.Company, e.Role, e.StartDate, e.EndDate)).Should().Equal(
            ("Acme Corp", "Senior Developer", new DateOnly(2020, 1, 1), (DateOnly?)null),
            ("Beta Ltd", "Developer", new DateOnly(2015, 1, 1), new DateOnly(2019, 12, 1)));
        cv.Experiences[0].Description.Should().Be("• Built the billing system.");
    }

    [Fact]
    public void Experience_DatesOnTheirOwnLineAboveTitleAndCompany()
    {
        var cv = Parse("""
            Jane Doe
            EMPLOYMENT HISTORY
            Jan 2020 - Present
            Senior Developer
            Acme Corp
            • Built the billing system.
            2015 - 2019
            Developer
            Beta Ltd
            • Maintained the legacy portal.
            """);

        cv.Experiences.Select(e => (e.Company, e.Role)).Should().Equal(
            ("Acme Corp", "Senior Developer"), ("Beta Ltd", "Developer"));
    }

    [Theory]
    [InlineData("Software Engineer at Acme Corp (03/2018 – 02/2021)")]
    [InlineData("Software Engineer, Acme Corp (Mar 2018 - Feb 2021)")]
    [InlineData("Acme Corp — Software Engineer | March 2018 to February 2021")]
    [InlineData("Acme Corp | Software Engineer | 2018-03 – 2021-02")]
    public void Experience_OtherHeaderStylesAreUnderstood(string header)
    {
        var cv = Parse($"Jane Doe\nPROFESSIONAL EXPERIENCE\n{header}\n• Did the work.");

        var experience = cv.Experiences.Should().ContainSingle().Subject;
        experience.Company.Should().Be("Acme Corp");
        experience.Role.Should().Be("Software Engineer");
        experience.StartDate.Should().Be(new DateOnly(2018, 3, 1));
        experience.EndDate.Should().Be(new DateOnly(2021, 2, 1));
    }

    [Fact]
    public void Experience_WhenKeywordsCannotTellTitleFromCompany_FollowsTheOrderUsedElsewhere()
    {
        var cv = Parse("""
            Jane Doe
            EXPERIENCE
            Acme Corp | Software Engineer | Jan 2020 – Present
            Zorp | Wizard | 2015 – 2019
            """);

        // "Wizard" says nothing, but the first entry shows this CV writes "Company | Title".
        cv.Experiences.Select(e => (e.Company, e.Role)).Should().Equal(("Acme Corp", "Software Engineer"), ("Zorp", "Wizard"));
    }

    [Fact]
    public void Skills_OnePerLineBulletedAndWithQualifiers()
    {
        var cv = Parse("""
            Jane Doe
            SKILLS
            • C# (8 years)
            • Python - Advanced
            • Docker
            • Strong communication skills and a passion for building great software for customers worldwide
            """);

        cv.Skills.Select(s => s.Name).Should().Equal("C#", "Python", "Docker");
        cv.Skills[0].YearsOfExperience.Should().Be(8, "years written on the CV win over the estimate");
        cv.Skills[1].Level.Should().Be(4);
        cv.Warnings.Should().ContainSingle(w => w.Contains("1 item"));
    }

    [Fact]
    public void Skills_InlineHeadingAndPlainCommaList()
    {
        var cv = Parse("Jane Doe\nSkills: C#, Java, SQL Server; Docker | Kubernetes\n");

        cv.Skills.Select(s => s.Name).Should().Equal("C#", "Java", "SQL Server", "Docker", "Kubernetes");
        cv.Skills.Should().OnlyContain(s => s.Category == "Other");
    }

    [Fact]
    public void Skills_OverlappingJobsAreCountedOnce()
    {
        var cv = Parse("""
            Jane Doe
            SKILLS
            Languages: Rust
            EXPERIENCE
            Engineer | Acme Ltd   Jan 2018 – Dec 2020
            • Wrote Rust services.
            Developer | Beta Ltd   Jan 2020 – Dec 2021
            • More Rust.
            """);

        // 2018-01..2021-12 is 4 years, not 3 + 2.
        cv.Skills.Should().ContainSingle().Which.YearsOfExperience.Should().Be(4);
    }

    [Fact]
    public void Education_DatesComeLastOrOnlyAYear()
    {
        var cv = Parse("""
            Jane Doe
            EDUCATION
            BSc Computer Science
            University of Limerick
            2010 – 2014
            MBA
            Trinity College Dublin
            Graduated 2018
            """);

        cv.Education.Should().Equal(
            new ParsedEducation("University of Limerick", "BSc Computer Science", 2010, 2014),
            new ParsedEducation("Trinity College Dublin", "MBA", 2018, 2018));
    }

    [Fact]
    public void Education_CurrentStudiesHaveNoEndYear()
    {
        var cv = Parse("Jane Doe\nEDUCATION\nMSc Data Science — University of Porto (2024 – Present)\n");

        cv.Education.Should().ContainSingle().Which.EndYear.Should().BeNull();
    }

    [Fact]
    public void Header_HeadlineOnTheNameLineAndDocumentTitleSkipped()
    {
        var cv = Parse("CURRICULUM VITAE\nJane Doe | Senior Software Engineer\nDublin, Ireland | +353 1 234 5678 | jane@example.com\nSUMMARY\nHello.\n");

        cv.FullName.Should().Be("Jane Doe");
        cv.Headline.Should().Be("Senior Software Engineer");
        cv.Location.Should().Be("Dublin, Ireland");
    }

    [Fact]
    public void MissingSections_ProduceWarningsInsteadOfFailing()
    {
        var cv = Parse("Jane Doe\nSUMMARY\nJust a summary.\n");

        cv.Summary.Should().Be("Just a summary.");
        cv.Skills.Should().BeEmpty();
        cv.Experiences.Should().BeEmpty();
        cv.Warnings.Should().HaveCount(3).And.OnlyContain(w => w.StartsWith("No "));
    }

    [Fact]
    public void Experience_UnresolvableHeaderUsesPlaceholdersAndWarns()
    {
        var cv = Parse("Jane Doe\nEXPERIENCE\nJan 2020 – Present\n• Did things.\n");

        var experience = cv.Experiences.Should().ContainSingle().Subject;
        experience.Company.Should().Be("Unknown company");
        experience.Role.Should().Be("Unknown role");
        cv.Warnings.Should().Contain(w => w.Contains("company")).And.Contain(w => w.Contains("job title"));
    }

    [Fact]
    public void Experience_EndBeforeStartIsClampedAndWarned()
    {
        var cv = Parse("Jane Doe\nEXPERIENCE\nDeveloper | Acme Ltd   Jan 2020 – Jan 2019\n");

        var experience = cv.Experiences.Should().ContainSingle().Subject;
        experience.EndDate.Should().Be(experience.StartDate);
        cv.Warnings.Should().Contain(w => w.Contains("end date is before"));
    }

    [Fact]
    public void BodyLinesWithYearsAreNotMistakenForEntries()
    {
        var cv = Parse("""
            Jane Doe
            EXPERIENCE
            Developer | Acme Ltd   Jan 2020 – Present
            • Grew revenue from 2018 to 2021 by modernising the
            platform between Jan 2019 and Mar 2020 and beyond.
            """);

        cv.Experiences.Should().ContainSingle();
    }

    [Fact]
    public void PageMarkersAreDropped()
    {
        var cv = Parse("Jane Doe\nSKILLS\nLanguages: C#\nPage 1 of 2\nEXPERIENCE\nDeveloper | Acme Ltd   Jan 2020 – Present\nPage 2 of 2\n");

        cv.Skills.Select(s => s.Name).Should().Equal("C#");
        cv.Experiences.Single().Description.Should().BeEmpty();
    }
}
