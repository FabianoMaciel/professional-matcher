# Professional Matcher

Plataforma onde o usuário (inicialmente o próprio autor) mantém um perfil profissional completo, no estilo LinkedIn, mas com foco diferente: registrar **todos** os conhecimentos e experiências, para depois montar currículos sob medida para cada vaga, acompanhar candidaturas e analisar mercados de interesse.

## Estrutura

- `src/Backend` – API .NET (Api, Application, Contracts, Domain, Infra)
- `src/Frontend` – Angular (`professional-matcher-web`)
- `tests` – testes do backend
- `db` – scripts de banco

## Abas / Módulos

### 1. Perfil
Foto, dados gerais, experiências e formações, além de um inventário completo de conhecimentos (tecnologias, ferramentas e práticas), com destaque para o que é principal e registro do que é secundário.

Exemplo (autor):
- Principal: .NET C# (WebForms, MVC, WebAPI), Azure, SQL Server, Oracle, PostgreSQL, MongoDB.
- Também: Python, Java, Angular, Knockout.js, microsserviços, IIS, reporting, stored procedures.

O objetivo é que, conforme a vaga, o currículo possa ser modelado a partir desse inventário.

### 2. Currículos
Várias versões do currículo, cada uma focada num tipo de vaga. Exemplos:
- Fabiano_CV_Fullstack
- Fabiano_CV_Backend
- Fabiano_CV_BackendAzure
- Fabiano_CV_AIAssisted

Cada currículo seleciona e ordena experiências e conhecimentos do perfil.

### 3. Aplicações
Registro de cada candidatura: empresa, vaga, qual currículo foi usado, e o estágio atual com histórico e status de retorno.

Exemplos:
- Empresa X – Stage 2 – Code assessment enviado
- Empresa Z – Stage 1 – Call com RH, aguardando retorno

### 4. Análise de Mercado
A partir do perfil/currículo, busca dados de mercado em fontes como LinkedIn, Glassdoor e Levels.fyi. O usuário seleciona num dropdown os países de interesse e vê, por país:
- Quantidade de vagas abertas para o perfil
- Pagamento médio (na moeda local / convertido)

Exemplo: Brasil – 5248 vagas, média R$ xxx; Itália – 489; Portugal – 321; Espanha – 593; Luxemburgo – 101.

A sincronização com as fontes é feita no backend (escopo futuro).
