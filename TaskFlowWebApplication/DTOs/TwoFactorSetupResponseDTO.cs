namespace TaskFlowWebApplication.DTOs
{
    public class TwoFactorSetupResponseDTO
    {
        public string SecretKey { get; set; } = string.Empty;

        public string QrCodeBase64 { get; set; } = string.Empty;

        public string ManualEntryKey { get; set; } = string.Empty;
    }
}
