using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TF.src.Infra.Logging;
using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento
{
    public class TransformarDados(ILimparDados limpador, IConsoleLogger log, int dop = 1, CancellationToken comando = default) : ITransformarDados
    {
        private readonly ILimparDados _limpador = limpador ?? throw new ArgumentNullException(nameof(limpador));
        private readonly IConsoleLogger _log = log ?? throw new ArgumentNullException(nameof(log));
        private readonly int _dop = Math.Max(1, dop);

        public IEnumerable<ApiLinha> Transformar(string tabelaChave, IEnumerable<ApiLinha> linhas)
        {
            _log.Info("[TransformarDados] Iniciando transformação e limpeza dos dados");
            if (linhas is null) yield break;

            Func<ApiLinha, ApiLinha> aplicar = tabelaChave switch
            {
                "Contadores" => Tabelas.TContadores.Aplicar,
                "Formulario" => Tabelas.TFormulario.Aplicar,
                "Apontamento" => Tabelas.TApontamento.Aplicar,
                "Localizacao" => Tabelas.TLocalizacao.Aplicar,
                "Producao" => Tabelas.TProducao.Aplicar,
                "Produto" => Tabelas.TProduto.Aplicar,
                _ => static x => x
            };

            if (_dop > 1 && linhas is IList<ApiLinha> lista)
            {
                var saida = new ApiLinha[lista.Count];
                Parallel.For(
                    0, lista.Count,
                    new ParallelOptions { MaxDegreeOfParallelism = _dop, CancellationToken = comando },
                    i =>
                    {
                        var limpo = _limpador.Limpar(lista[i]);
                        saida[i] = aplicar(limpo);
                    }
                );

                for (int i = 0; i < saida.Length; i++) yield return saida[i];

                yield break;
            }
        }
    }
}