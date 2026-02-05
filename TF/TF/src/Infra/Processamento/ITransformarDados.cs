using TF.src.Infra.Modelo;

namespace TF.src.Infra.Processamento
{
    public interface ITransformarDados
    {
        IEnumerable<ApiLinha> Transformar(string tabelaChave, IEnumerable<ApiLinha> linhas);
    }
}