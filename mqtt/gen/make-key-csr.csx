#r "nuget: Lestaly, 0.75.0"
#nullable enable
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Lestaly;
using Lestaly.Cx;

var settings = new
{
    // generate key file
    GenerateKeyFile = ThisSource.RelativeFile("../certs/client.key"),

    // generate csr file
    GenerateCsrFile = ThisSource.RelativeFile("../certs/client.csr"),

    // csr subjects
    CsrSubjects = "C=JP, ST=Tokyo, O=Test, CN=test.example",
};

return await Paved.RunAsync(config: o => o.AnyPause(), action: async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Generate key file");
    using var key = RSA.Create();
    await settings.GenerateKeyFile.WithDirectoryCreate().WriteAllTextAsync(key.ExportPkcs8PrivateKeyPem());

    WriteLine("Generate csr file");
    var subjects = new X500DistinguishedName(settings.CsrSubjects);
    var req = new CertificateRequest(subjects, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    await settings.GenerateCsrFile.WithDirectoryCreate().WriteAllTextAsync(req.CreateSigningRequestPem());
});
