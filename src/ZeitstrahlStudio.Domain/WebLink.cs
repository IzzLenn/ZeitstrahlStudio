namespace ZeitstrahlStudio.Domain;

/// <summary>Ein bewusst gespeicherter externer Webseitenverweis.</summary>
public sealed record WebLink
{
    /// <summary>Initialisiert einen HTTP- oder HTTPS-Verweis.</summary>
    public WebLink(Guid id, Uri address, string? label = null)
    {
        if (id == Guid.Empty)
        {
            throw new DomainValidationException("Ein Webseitenlink benötigt eine gültige ID.", nameof(id));
        }

        if (!address.IsAbsoluteUri || (address.Scheme != Uri.UriSchemeHttp && address.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainValidationException("Es sind nur absolute HTTP- oder HTTPS-Adressen zulässig.", nameof(address));
        }

        Id = id;
        Address = address;
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }

    public Guid Id { get; }
    public Uri Address { get; }
    public string? Label { get; }
}
