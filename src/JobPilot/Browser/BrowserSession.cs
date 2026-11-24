using JobPilot.Models;
using Microsoft.Playwright;

namespace JobPilot.Browser;

public sealed class BrowserSession
{
    private readonly string _browserProfilePath;
    private readonly AshbyFormFiller _formFiller;

    public BrowserSession(string browserProfilePath, AshbyFormFiller formFiller)
    {
        _browserProfilePath = browserProfilePath;
        _formFiller = formFiller;
    }

    public async Task<AutofillResult> OpenAndFillAsync(string url, UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Enter a valid http:// or https:// application URL.", nameof(url));

        Directory.CreateDirectory(_browserProfilePath);
        using var playwright = await Playwright.CreateAsync();

        // Use the Edge installation already present on the user's Windows VM.
        // The dedicated profile keeps the login session separate from everyday browsing.
        var context = await playwright.Chromium.LaunchPersistentContextAsync(
            _browserProfilePath,
            new BrowserTypeLaunchPersistentContextOptions
            {
                Channel = "msedge",
                Headless = false,
                Args = new[] { "--start-maximized" }
            });

        try
        {
            var page = context.Pages.FirstOrDefault() ?? await context.NewPageAsync();
            await page.GotoAsync(uri.ToString(), new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 60_000
            });

            Console.WriteLine();
            Console.WriteLine("Browser aperto. Se il portale richiede login, accedi manualmente.");
            Console.WriteLine("Quando il modulo di candidatura è visibile, premi INVIO qui.");
            Console.ReadLine();

            return await _formFiller.FillKnownFieldsAsync(page, profile, cancellationToken);
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine("Controlla i campi compilati nel browser. NON invierò la candidatura.");
            Console.WriteLine("Premi INVIO qui quando hai finito di controllare e/o inviare manualmente.");
            Console.ReadLine();
            await context.CloseAsync();
        }
    }
}
