namespace HGTSWebApi.DTOs
{
    public class PassengerImportDto
    {
        public string FullName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? CellNumber { get; set; }
        public string? ResidenceCode { get; set; }
        public string? FacultyName { get; set; }
        public string InstitutionCode { get; set; } = string.Empty;
        public string? NfcUid { get; set; }
    }

    public class PassengerImportResponseDto
    {
        public int TotalProcessed { get; set; }
        public int SuccessfullyImported { get; set; }
        public int Failed { get; set; }
        public List<PassengerImportErrorDto> Errors { get; set; } = new();
    }

    public class PassengerImportErrorDto
    {
        public int Row { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class BulkPassengerImportDto
    {
        public List<PassengerImportDto> Passengers { get; set; } = new();
    }
}