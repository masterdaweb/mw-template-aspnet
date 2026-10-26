namespace MwApp;

/// <summary>The page in the visitor's language: Portuguese when the browser prefers it, English otherwise.</summary>
public sealed record Texts(
    string Lang,
    string Title,
    string Lead,
    string Reaches,
    string Send,
    string File,
    string Submit,
    Func<string, string> Sent,
    string Empty,
    string Failed,
    string None,
    string Download,
    string Api,
    string Next,
    string Step1,
    string Step2,
    string Step3,
    string NoDatabase,
    string FormUnavailable)
{
    static readonly Texts En = new(
        "en",
        "Your ASP.NET Core app is live",
        "ASP.NET Core with PostgreSQL through Entity Framework Core and a private bucket: files sent below are stored in the bucket, described in the database, and downloaded through links that expire in 15 minutes.",
        "What this app reaches",
        "Send a file",
        "File",
        "Send to Object Storage",
        name => $"{name} was saved.",
        "Choose a file first.",
        "The file could not be saved. See the service logs.",
        "No files yet.",
        "download (link valid for 15 min)",
        "The same as JSON",
        "Next steps",
        "Make the code yours: in the console, open this project and follow “Your code” to copy it to your GitHub and connect it.",
        "Change the model in src/MwApp/AppDb.cs, add a migration (dotnet ef migrations add Name) and push: it runs once, before the new version takes traffic.",
        "Add your own domain on the web service, under Domains.",
        "No PostgreSQL reachable. On Master da Web, open this service in the console and add a PostgreSQL database under Resources: it arrives as ConnectionStrings__DefaultConnection, and the next deploy runs the migrations.",
        "Sending files needs the database: it keeps the keys the form is sealed with.");

    static readonly Texts Pt = new(
        "pt-BR",
        "Seu app ASP.NET Core está no ar",
        "ASP.NET Core com PostgreSQL pelo Entity Framework Core e um bucket privado: os arquivos enviados abaixo ficam no bucket, são descritos no banco e baixados por links que expiram em 15 minutos.",
        "O que este app alcança",
        "Envie um arquivo",
        "Arquivo",
        "Enviar para o Object Storage",
        name => $"{name} foi salvo.",
        "Escolha um arquivo primeiro.",
        "Não foi possível salvar o arquivo. Veja os logs do serviço.",
        "Nenhum arquivo ainda.",
        "baixar (link válido por 15 min)",
        "O mesmo em JSON",
        "Próximos passos",
        "Torne o código seu: no console, abra este projeto e siga “Seu código” para copiá-lo para o seu GitHub e conectá-lo.",
        "Altere o modelo em src/MwApp/AppDb.cs, crie uma migração (dotnet ef migrations add Nome) e faça push: ela roda uma vez, antes de a nova versão receber tráfego.",
        "Adicione o seu domínio no serviço web, em Domínios.",
        "Nenhum PostgreSQL alcançável. Na Master da Web, abra este serviço no console e adicione um banco PostgreSQL em Recursos: ele chega como ConnectionStrings__DefaultConnection, e o próximo deploy roda as migrações.",
        "O envio de arquivos precisa do banco: ele guarda as chaves que selam o formulário.");

    public static Texts For(string? acceptLanguage) =>
        (acceptLanguage ?? "").Split(',')[0].Trim().StartsWith("pt", StringComparison.OrdinalIgnoreCase) ? Pt : En;
}
