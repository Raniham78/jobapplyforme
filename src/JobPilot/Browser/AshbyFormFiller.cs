using System.Text.Json;
using System.Text.RegularExpressions;
using JobPilot.Models;
using Microsoft.Playwright;

namespace JobPilot.Browser;

public sealed record AutofillResult(int Filled, int AlreadyPresent, int Skipped, bool ResumeUploaded);

public sealed class AshbyFormFiller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AutofillResult> FillKnownFieldsAsync(IPage page, UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        var filled = 0;
        var alreadyPresent = 0;
        var skipped = 0;
        var resumeUploaded = false;

        // Upload the CV only when the profile points to an existing local file.
        if (!string.IsNullOrWhiteSpace(profile.ResumePath) && File.Exists(profile.ResumePath))
        {
            var fileInputs = page.Locator("input[type='file']");
            var count = await fileInputs.CountAsync();
            for (var i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var input = fileInputs.Nth(i);
                try
                {
                    await input.SetInputFilesAsync(profile.ResumePath);
                    resumeUploaded = true;
                    break;
                }
                catch (PlaywrightException)
                {
                    // Some forms expose hidden file inputs that are not the resume picker.
                    skipped++;
                }
            }
        }

        var fields = page.Locator("input, textarea");
        var fieldCount = await fields.CountAsync();

        for (var i = 0; i < fieldCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var field = fields.Nth(i);

            string snapshotJson;
            try
            {
                snapshotJson = await field.EvaluateAsync<string>("""
                    el => JSON.stringify({
                      tag: (el.tagName || '').toLowerCase(),
                      type: (el.getAttribute('type') || 'text').toLowerCase(),
                      label: Array.from(el.labels || []).map(x => x.innerText || '').join(' '),
                      ariaLabel: el.getAttribute('aria-label') || '',
                      placeholder: el.getAttribute('placeholder') || '',
                      name: el.getAttribute('name') || '',
                      id: el.getAttribute('id') || '',
                      autocomplete: el.getAttribute('autocomplete') || '',
                      value: el.value || '',
                      disabled: !!el.disabled,
                      readOnly: !!el.readOnly,
                      visible: !!(el.offsetWidth || el.offsetHeight || el.getClientRects().length)
                    })
                    """);
            }
            catch (PlaywrightException)
            {
                skipped++;
                continue;
            }

            var info = JsonSerializer.Deserialize<FieldSnapshot>(snapshotJson, JsonOptions);
            if (info is null || !info.Visible || info.Disabled || info.ReadOnly ||
                info.Type is "hidden" or "file" or "checkbox" or "radio" or "submit" or "button" or "password" or "image")
            {
                skipped++;
                continue;
            }

            var currentValue = info.Value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(currentValue))
            {
                alreadyPresent++;
                continue;
            }

            var value = MatchValue(info, profile);
            if (string.IsNullOrWhiteSpace(value))
            {
                skipped++;
                continue;
            }

            try
            {
                await field.FillAsync(value, new LocatorFillOptions { Timeout = 2500 });
                filled++;
            }
            catch (PlaywrightException)
            {
                skipped++;
            }
        }

        return new AutofillResult(filled, alreadyPresent, skipped, resumeUploaded);
    }

    private static string? MatchValue(FieldSnapshot field, UserProfile profile)
    {
        var hint = string.Join(' ', field.Label, field.AriaLabel, field.Placeholder,
            field.Name, field.Id, field.Autocomplete).ToLowerInvariant();
        var type = field.Type.ToLowerInvariant();

        if (type == "email" || Has(hint, "email", "e-mail")) return profile.Email;
        if (type == "tel" || Has(hint, "phone", "telephone", "mobile", "cell phone")) return profile.Phone;

        if (Has(hint, "linkedin")) return profile.LinkedInUrl;
        if (Has(hint, "github")) return profile.GitHubUrl;
        if (Has(hint, "portfolio", "personal website", "website url", "personal url")) return profile.PortfolioUrl;

        if (Has(hint, "first name", "given name", "firstname", "given-name")) return profile.FirstName;
        if (Has(hint, "last name", "family name", "surname", "lastname", "family-name")) return profile.LastName;

        if (Has(hint, "country", "country/region")) return profile.Country;
        if (Has(hint, "city", "current location", "location", "where are you based")) return profile.City;

        if (Has(hint, "years of experience", "years experience", "professional experience"))
            return profile.YearsOfExperience > 0 ? profile.YearsOfExperience.ToString() : string.Empty;

        if (Has(hint, "current title", "current job title", "current position", "job title"))
            return profile.CurrentTitle;

        if (IsFullNameField(hint)) return profile.FullName;
        return null;
    }

    private static bool IsFullNameField(string hint)
    {
        if (hint.Contains("company", StringComparison.Ordinal) ||
            hint.Contains("school", StringComparison.Ordinal) ||
            hint.Contains("organization", StringComparison.Ordinal))
            return false;

        return Regex.IsMatch(hint, @"(^|\s)(full name|your name|legal name|name)(\s|$)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool Has(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    private sealed class FieldSnapshot
    {
        public string Tag { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string AriaLabel { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string Autocomplete { get; set; } = string.Empty;
        public string? Value { get; set; }
        public bool Disabled { get; set; }
        public bool ReadOnly { get; set; }
        public bool Visible { get; set; }
    }
}
