using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento
{
    public interface ILimparDados
    {
        ApiLinha Limpar(ApiLinha linha);
    }
}