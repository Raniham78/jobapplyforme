using JobPilot.Browser;
using JobPilot.Data;
using JobPilot.Services;

namespace JobPilot;

internal static class Program
{
    private static async Task<int> Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.Title = "Rani JobPilot";

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RaniJobPilot");

        // Ensure the application data folder exists before SQLite opens its database.
        Directory.CreateDirectory(dataDirectory);

        var profileStore = new ProfileStore(dataDirectory);
        var repository = new ApplicationRepository(profileStore.DatabasePath);
        await repository.InitializeAsync();

        Console.WriteLine("======================================");
        Console.WriteLine("          RANI JOBPILOT");
        Console.WriteLine("======================================");
        Console.WriteLine($"Dati locali: {dataDirectory}");
        Console.WriteLine("Le password non vengono lette o salvate dall'applicazione.");

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. Apri una candidatura e compila i campi riconosciuti");
            Console.WriteLine("2. Visualizza le candidature registrate");
            Console.WriteLine("3. Modifica il mio profilo");
            Console.WriteLine("4. Esci");
            Console.Write("Scelta: ");

            var choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    await RunAutofillAsync(profileStore, repository);
                    break;
                case "2":
                    await ShowApplicationsAsync(repository);
                    break;
                case "3":
                    profileStore.OpenProfileInNotepad();
                    Console.WriteLine($"Profilo aperto: {profileStore.ProfilePath}");
                    Console.WriteLine("Salva le modifiche in Notepad e torna qui.");
                    break;
                case "4":
                    return 0;
                default:
                    Console.WriteLine("Scelta non riconosciuta.");
                    break;
            }
        }
    }

    private static async Task RunAutofillAsync(ProfileStore profileStore,
        ApplicationRepository repository)
    {
        var profile = await profileStore.LoadOrCreateAsync();
        Console.WriteLine();
        Console.WriteLine("Nuova candidatura");
        Console.Write("Azienda: ");
        var company = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("Ruolo: ");
        var role = Console.ReadLine()?.Trim() ?? string.Empty;
        Console.Write("URL della pagina di candidatura: ");
        var url = Console.ReadLine()?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(role) ||
            string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("Azienda, ruolo e URL sono obbligatori. Operazione annullata.");
            return;
        }

        var applicationId = await repository.AddDraftAsync(company, role, url);
        Console.WriteLine($"Candidatura #{applicationId} registrata come Draft.");

        try
        {
            var browser = new BrowserSession(
                profileStore.BrowserProfilePath,
                new AshbyFormFiller());

            var result = await browser.OpenAndFillAsync(url, profile);
            Console.WriteLine();
            Console.WriteLine("Risultato autocompilazione:");
            Console.WriteLine($"  Campi compilati: {result.Filled}");
            Console.WriteLine($"  Campi già valorizzati e lasciati invariati: {result.AlreadyPresent}");
            Console.WriteLine($"  Campi non riconosciuti o saltati: {result.Skipped}");
            Console.WriteLine($"  CV caricato: {(result.ResumeUploaded ? "sì" : "no / non configurato")}");
            Console.WriteLine();
            Console.Write("Hai inviato manualmente la candidatura? (s/N): ");
            var sent = Console.ReadLine()?.Trim();
            if (string.Equals(sent, "s", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sent, "si", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(sent, "sì", StringComparison.OrdinalIgnoreCase))
            {
                await repository.MarkSubmittedAsync(applicationId);
                Console.WriteLine("Stato aggiornato a Submitted.");
            }
            else
            {
                Console.WriteLine("La candidatura resta in stato Draft.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Autocompilazione non completata: {ex.Message}");
            Console.WriteLine("La candidatura resta registrata come Draft.");
        }
    }

    private static async Task ShowApplicationsAsync(ApplicationRepository repository)
    {
        var applications = await repository.GetRecentAsync();
        Console.WriteLine();
        if (applications.Count == 0)
        {
            Console.WriteLine("Non ci sono ancora candidature registrate.");
            return;
        }

        foreach (var item in applications)
        {
            Console.WriteLine($"#{item.Id} | {item.Company} | {item.Role} | {item.Status}");
            Console.WriteLine($"    {item.Url}");
            Console.WriteLine($"    {item.CreatedUtc.ToLocalTime():dd/MM/yyyy HH:mm}");
        }
    }
}
