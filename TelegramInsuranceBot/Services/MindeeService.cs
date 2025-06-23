using Mindee;
using Mindee.Http;
using Mindee.Input;
using Mindee.Product.InternationalId;
using Mindee.Product.Generated;

namespace TelegramInsuranceBot.Services;

public class MindeeService
{
    private readonly MindeeClient _client;

    public MindeeService(IConfiguration config)
    {
        var apiKey = config["MINDEE_API_KEY"]!;
        _client = new MindeeClient(apiKey);
    }

    public async Task<string> AnalyzeDocumentAsync(string filePath)
    {
        var inputSource = new LocalInputSource(filePath);
        var result = await _client.EnqueueAndParseAsync<InternationalIdV2>(inputSource);
        return result.Document.ToString();
    }

    public async Task<string> AnalyzeCarCertificateAsync(string filePath)
    {
        var inputSource = new LocalInputSource(filePath);

        var endpoint = new CustomEndpoint(
            endpointName: "vehicle_registration_certificate",
            accountName: "Serhii7",
            version: "1"
        );

        var result = await _client.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);

        return result.Document.ToString();
    }
}
