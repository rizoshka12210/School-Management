using Microsoft.Extensions.Localization;

namespace SchoolManagementSystem.Web.Tests;

public sealed class TestStringLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new(name, name, false);

    public LocalizedString this[string name, params object[] arguments] =>
        new(name, string.Format(name, arguments), false);

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        Array.Empty<LocalizedString>();
}
