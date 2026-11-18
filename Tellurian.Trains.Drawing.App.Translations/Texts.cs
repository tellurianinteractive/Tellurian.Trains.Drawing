using Microsoft.Extensions.DependencyInjection;
using Tellurian.Localization;
using Tellurian.Trains.Drawing.App.Translations.Resources;

namespace Tellurian.Trains.Drawing.App.Translations;

/// <summary>The app's UI texts in the current UI culture, e.g. <c>@T["Save"]</c> in markup.</summary>
public sealed class Texts([FromKeyedServices(Texts.ResxProviders)] IResourceProviderGroup resx)
{
    public const string ResxProviders = "Resx";

    public string this[string key] => resx.Translated<AppTexts>(key).Text;

    /// <summary>
    /// The help text for a field, the <c>{key}Help</c> resource, e.g. <c>MinimumAisleWidthHelp</c>; null when there is
    /// none, so the field shows no "?".
    /// </summary>
    public string? Help(string key)
    {
        var helpKey = $"{key}Help";
        var text = this[helpKey];
        return string.IsNullOrWhiteSpace(text) || text == helpKey ? null : text;
    }

    /// <summary>A text with placeholders, formatted in the current culture.</summary>
    public string Format(string key, params object?[] args) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, this[key], args);
}
