#r "nuget: System.DirectoryServices, 8.0.0"
#r "nuget: System.DirectoryServices.Protocols, 8.0.0"
#nullable enable
using System.DirectoryServices.Protocols;

public static Task<DirectoryResponse> SendRequestAsync(this LdapConnection self, DirectoryRequest request, PartialResultProcessing partialMode = PartialResultProcessing.NoPartialResultSupport)
    => Task.Factory.FromAsync<DirectoryRequest, PartialResultProcessing, DirectoryResponse>(self.BeginSendRequest, self.EndSendRequest, request, partialMode, default(object));

public static string? GetAttributeSingleValue(this SearchResultEntry self, string name)
{
    var attr = self.Attributes[name];
    if (attr == null) return null;
    if (attr.Count <= 0) return null;
    if (1 < attr.Count) throw new Exception("multiple attribute value");

    return attr[0] as string;
}

public static IEnumerable<string> EnumerateAttributeValues(this SearchResultEntry self, string name)
{
    var attr = self.Attributes[name];
    if (attr == null) yield break;

    for (var i = 0; i < attr.Count; i++)
    {
        if (attr[i] is string value)
        {
            yield return value;
        }
    }
}

public static DirectoryAttributeModification AddAttributeReplace(this ModifyRequest self, string name, string value)
{
    var attr = new DirectoryAttributeModification();
    attr.Operation = DirectoryAttributeOperation.Replace;
    attr.Name = name;
    attr.Add(value);

    self.Modifications.Add(attr);

    return attr;
}
