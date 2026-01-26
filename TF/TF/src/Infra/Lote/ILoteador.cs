using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Lote
{
    public interface ILoteador
    {
        IEnumerable<LoteadorPayload> Lotear(IEnumerable<Dictionary<string, object?>> envelopes, Func<Dictionary<string, object?>, DateTimeOffset?>? waterMark = null);
    }
}