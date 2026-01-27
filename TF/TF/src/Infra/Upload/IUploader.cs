namespace TF.src.Infra.Upload
{
    public interface IUploader
    {
        Task UploadPhp(LoteadorPayload lote, CancellationToken comando = default);
    }
}