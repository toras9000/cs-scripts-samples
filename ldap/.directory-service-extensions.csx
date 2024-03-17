#r "nuget: System.DirectoryServices, 8.0.0"
#r "nuget: System.DirectoryServices.Protocols, 8.0.0"
#nullable enable
using System.DirectoryServices.Protocols;

public static Task<DirectoryResponse> SendRequestAsync(this LdapConnection self, DirectoryRequest request, PartialResultProcessing partialMode = PartialResultProcessing.NoPartialResultSupport)
    => Task.Factory.FromAsync<DirectoryRequest, PartialResultProcessing, DirectoryResponse>(self.BeginSendRequest, self.EndSendRequest, request, partialMode, default(object));