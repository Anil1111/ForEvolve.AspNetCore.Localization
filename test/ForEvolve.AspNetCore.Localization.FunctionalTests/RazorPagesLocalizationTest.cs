using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebPagesSample;
using WebPagesSample.Pages;
using Xunit;

namespace ForEvolve.AspNetCore.Localization
{
    public class RazorPagesLocalizationTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        public RazorPagesLocalizationTest(WebApplicationFactory<Startup> factory)
        {
            if (factory == null) { throw new ArgumentNullException(nameof(factory)); }
            
            // We don't need Antiforgery for tests
            _factory = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services => services
                .PostConfigure<RazorPagesOptions>(options => options.Conventions
                    .ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()))
            ));
        }

        [Theory]
        [InlineData("fr")]
        public async Task When_the_form_is_submitted_messages_should_be_translated(string culture)
        {
            // Arrange
            var uri = $"/?culture={culture}";
            var parser = new HtmlParser();
            var client = _factory.CreateClient();
            var config = Configuration.Default.WithDefaultLoader();

            var model = new IndexModel.ValidationModel();
            var jsonModel = JsonConvert.SerializeObject(model);
            var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonModel);

            var formContent = new FormUrlEncodedContent(dictionary.Select(kv 
                => new KeyValuePair<string, string>($"Model.{kv.Key}", kv.Value)));

            // Post the "form"
            var response = await client.PostAsync(uri, formContent);

            // Parse the response
            var pageContent = await response.Content.ReadAsStringAsync();
            var document = await parser.ParseDocumentAsync(pageContent);
            var summary = document.GetElementById("validationSummary");

            // Find the summary messages
            var listItems = summary.GetElementsByTagName("li");
            var errorMessages = listItems.Select(x => x.TextContent);

            // Assert
            Assert.Collection(errorMessages,
                message => Assert.Equal("Les champs 'Compare1' et 'Compare2' ne sont pas identiques.", message),
                message => Assert.Equal("Le champ CreditCard est une num�ro de carte de cr�dit invalide.", message),
                message => Assert.Equal("Le champ EmailAddress est une adresse courriel invalide.", message),
                message => Assert.Equal("Le champ FileExtensions  accepte uniquement les fichiers aux extensions suivantes: .png, .jpg, .jpeg, .gif", message),
                message => Assert.Equal("Le champ MaxLength doit avoir une longueur maximum de '5'.", message),
                message => Assert.Equal("Le champ MinLength doit avoir une longueur minimum de '5'.", message),
                message => Assert.Equal("Le champ Phone est un num�ro de t�l�phone invalide.", message),
                message => Assert.Equal("Le champ Range doit �tre entre 10 et 20.", message),
                message => Assert.Equal("Le champ RegularExpression doit correspondre � l'expression r�guli�re '[a-z]'.", message),
                message => Assert.Equal("Le champ Required est requis.", message),
                message => Assert.Equal("Le champ StringLength doit �tre compos� d'un maximum de 5 caract�res.", message),
                message => Assert.Equal("Le champ StringLengthIncludingMinimum doit �tre entre 3 et 5 caract�res.", message),
                message => Assert.Equal("Le champ Website n'est pas une URL pleinement qualifi�e valide, commen�ant par HTTP, HTTP ou FTP.", message)
            );
        }
    }
}
