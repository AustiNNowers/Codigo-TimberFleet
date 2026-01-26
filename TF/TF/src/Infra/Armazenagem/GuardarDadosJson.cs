
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Components.Endpoints;

namespace TF.src.Infra.Armazenagem
{
    public class GuardarDadosJson(string caminhoArquivo) : IGuardarDados
    {
        private readonly string _caminhoArquivo = caminhoArquivo ?? throw new ArgumentNullException(nameof(caminhoArquivo));
        private readonly SemaphoreSlim _mutex = new(1, 1);

        private readonly JsonSerializerOptions _opcoes = new() { WriteIndented = true };

        public async Task SalvarDados(string tabela, string dados, CancellationToken comando = default)
        {
            await _mutex.WaitAsync(comando);
            try
            {
                JsonObject root;

                if (File.Exists(_caminhoArquivo))
                {
                    root = JsonNode.Parse(await File.ReadAllTextAsync(_caminhoArquivo, comando)) as JsonObject ?? [];
                }
                else
                {
                    root = [];
                }

                if (root["Tabelas"] is not JsonObject tabelas)
                {
                    tabelas = [];
                    root["Tabelas"] = tabelas;
                }

                if(tabelas[tabela] is not JsonObject tabelaObj)
                {
                    tabelaObj = [];
                    tabelas[tabela] = tabelaObj;
                }

                tabelaObj["UltimaAtualizacao"] = dados;
                var atualizacao = root.ToJsonString(_opcoes);
                await File.WriteAllTextAsync(_caminhoArquivo, atualizacao, comando);
            }
            finally
            {
                _mutex.Release();
            }
        }

        public async Task<string?> ObterDados(string tabela, CancellationToken comando = default)
        {
            await _mutex.WaitAsync(comando);
            try
            {
                if (!File.Exists(_caminhoArquivo)) return null;

                var root = JsonNode.Parse(await File.ReadAllTextAsync(_caminhoArquivo, comando)) as JsonObject;
                if (root is null) return null;

                var tabelas = root["Tabelas"] as JsonObject;
                var tabelaObj = tabelas?[tabela] as JsonObject;
                if (tabelaObj is null) return null;

                var ultimaData = tabelaObj["UltimaAtualizacao"];
                return ultimaData?.GetValue<string>();
            }
            finally
            {
                _mutex.Release();
            }
        }
    }
}