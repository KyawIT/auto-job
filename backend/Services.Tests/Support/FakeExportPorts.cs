using System.Text;
using Services.Ports;

namespace Services.Tests.Support
{
    public class FakeCoverLetterRenderer : ICoverLetterRenderer
    {
        public List<LetterContent> Rendered { get; } = new();

        public byte[] RenderPdf(LetterContent letter) => Render("PDF", letter);

        public byte[] RenderDocx(LetterContent letter) => Render("DOCX", letter);

        private byte[] Render(string format, LetterContent letter)
        {
            Rendered.Add(letter);
            return Encoding.UTF8.GetBytes($"{format}:{letter.Body}");
        }
    }

    public class FakeEmailDraftBuilder : IEmailDraftBuilder
    {
        public List<EmailDraft> Built { get; } = new();

        public byte[] Build(EmailDraft draft)
        {
            Built.Add(draft);
            return Encoding.UTF8.GetBytes($"EML:{draft.Subject}");
        }
    }
}
