using MimeKit;
using Services.Ports;

namespace Infrastructure.Export
{
    // RFC 822 message with "X-Unsent: 1": Outlook opens it as a draft ready to send (Thunderbird via
    // "Edit as new message"). Unlike mailto:, this carries attachments.
    public class EmlDraftBuilder : IEmailDraftBuilder
    {
        public byte[] Build(EmailDraft draft)
        {
            var message = new MimeMessage { Subject = draft.Subject };
            message.Headers.Add("X-Unsent", "1");
            if (!string.IsNullOrWhiteSpace(draft.From))
                message.From.Add(MailboxAddress.Parse(draft.From));
            if (!string.IsNullOrWhiteSpace(draft.To))
                message.To.Add(MailboxAddress.Parse(draft.To));

            var body = new BodyBuilder { TextBody = draft.Body };
            foreach (var attachment in draft.Attachments)
                body.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.MimeType));
            message.Body = body.ToMessageBody();

            using var stream = new MemoryStream();
            message.WriteTo(stream);
            return stream.ToArray();
        }
    }
}
