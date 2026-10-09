namespace Core.Enums
{
    // Text extraction (PDF text / OCR) of an uploaded document.
    public enum ExtractionStatus
    {
        Pending,
        Running,
        Done,
        Failed,
        NotSupported
    }
}
