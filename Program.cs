using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static readonly string LOG_FILE = "rename_log.json";

    static void Main()
    {
        Console.Write("Digite o caminho da pasta com os arquivos: ");
        string pasta = Console.ReadLine()?.Trim() ?? "";

        if (!Directory.Exists(pasta))
        {
            Console.WriteLine("Pasta nao encontrada!");
            return;
        }

        while (true)
        {
            Console.WriteLine("\n=== MENU PRINCIPAL ===");
            Console.WriteLine("1. Renomear arquivos");
            Console.WriteLine("2. Desfazer renomeamento");
            Console.WriteLine("3. Sair");
            Console.Write("Escolha uma opcao: ");

            string opcao = Console.ReadLine()?.Trim() ?? "";

            switch (opcao)
            {
                case "1":
                    RenomearArquivos(pasta);
                    break;
                case "2":
                    DesfazerRenomeamento(pasta);
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Opcao invalida!");
                    break;
            }
        }
    }

    static void RenomearArquivos(string pasta)
    {
        var arquivos = Directory.GetFiles(pasta)
            .OrderBy(f => f)
            .ToArray();

        if (arquivos.Length == 0)
        {
            Console.WriteLine("Nenhum arquivo encontrado na pasta!");
            return;
        }

        int digits = Math.Max(3, arquivos.Length.ToString().Length);
        var mapeamento = new List<(string original, string novo)>();
        var renomearIr = new List<(string origem, string destino)>();

        Console.WriteLine($"\n{arquivos.Length} arquivo(s) encontrado(s)");
        Console.WriteLine("\n=== PREVIA DO RENOMEAMENTO ===");

        int contador = 0;
        for (int i = 0; i < arquivos.Length; i++)
        {
            string nomeOriginal = Path.GetFileName(arquivos[i]);
            string nomeBase = Path.GetFileNameWithoutExtension(arquivos[i]);
            string extensao = Path.GetExtension(arquivos[i]);
            string numero = (i + 1).ToString($"D{digits}");
            string novoNome = $"{nomeBase}{numero}{extensao}";
            string caminhoDestino = Path.Combine(pasta, novoNome);

            if (File.Exists(caminhoDestino) && !caminhoDestino.Equals(arquivos[i], StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"PULADO: {nomeOriginal} -> {novoNome} (ja existe)");
                continue;
            }

            Console.WriteLine($"{i + 1:D3}. {nomeOriginal} -> {novoNome}");
            mapeamento.Add((nomeOriginal, novoNome));
            renomearIr.Add((arquivos[i], caminhoDestino));
            contador++;
        }

        if (contador == 0)
        {
            Console.WriteLine("Nenhum arquivo para renomear!");
            return;
        }

        Console.WriteLine($"\n{contador} arquivo(s) sera(ao) renomeado(s)");
        Console.Write("Deseja continuar? (S/N): ");

        string resposta = Console.ReadLine()?.Trim().ToUpper() ?? "N";
        if (resposta != "S")
        {
            Console.WriteLine("Operacao cancelada.");
            return;
        }

        var temporarios = new List<(string temp, string original)>();

        try
        {
            Console.WriteLine("\nRenomeando...");

            for (int i = 0; i < renomearIr.Count; i++)
            {
                string temp = Path.Combine(pasta, Guid.NewGuid().ToString() + ".tmp");
                File.Move(renomearIr[i].origem, temp, true);
                temporarios.Add((temp, renomearIr[i].origem));
            }

            for (int i = 0; i < renomearIr.Count; i++)
            {
                File.Move(temporarios[i].temp, renomearIr[i].destino, true);
            }

            SalvarLog(pasta, mapeamento);

            Console.WriteLine("Renomeamento concluido com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro durante renomeamento: {ex.Message}");
            foreach (var (temp, original) in temporarios)
            {
                if (File.Exists(temp))
                {
                    File.Move(temp, original, true);
                }
            }
            Console.WriteLine("Arquivos restaurados para o estado anterior.");
        }
    }

    static void DesfazerRenomeamento(string pasta)
    {
        string logPath = Path.Combine(pasta, LOG_FILE);

        if (!File.Exists(logPath))
        {
            Console.WriteLine("Nenhum registro de renomeamento encontrado!");
            return;
        }

        try
        {
            var linhas = File.ReadAllLines(logPath);
            int total = linhas.Length;

            Console.WriteLine($"\nEncontrado registro de {total} arquivo(s)");
            Console.WriteLine("=== PREVIA DE DESFAZIMENTO ===");

            for (int i = 0; i < Math.Min(10, total); i++)
            {
                var partes = linhas[i].Split('|');
                if (partes.Length == 2)
                {
                    Console.WriteLine($"{i + 1}. {partes[1]} -> {partes[0]}");
                }
            }

            if (total > 10)
            {
                Console.WriteLine($"... e mais {total - 10} arquivo(s)");
            }

            Console.Write("\nDeseja desfazer o renomeamento? (S/N): ");
            string resposta = Console.ReadLine()?.Trim().ToUpper() ?? "N";

            if (resposta != "S")
            {
                Console.WriteLine("Operacao cancelada.");
                return;
            }

            Console.WriteLine("\nDesfazendo...");
            int sucessos = 0;

            foreach (var linha in linhas)
            {
                var partes = linha.Split('|');
                if (partes.Length == 2)
                {
                    string nomeOriginal = partes[0];
                    string nomeAtual = partes[1];
                    string caminhoAtual = Path.Combine(pasta, nomeAtual);
                    string caminhoOriginal = Path.Combine(pasta, nomeOriginal);

                    if (File.Exists(caminhoAtual))
                    {
                        File.Move(caminhoAtual, caminhoOriginal, true);
                        sucessos++;
                    }
                }
            }

            File.Delete(logPath);
            Console.WriteLine($"Desfazimento concluido! {sucessos} arquivo(s) restaurado(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao desfazer: {ex.Message}");
        }
    }

    static void SalvarLog(string pasta, List<(string original, string novo)> mapeamento)
    {
        try
        {
            string logPath = Path.Combine(pasta, LOG_FILE);
            var linhas = mapeamento.Select(m => $"{m.original}|{m.novo}");
            File.WriteAllLines(logPath, linhas);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Aviso: Nao foi possivel salvar o log: {ex.Message}");
        }
    }
}