using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TF.src.Infra.Lote;

namespace TF.src.Infra.Upload
{
    public interface IUploader
    {
        Task UploadPhp(LoteadorPayload lote, CancellationToken comando = default);
    }
}