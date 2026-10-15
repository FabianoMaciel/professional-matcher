-- The app has no authentication yet and works on a single professional.
-- This id must match `currentProfessionalId` in the frontend environment files.
INSERT INTO professionals (id, full_name, email, headline, location, summary)
VALUES ('00000000-0000-0000-0000-000000000001', 'Fabiano Maciel', 'fmaciel@glasslewis.com',
        'Senior Software Engineer · .NET', 'Brasil', '')
ON CONFLICT (id) DO NOTHING;
