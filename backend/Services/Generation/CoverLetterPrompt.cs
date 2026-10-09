using System.Globalization;
using System.Text;
using System.Text.Json;
using Core.Entities;
using Services.Ports;

namespace Services.Generation
{
    // Builds the prompt for one application and reads the model's JSON answer.
    internal static class CoverLetterPrompt
    {
        // Keeps the prompt within the context window even with many long documents.
        private const int MaxCharsPerDocument = 4000;

        public static IReadOnlyList<ChatMessage> Build(Profile profile, Application application, IEnumerable<Document> documents)
        {
            var language = LanguageName(application.Language);
            var system = $$"""
                You write job applications for the applicant described below.
                Write in {{language}} (language code "{{application.Language}}"), regardless of the language of the job posting or the documents.
                Use only facts from the applicant's profile and documents. Never invent experience, degrees, grades or certificates.
                Tailor the letter to the job posting: pick the 2-3 most relevant qualifications and connect them to the requirements.
                Tone: professional, confident, concrete. Avoid clichés and empty phrases.

                Answer with JSON only, no markdown, exactly these keys:
                {"coverLetter": "...", "emailSubject": "...", "emailBody": "..."}
                - coverLetter: the letter from the salutation to the closing and the applicant's name. No address block, no date, no subject line. 250-400 words.
                - emailSubject: a short subject line for the application email.
                - emailBody: a short email (60-120 words) that refers to the attached cover letter and documents, with salutation and closing.
                Address the recipient by name if one is given, otherwise use a neutral salutation.
                """;

            var user = new StringBuilder();
            user.AppendLine("## Applicant");
            user.AppendLine($"Name: {profile.FirstName} {profile.LastName}");
            AppendIfSet(user, "Location", profile.City);
            AppendIfSet(user, "Desired position", profile.DesiredPosition);
            AppendIfSet(user, "About", profile.Summary);

            if (profile.WorkExperiences.Count > 0)
            {
                user.AppendLine("\n### Work experience");
                foreach (var w in profile.WorkExperiences.OrderByDescending(w => w.StartDate))
                    user.AppendLine($"- {w.Role} at {w.Company} ({w.StartDate:yyyy-MM} – {(w.EndDate?.ToString("yyyy-MM") ?? "today")}){Suffix(w.Description)}");
            }
            if (profile.Educations.Count > 0)
            {
                user.AppendLine("\n### Education");
                foreach (var e in profile.Educations.OrderByDescending(e => e.StartDate))
                    user.AppendLine($"- {e.Degree}{Suffix(e.FieldOfStudy, ", ")} at {e.Institution}{Suffix(e.Grade, ", grade ")}");
            }
            if (profile.Skills.Count > 0)
                user.AppendLine($"\n### Skills\n{string.Join(", ", profile.Skills.Select(s => s.Level == null ? s.Name : $"{s.Name} ({s.Level})"))}");
            if (profile.Languages.Count > 0)
                user.AppendLine($"\n### Languages\n{string.Join(", ", profile.Languages.Select(l => $"{l.Name} ({l.Level})"))}");

            foreach (var document in documents.Where(d => !string.IsNullOrWhiteSpace(d.ExtractedText)))
            {
                var text = document.ExtractedText!;
                user.AppendLine($"\n### Document: {document.DisplayName} ({document.Type})");
                user.AppendLine(text.Length > MaxCharsPerDocument ? text[..MaxCharsPerDocument] + " […]" : text);
            }

            var job = application.JobPosting;
            user.AppendLine("\n## Job posting");
            user.AppendLine($"Title: {job.Title}");
            user.AppendLine($"Company: {job.CompanyName}");
            AppendIfSet(user, "Location", job.Location);
            AppendIfSet(user, "Recipient", application.RecipientName);
            user.AppendLine($"Description:\n{job.Description}");

            return [ChatMessage.System(system), ChatMessage.User(user.ToString())];
        }

        public static GeneratedLetter Parse(string answer)
        {
            // Models sometimes wrap JSON in ```json fences or add a sentence around it.
            var start = answer.IndexOf('{');
            var end = answer.LastIndexOf('}');
            if (start < 0 || end <= start)
                throw new AiRequestException("The AI answer did not contain the expected JSON.");

            try
            {
                var letter = JsonSerializer.Deserialize<GeneratedLetter>(answer[start..(end + 1)], new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (letter == null || string.IsNullOrWhiteSpace(letter.CoverLetter) || string.IsNullOrWhiteSpace(letter.EmailSubject) || string.IsNullOrWhiteSpace(letter.EmailBody))
                    throw new AiRequestException("The AI answer is missing the letter, subject or email text.");
                return letter with { CoverLetter = letter.CoverLetter.Trim(), EmailSubject = letter.EmailSubject.Trim(), EmailBody = letter.EmailBody.Trim() };
            }
            catch (JsonException ex)
            {
                throw new AiRequestException("The AI answer was not valid JSON.", ex);
            }
        }

        private static string LanguageName(string code)
        {
            try
            {
                return CultureInfo.GetCultureInfo(code).EnglishName;
            }
            catch (CultureNotFoundException)
            {
                return code;
            }
        }

        private static void AppendIfSet(StringBuilder sb, string label, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                sb.AppendLine($"{label}: {value}");
        }

        private static string Suffix(string? value, string separator = ": ") =>
            string.IsNullOrWhiteSpace(value) ? "" : separator + value;
    }

    internal record GeneratedLetter(string CoverLetter, string EmailSubject, string EmailBody);
}
