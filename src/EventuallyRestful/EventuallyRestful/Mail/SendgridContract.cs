using System.Text.Json;
using System.Text.Json.Serialization;

namespace EventuallyRestful.Mail;

public record SendGridContract(Personalization[] Personalizations, From From, string TemplateId)
{
  private static readonly HttpClient HttpClient = new();
  private static readonly JsonSerializerOptions JsonSerializerOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  public static async Task SendEmail(string email, string about, string name)
  {
    var apiKey = Environment.GetEnvironmentVariable("MY_SENDGRID_KEY")!;

    var jsonContent = JsonContent.Create(
      new SendGridContract(
        [new Personalization([new From(email, name)], new TemplateData(about, name))],
        new From("maestro@consistentlyeventful.com", "Yeray Cabello"),
        "d-b5a7974b7c6e46799ddf810224b3fa9e"),
      options: JsonSerializerOptions);

    var request = new HttpRequestMessage
    {
      Content = jsonContent,
      RequestUri = new Uri("https://api.sendgrid.com/v3/mail/send"),
      Method = HttpMethod.Post,
      Headers = { { "Authorization", $"Bearer {apiKey}" } }
    };

    await HttpClient.SendAsync(request);
  }
}

public record From(string Email, string Name);

public record TemplateData(
  string About,
  string Username);

public record Personalization(From[] To, TemplateData DynamicTemplateData);
