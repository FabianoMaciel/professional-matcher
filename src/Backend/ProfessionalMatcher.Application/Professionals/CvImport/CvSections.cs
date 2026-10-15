using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

internal enum CvSectionKind
{
    Summary,
    Skills,
    Experience,
    Education,

    /// <summary>Recognised but not imported (certifications, languages, achievements...). Also ends the previous section.</summary>
    Ignored,
}

internal sealed record CvSection(CvSectionKind Kind, string Title, List<CvLine> Lines);

internal sealed record CvDocument(List<CvLine> Header, List<CvSection> Sections);

/// <summary>Splits a CV into its header and titled sections, in English and Portuguese (plus a little Spanish).</summary>
internal static class CvSections
{
    private static readonly Dictionary<string, CvSectionKind> Headings = Build();

    private static readonly Regex InlineHeading = new(
        @"^(?<heading>[^:|]{3,40}):\s*(?<rest>\S.*)$", RegexOptions.Compiled);

    public static CvDocument Split(IEnumerable<CvLine> lines)
    {
        var header = new List<CvLine>();
        var sections = new List<CvSection>();

        foreach (var line in lines)
        {
            if (TryHeading(line, out var kind, out var title, out var rest))
            {
                var section = new CvSection(kind, title, []);
                if (rest is not null) section.Lines.Add(CvLine.From(rest));
                sections.Add(section);
            }
            else if (sections.Count == 0)
            {
                header.Add(line);
            }
            else
            {
                sections[^1].Lines.Add(line);
            }
        }

        return new CvDocument(header, sections);
    }

    private static bool TryHeading(CvLine line, out CvSectionKind kind, out string title, out string? inlineRest)
    {
        kind = default;
        title = string.Empty;
        inlineRest = null;
        if (line.IsBullet || line.Text.Length > 80) return false;

        if (Headings.TryGetValue(Key(line.Text), out kind))
        {
            title = line.Text.Trim(' ', ':', '-', '–', '—', '_', '=', '*', '#', '|');
            return true;
        }

        // "Skills: C#, Java" or "Summary: Senior engineer..." — a heading with its content on the same line.
        // Not for Ignored: "Languages: C#, Java" is a skills category far more often than a list of spoken languages.
        var inline = InlineHeading.Match(line.Text);
        if (inline.Success
            && Headings.TryGetValue(Key(inline.Groups["heading"].Value), out kind)
            && kind is CvSectionKind.Summary or CvSectionKind.Skills or CvSectionKind.Education)
        {
            title = inline.Groups["heading"].Value.Trim();
            inlineRest = inline.Groups["rest"].Value;
            return true;
        }

        return false;
    }

    private static string Key(string text)
    {
        var folded = CvText.Fold(text).Replace("&", " and ");
        folded = Regex.Replace(folded, @"[^\p{L}\p{N} ]+", " ");
        return CvText.Collapse(folded);
    }

    private static Dictionary<string, CvSectionKind> Build()
    {
        var map = new Dictionary<string, CvSectionKind>();

        void Add(CvSectionKind kind, params string[] names)
        {
            foreach (var name in names) map[Key(name)] = kind;
        }

        Add(CvSectionKind.Summary,
            "summary", "professional summary", "career summary", "executive summary", "profile", "professional profile",
            "about", "about me", "objective", "career objective", "personal statement",
            "resumo", "resumo profissional", "perfil", "perfil profissional", "sobre mim", "objetivo", "objetivo profissional",
            "resumen", "perfil profesional");

        Add(CvSectionKind.Skills,
            "skills", "technical skills", "core skills", "key skills", "technical competencies", "core competencies",
            "competencies", "technologies", "technology", "tech stack", "technical stack", "skills and tools",
            "tools and technologies", "technical expertise", "expertise", "areas of expertise", "technical proficiencies",
            "proficiencies", "skills summary",
            "habilidades", "habilidades tecnicas", "competencias", "competencias tecnicas", "conhecimentos",
            "conhecimentos tecnicos", "tecnologias", "habilidades e competencias", "competencias e habilidades",
            "habilidades tecnicas e ferramentas");

        Add(CvSectionKind.Experience,
            "experience", "work experience", "professional experience", "job experience", "job history",
            "employment history", "work history", "career history", "employment", "professional background",
            "relevant experience",
            "experiencia", "experiencias", "experiencia profissional", "experiencias profissionais",
            "experiencia de trabalho", "historico profissional", "historico de trabalho", "historico de empregos",
            "atuacao profissional", "experiencia laboral");

        Add(CvSectionKind.Education,
            "education", "academic background", "academic education", "education and training",
            "academic qualifications", "qualifications", "educational background",
            "educacao", "formacao", "formacao academica", "escolaridade", "educacao e formacao",
            "formacao academica e complementar", "educacion", "formacion academica");

        Add(CvSectionKind.Ignored,
            "certifications", "certification", "licenses", "licenses and certifications", "certificacoes", "certificados",
            "languages", "language skills", "idiomas",
            "key achievements", "achievements", "accomplishments", "highlights", "key highlights", "conquistas",
            "principais conquistas",
            "projects", "personal projects", "projetos", "publications", "publicacoes", "awards", "honors",
            "interests", "hobbies", "interesses", "references", "referencias",
            "volunteer", "volunteering", "voluntariado", "courses", "cursos", "training", "treinamentos",
            "patents", "additional information", "informacoes adicionais", "contact", "contato", "contacto");

        return map;
    }
}
