# Professional Matcher

A platform where the user (initially the author) keeps a complete professional profile, LinkedIn-style, with a different focus: record **everything** they know and have done, then build tailor-made résumés for each job, track applications, and analyse the markets they are interested in.

## Structure

- `src/Backend` – .NET API (Api, Application, Contracts, Domain, Infra)
- `src/Frontend` – Angular app (`professional-matcher-web`)
- `tests` – backend tests
- `db` – SQL migration scripts (applied by the API on startup)
- `docs/prototypes` – screen mockups
- `docker-compose.yml` – local PostgreSQL

## Getting started

```powershell
docker compose up -d                                                              # PostgreSQL on localhost:6538
dotnet run --project src\Backend\ProfessionalMatcher.Api --launch-profile http    # API on http://localhost:5000
cd src\Frontend; npm install; npm start                                           # UI on http://localhost:4200
```

The frontend runs on an in-memory mock API by default. Set `useMocks: false` in
`src/Frontend/src/environments/environment.development.ts` to use the real backend.

## Tabs / Modules

### 1. Profile
Photo, general data, experiences and education, plus a complete inventory of knowledge (technologies, tools and practices), highlighting what is primary and recording what is secondary.

Example (author):
- Primary: .NET C# (WebForms, MVC, WebAPI), Azure, SQL Server, Oracle, PostgreSQL, MongoDB.
- Also: Python, Java, Angular, Knockout.js, microservices, IIS, reporting, stored procedures.

The goal is that, depending on the job, a résumé can be shaped from this inventory.

The profile can be populated from an existing résumé in PDF (`POST /api/v1/professionals/{id}/cv-import`). The import is rule-based (no AI): it recognises sections such as Experience / Job History and Technical Skills / Education in English or Portuguese, reads job titles, companies and date ranges, and estimates each skill's years and level from the jobs that mention it. Existing data is never removed, and entries already on the profile are skipped.

### 2. Résumés
Several versions of the résumé, each focused on a type of job. Examples:
- Fabiano_CV_Fullstack
- Fabiano_CV_Backend
- Fabiano_CV_BackendAzure
- Fabiano_CV_AIAssisted

Each résumé selects and orders experiences and skills from the profile.

### 3. Applications
A record of each application: company, position, which résumé was used, and the current stage with its history and whether a reply is pending.

Examples:
- Company X – Stage 2 – Code assessment submitted
- Company Z – Stage 1 – HR call, waiting for an answer

### 4. Market Analysis
From the profile/résumé, collects market data from sources such as LinkedIn, Glassdoor and Levels.fyi. The user picks the countries of interest from a dropdown and sees, per country:
- Number of open jobs matching the profile
- Average pay (in local currency / converted)

Example: Brazil – 5,248 jobs, average R$ xxx; Italy – 489; Portugal – 321; Spain – 593; Luxembourg – 101.

Syncing with the sources is done in the backend (future scope).
