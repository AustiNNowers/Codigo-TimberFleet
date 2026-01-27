namespace TF.src.Infra.Processamento
{
    public class OpcoesLimpeza
    {
        public bool NormalizarUpdateAtIso { get; init; } = true;
        public bool RemoverEspaços { get; init; } = true;
        public bool RemoverStringsVazias { get; init; } = true;
        public bool ParseNumerosString { get; init; } = true;
        public bool ParseBooleanosString { get; init; } = true;
        public bool ParseDatasString { get; init; } = true;
        public string[] ChaveDatas { get; init; } = ["date", "data", "created", "update"];
    }
}