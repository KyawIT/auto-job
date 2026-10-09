using Core.Enums;

namespace Services.Ports
{
    public record LetterAddress(string Name, IReadOnlyList<string> Lines);

    // Everything needed to lay out one cover letter. Body = the letter from salutation to signature.
    public record LetterContent(
        LetterAddress Sender,
        LetterAddress Recipient,
        string? Place,
        DateOnly Date,
        string Subject,
        string Body,
        string Language,
        LetterLayout Layout);

    public interface ICoverLetterRenderer
    {
        byte[] RenderPdf(LetterContent letter);
        byte[] RenderDocx(LetterContent letter);
    }

    public record EmailAttachment(string FileName, string MimeType, byte[] Content);

    public record EmailDraft(string? From, string? To, string Subject, string Body, IReadOnlyList<EmailAttachment> Attachments);

    // An .eml file that mail clients open as an unsent, ready-to-send draft.
    public interface IEmailDraftBuilder
    {
        byte[] Build(EmailDraft draft);
    }
}
