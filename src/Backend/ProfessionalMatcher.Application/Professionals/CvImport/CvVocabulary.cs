using System.Text.RegularExpressions;

namespace ProfessionalMatcher.Application.Professionals.CvImport;

/// <summary>
/// Word lists used to tell apart the pieces of an entry header (job title vs company, degree vs institution)
/// and to recognise locations. English and Portuguese, with a few Spanish terms.
/// </summary>
internal static class CvVocabulary
{
    private static readonly HashSet<string> RoleWords =
    [
        "engineer", "developer", "programmer", "analyst", "architect", "manager", "lead", "leader", "director",
        "head", "consultant", "specialist", "administrator", "admin", "designer", "tester", "intern", "internship",
        "trainee", "officer", "coordinator", "supervisor", "scientist", "researcher", "owner", "founder", "cto",
        "ceo", "cio", "vp", "president", "technician", "assistant", "advisor", "executive", "representative",
        "sre", "devops", "qa", "swe", "fullstack", "analista", "desenvolvedor", "desenvolvedora", "engenheiro",
        "engenheira", "programador", "programadora", "gerente", "arquiteto", "arquiteta", "consultor",
        "consultora", "especialista", "administrador", "administradora", "estagiario", "estagiaria",
        "coordenador", "coordenadora", "lider", "tecnico", "tecnica", "diretor", "diretora", "pesquisador",
        "assistente", "auxiliar", "gestor", "gestora", "ingeniero", "desarrollador",
    ];

    private static readonly HashSet<string> CompanyWords =
    [
        "inc", "ltd", "ltda", "llc", "plc", "corp", "corporation", "co", "company", "gmbh", "ag", "sa", "srl",
        "spa", "bv", "nv", "oy", "ab", "group", "grupo", "holdings", "technologies", "technology", "tecnologia",
        "solutions", "solucoes", "systems", "sistemas", "software", "labs", "consulting", "consultoria",
        "services", "servicos", "partners", "bank", "banco", "studio", "agency", "agencia", "cia",
    ];

    private static readonly HashSet<string> DegreeWords =
    [
        "degree", "bachelor", "bachelors", "master", "masters", "msc", "bsc", "mba", "phd", "doctorate",
        "diploma", "specialization", "specialisation", "postgraduate", "graduate", "undergraduate", "associate",
        "technologist", "bacharelado", "bacharel", "licenciatura", "mestrado", "doutorado", "graduacao",
        "posgraduacao", "pos", "especializacao", "tecnologo", "tecnologia", "engineering", "engenharia",
        "bootcamp", "licenciado", "grado", "maestria",
    ];

    private static readonly HashSet<string> InstitutionWords =
    [
        "university", "universidade", "universidad", "universitat", "college", "school", "institute",
        "instituto", "faculdade", "academy", "academia", "escola", "centro", "polytechnic", "politecnico",
        "uni", "education", "educacao", "grupo", "fatec", "senai", "senac", "usp", "unicamp", "mit",
    ];

    private static readonly HashSet<string> Countries =
    [
        "ireland", "irlanda", "brazil", "brasil", "portugal", "spain", "espanha", "españa", "italy", "italia",
        "germany", "alemanha", "france", "franca", "uk", "united kingdom", "usa", "united states", "canada",
        "netherlands", "luxembourg", "luxemburgo", "india", "poland", "remote", "hybrid", "on-site", "onsite",
        "remoto", "hibrido",
    ];

    public static int RoleScore(string part) => Tokens(part).Count(RoleWords.Contains);

    public static int CompanyScore(string part) => Tokens(part).Count(CompanyWords.Contains);

    public static int DegreeScore(string part) => Tokens(part).Count(DegreeWords.Contains);

    public static int InstitutionScore(string part) => Tokens(part).Count(InstitutionWords.Contains);

    /// <summary>"Dublin, Ireland", "São Paulo - SP", "Remote": text that says where, not what or who.</summary>
    public static bool LooksLikeLocation(string part)
    {
        var folded = CvText.Fold(part).Trim(' ', '.', ',');
        if (Countries.Contains(folded)) return true;

        // Anything that names a job, company, degree or school is not (only) a place: "Grupo Uniftec, Brazil".
        var namesSomethingElse = RoleScore(part) + CompanyScore(part) + DegreeScore(part) + InstitutionScore(part) > 0;
        if (namesSomethingElse) return false;

        if (Countries.Any(c => folded.EndsWith(", " + c, StringComparison.Ordinal) || folded.EndsWith(" - " + c, StringComparison.Ordinal)))
            return true;

        return part.Contains(',') && CvText.WordCount(part) <= 4 && Regex.IsMatch(part, @"^[\p{L}.' -]+,\s*[\p{L}.' -]+$");
    }

    private static HashSet<string> Tokens(string part) => CvText.Tokens(part);
}
