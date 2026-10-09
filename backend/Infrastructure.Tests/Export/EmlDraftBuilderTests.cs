using System.Text;
using Infrastructure.Export;
using MimeKit;
using Services.Ports;

namespace Infrastructure.Tests.Export
{
    public class EmlDraftBuilderTests
    {
        [Fact]
        public void Draft_is_marked_unsent_and_carries_text_and_attachments()
        {
            var draft = new EmailDraft(
                From: "anna@example.com",
                To: "jobs@contoso.example",
                Subject: "Bewerbung als Backend Developer",
                Body: "Sehr geehrte Frau Müller,\n\nanbei meine Unterlagen.",
                Attachments:
                [
                    new EmailAttachment("Anschreiben.pdf", "application/pdf", Encoding.UTF8.GetBytes("%PDF letter")),
                    new EmailAttachment("Zeugnis.pdf", "application/pdf", Encoding.UTF8.GetBytes("%PDF zeugnis")),
                ]);

            var message = MimeMessage.Load(new MemoryStream(new EmlDraftBuilder().Build(draft)));

            Assert.Equal("1", message.Headers["X-Unsent"]);
            Assert.Equal("jobs@contoso.example", message.To.Mailboxes.Single().Address);
            Assert.Equal("anna@example.com", message.From.Mailboxes.Single().Address);
            Assert.Equal("Bewerbung als Backend Developer", message.Subject);
            Assert.Contains("anbei meine Unterlagen.", message.TextBody);
            Assert.Equal(["Anschreiben.pdf", "Zeugnis.pdf"], message.Attachments.OfType<MimePart>().Select(a => a.FileName));

            using var content = new MemoryStream();
            message.Attachments.OfType<MimePart>().Last().Content!.DecodeTo(content);
            Assert.Equal("%PDF zeugnis", Encoding.UTF8.GetString(content.ToArray()));
        }

        [Fact]
        public void Draft_without_recipient_is_still_created()
        {
            var draft = new EmailDraft(From: null, To: null, "Bewerbung", "Text", []);

            var message = MimeMessage.Load(new MemoryStream(new EmlDraftBuilder().Build(draft)));

            Assert.Empty(message.To);
            Assert.Equal("Bewerbung", message.Subject);
        }
    }
}
