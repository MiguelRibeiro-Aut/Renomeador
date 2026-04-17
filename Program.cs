using System;
using System.IO;

class Program
{
    static void Main()
    {
        string oldPath = @"C:\Users\migue\Downloads\renomeador";
        var arquivos = Directory.GetFiles(oldPath);

        var temporarios = new string[arquivos.Length];
        for (int i = 0; i < arquivos.Length; i++)
        {
            var temp = Path.Combine(oldPath, Guid.NewGuid().ToString() + ".tmp");
            File.Move(arquivos[i], temp);
            temporarios[i] = temp;
        }

        for (int i = 0; i < temporarios.Length; i++)
        {
            int numero = i + 1;
            string nomeArquivo = numero < 10 ? $"teste00{numero}.png"
                                : numero < 100 ? $"teste0{numero}.png"
                                : $"teste{numero}.png";
            string destino = Path.Combine(oldPath, nomeArquivo);
            File.Move(temporarios[i], destino);
        }
    }
}